using System.Diagnostics;
using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;
using GymSys.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Uji performa NFR-02: 5.000 member + 20.000 transaksi.
//   Dashboard < 2 detik, pencarian < 500 ms. Juga mengukur halaman Riwayat Transaksi dan laporan status member.
// Pakai DATABASE TERPISAH, jangan GymSysDb harian. Contoh:
//   dotnet run -c Release --project tools/GymSys.PerfTest -- "Server=localhost;Database=GymSysPerf;Trusted_Connection=True;TrustServerCertificate=True"
// Database dibuat/di-migrate otomatis; data uji hanya diisi sekali (kode member PERF*).

const int JumlahMember = 5_000;
const int TransaksiPerMember = 4;
const int Ulang = 5;

if (args.Length != 1)
{
    Console.Error.WriteLine("Pemakaian: GymSys.PerfTest \"<connection string ke database uji, mis. Database=GymSysPerf>\"");
    return 2;
}

var connectionString = args[0];
var dbName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
if (string.Equals(dbName, "GymSysDb", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Tolak: jangan isi data uji ke GymSysDb. Pakai database terpisah, mis. GymSysPerf.");
    return 2;
}

using var services = new ServiceCollection().AddGymSysData(connectionString).BuildServiceProvider();
var factory = services.GetRequiredService<IDbContextFactory<GymSysDbContext>>();

await using (var db = await factory.CreateDbContextAsync())
{
    Console.WriteLine($"Migrate {dbName}...");
    await db.Database.MigrateAsync();
}

await SeedAsync();

var dashboard = services.GetRequiredService<IDashboardQueries>();
var reports = services.GetRequiredService<IReportQueries>();
var transactions = services.GetRequiredService<ITransactionRepository>();
var hariIni = DateOnly.FromDateTime(DateTime.Today);

var hasil = new List<(string Nama, double MaksMs, long BatasMs)>
{
    await UkurAsync("Dashboard (ringkasan + tren 12 bln + akan habis)", 2000, async () =>
    {
        await dashboard.GetSummaryAsync();
        await dashboard.GetRegistrationTrendAsync(12);
        await dashboard.GetExpiringMembersAsync(7);
    }),
    await UkurAsync("Cari member: nama \"budi\"", 500, () => transactions.SearchMembersAsync("budi", 20)),
    await UkurAsync("Cari member: kode \"PERF0421\"", 500, () => transactions.SearchMembersAsync("PERF0421", 20)),
    await UkurAsync("Cari member: no. HP \"0811000042\"", 500, () => transactions.SearchMembersAsync("0811000042", 20)),
    await UkurAsync("Riwayat transaksi 30 hari, halaman 1", 500, () =>
        transactions.GetHistoryAsync(new TransactionHistoryQuery(hariIni.AddDays(-29), hariIni, null, 1, 50))),
    await UkurAsync("Laporan status member (semua)", 2000, () => reports.GetMemberStatusAsync()),
    await UkurAsync("Laporan transaksi bulan ini", 2000, () =>
        reports.GetTransactionsAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today)),
};

Console.WriteLine();
Console.WriteLine($"{"Skenario",-52} {"Maks (ms)",10} {"Batas",7}  Hasil");
foreach (var (nama, maks, batas) in hasil)
    Console.WriteLine($"{nama,-52} {maks,10:0} {batas,7}  {(maks < batas ? "LULUS" : "GAGAL")}");

return hasil.All(h => h.MaksMs < h.BatasMs) ? 0 : 1;

// Ukur waktu terlama dari beberapa kali jalan (setelah satu kali pemanasan), bukan rata-rata.
static async Task<(string, double, long)> UkurAsync(string nama, long batasMs, Func<Task> aksi)
{
    await aksi();
    double maks = 0;
    for (var i = 0; i < Ulang; i++)
    {
        var sw = Stopwatch.StartNew();
        await aksi();
        maks = Math.Max(maks, sw.Elapsed.TotalMilliseconds);
    }
    return (nama, maks, batasMs);
}

async Task SeedAsync()
{
    await using var db = await factory.CreateDbContextAsync();
    if (await db.Members.AnyAsync(m => m.KodeMember.StartsWith("PERF")))
    {
        Console.WriteLine("Data uji sudah ada, lewati pengisian.");
        return;
    }

    var kasir = await db.Users.Select(u => u.UserId).FirstAsync();
    var plans = await db.MembershipPlans.Where(p => p.IsActive).ToListAsync();
    if (plans.Count == 0)
    {
        plans =
        [
            new MembershipPlan { NamaPaket = "PERF Bulanan", DurasiHari = 30, Harga = 250_000m },
            new MembershipPlan { NamaPaket = "PERF 3 Bulan", DurasiHari = 90, Harga = 675_000m },
            new MembershipPlan { NamaPaket = "PERF Tahunan", DurasiHari = 365, Harga = 2_400_000m },
        ];
        db.MembershipPlans.AddRange(plans);
        await db.SaveChangesAsync();
    }

    string[] depan = ["Budi", "Siti", "Andi", "Dewi", "Rizky", "Putri", "Agus", "Rina", "Joko", "Maya", "Fajar", "Lina"];
    string[] belakang = ["Santoso", "Rahmawati", "Pratama", "Lestari", "Ramadhan", "Wulandari", "Saputra", "Hidayat", "Kusuma", "Nugroho"];
    var rnd = new Random(42);
    var hariIni = DateOnly.FromDateTime(DateTime.Today);
    var urutPerHari = new Dictionary<DateOnly, int>();

    Console.WriteLine($"Mengisi {JumlahMember} member + {JumlahMember * TransaksiPerMember} transaksi...");
    db.ChangeTracker.AutoDetectChangesEnabled = false;
    var members = new List<Member>(JumlahMember);
    for (var i = 1; i <= JumlahMember; i++)
    {
        // Masa aktif terakhir tersebar dari ~1 tahun lalu sampai ~3 bulan ke depan: ada Aktif, Akan Habis, Kedaluwarsa.
        var berakhirTerakhir = hariIni.AddDays(rnd.Next(-365, 90));
        var mulai = berakhirTerakhir;
        var trx = new List<Transaction>();
        for (var k = 0; k < TransaksiPerMember; k++)
        {
            var plan = plans[rnd.Next(plans.Count)];
            var tglMulai = k == 0 ? berakhirTerakhir.AddDays(-(plan.DurasiHari - 1)) : mulai.AddDays(-(plan.DurasiHari));
            var tglBerakhir = MembershipCalculator.HitungTglBerakhir(tglMulai, plan.DurasiHari);
            mulai = tglMulai;
            var tglTransaksi = tglMulai > hariIni ? hariIni : tglMulai;
            var urut = urutPerHari[tglTransaksi] = urutPerHari.GetValueOrDefault(tglTransaksi) + 1;
            var diskon = rnd.Next(10) == 0 ? 50_000m : 0m;
            trx.Add(new Transaction
            {
                NoKuitansi = $"{MembershipCalculator.AwalanKuitansi(tglTransaksi)}{urut:D4}",
                PlanId = plan.PlanId, UserIdKasir = kasir,
                TglTransaksi = tglTransaksi.ToDateTime(new TimeOnly(8 + rnd.Next(12), rnd.Next(60))),
                TglMulaiAktif = tglMulai, TglBerakhir = tglBerakhir,
                HargaPaket = plan.Harga, Diskon = diskon, TotalBayar = plan.Harga - diskon,
                MetodeBayar = rnd.Next(2) == 0 ? MetodeBayar.Tunai : MetodeBayar.Transfer,
                Status = rnd.Next(20) == 0 ? StatusTransaksi.Batal : StatusTransaksi.Lunas,
            });
        }
        foreach (var t in trx.Where(t => t.Status == StatusTransaksi.Batal))
            t.AlasanBatal = "Data uji performa";

        members.Add(new Member
        {
            KodeMember = $"PERF{i:D4}",
            NamaLengkap = $"{depan[rnd.Next(depan.Length)]} {belakang[rnd.Next(belakang.Length)]} {i}",
            NoTelepon = $"0811{i:D6}",
            TglBergabung = trx.Min(t => t.TglMulaiAktif),
            Transactions = trx,
        });
    }

    var sw = Stopwatch.StartNew();
    foreach (var batch in members.Chunk(500))
    {
        db.Members.AddRange(batch);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }
    Console.WriteLine($"Selesai mengisi dalam {sw.Elapsed.TotalSeconds:0.0} detik.");
}
