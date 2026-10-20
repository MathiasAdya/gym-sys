using GymSys.Core.Entities;
using GymSys.Core.Services;

namespace GymSys.App.Fakes;

/// <summary>
/// "Database" PALSU di memori untuk Member, Paket, Instruktur, dan penugasan PT (Anggota 3),
/// dipakai bersama tiga Fake repository supaya datanya saling nyambung.
/// Catatan: layar Transaksi (Anggota 1) memakai FakeTransactionRepository dengan datanya sendiri.
/// </summary>
public sealed class FakeMasterDataStore
{
    private int _lastMemberId;
    private int _lastPlanId;
    private int _lastTrainerId;
    private int _lastAssignmentId;
    private int _lastTransactionId;

    public object Lock { get; } = new();

    public List<Member> Members { get; } = new();
    public List<MembershipPlan> Plans { get; } = new();
    public List<Trainer> Trainers { get; } = new();
    public List<PTAssignment> Assignments { get; } = new();

    /// <summary>Transaksi palsu, hanya untuk status member dan riwayat di Detail Member.</summary>
    public List<Transaction> Transactions { get; } = new();

    /// <summary>Akun berperan Instruktur. UserId 3 = akun demo pt/pt123 di FakeUserStore.</summary>
    public List<(int UserId, string Username, string NamaLengkap)> InstrukturAccounts { get; } =
        new() { (3, "pt", "Bima Saputra") };

    public FakeMasterDataStore()
    {
        var hariIni = DateOnly.FromDateTime(DateTime.Today);

        Plans.Add(new MembershipPlan { PlanId = 1, NamaPaket = "Bronze 1 Bulan", DurasiHari = 30, Harga = 250_000m, Deskripsi = "Akses gym 30 hari" });
        Plans.Add(new MembershipPlan { PlanId = 2, NamaPaket = "Silver 3 Bulan", DurasiHari = 90, Harga = 675_000m, Deskripsi = "Akses gym 90 hari" });
        Plans.Add(new MembershipPlan { PlanId = 3, NamaPaket = "Gold 12 Bulan", DurasiHari = 365, Harga = 2_400_000m, Deskripsi = "Akses gym 365 hari" });
        _lastPlanId = 3;

        string[] depan = { "Andi", "Siti", "Budi", "Dewi", "Rizky", "Putri", "Agus", "Nina", "Fajar", "Maya", "Hendra", "Lina" };
        string[] belakang = { "Pratama", "Rahmawati", "Santoso", "Lestari", "Ramadhan", "Wijaya", "Saputra", "Anggraini", "Hidayat", "Kusuma" };

        // 60 member supaya paging (50 per halaman) bisa dicoba.
        for (var i = 0; i < 60; i++)
        {
            var bergabung = hariIni.AddDays(-(60 - i) * 5);
            var member = new Member
            {
                MemberId = NextMemberId(),
                KodeMember = KodeBerikutnya(bergabung.Year),
                NamaLengkap = $"{depan[i % depan.Length]} {belakang[i % belakang.Length]}",
                NoTelepon = $"0812{i + 1:D8}",
                Email = i % 3 == 0 ? $"member{i + 1}@contoh.id" : null,
                JenisKelamin = i % 2 == 0 ? "L" : "P",
                TglBergabung = bergabung,
                IsActive = i % 13 != 12,
            };
            Members.Add(member);

            // Variasi status: Aktif, Akan Habis, Kedaluwarsa, Belum Berlangganan.
            DateOnly? berakhir = (i % 5) switch
            {
                0 => hariIni.AddDays(40),
                1 => hariIni.AddDays(3),
                2 => hariIni.AddDays(-10),
                3 => null,
                _ => hariIni.AddDays(100),
            };
            if (berakhir is not { } akhir)
                continue;

            var plan = i % 5 == 4 ? Plans[1] : Plans[0];
            var mulai = akhir.AddDays(-(plan.DurasiHari - 1));
            var id = ++_lastTransactionId;
            Transactions.Add(new Transaction
            {
                TransactionId = id,
                NoKuitansi = $"{MembershipCalculator.AwalanKuitansi(mulai)}{id:D4}",
                MemberId = member.MemberId,
                PlanId = plan.PlanId,
                UserIdKasir = 2,
                TglTransaksi = mulai.ToDateTime(new TimeOnly(10, 0)),
                TglMulaiAktif = mulai,
                TglBerakhir = akhir,
                HargaPaket = plan.Harga,
                TotalBayar = plan.Harga,
                MetodeBayar = MetodeBayar.Tunai,
                Status = StatusTransaksi.Lunas,
            });
        }

        Trainers.Add(new Trainer { TrainerId = 1, UserId = 3, Nama = "Bima Saputra", NoTelepon = "081300000001", Keahlian = "Kekuatan & hipertrofi" });
        Trainers.Add(new Trainer { TrainerId = 2, Nama = "Sari Anjani", NoTelepon = "081300000002", Keahlian = "Yoga & mobilitas" });
        Trainers.Add(new Trainer { TrainerId = 3, Nama = "Dodi Kurniawan", Keahlian = "Cardio", IsActive = false });
        _lastTrainerId = 3;

        // Member 1–6 ditangani Bima (akun pt), 7–9 ditangani Sari.
        for (var memberId = 1; memberId <= 9; memberId++)
        {
            Assignments.Add(new PTAssignment
            {
                AssignmentId = NextAssignmentId(),
                MemberId = memberId,
                TrainerId = memberId <= 6 ? 1 : 2,
                TglMulai = hariIni.AddDays(-30),
            });
        }
    }

    public int NextMemberId() => ++_lastMemberId;
    public int NextPlanId() => ++_lastPlanId;
    public int NextTrainerId() => ++_lastTrainerId;
    public int NextAssignmentId() => ++_lastAssignmentId;

    /// <summary>Kode member berikutnya untuk satu tahun, memakai aturan yang sama dengan repository asli.</summary>
    public string KodeBerikutnya(int tahun)
    {
        var awalan = MasterDataRules.AwalanKodeMember(tahun);
        var terakhir = Members
            .Where(m => m.KodeMember.StartsWith(awalan, StringComparison.Ordinal))
            .Select(m => m.KodeMember)
            .Max();
        return MasterDataRules.KodeMemberBerikutnya(tahun, terakhir);
    }

    public DateOnly? TglBerakhirTerakhir(int memberId) =>
        MembershipCalculator.TglBerakhirTerakhir(Transactions.Where(t => t.MemberId == memberId));

    // Salinan supaya ViewModel tidak mengubah data "database" sebelum tombol Simpan ditekan.
    public static Member Copy(Member m) => new()
    {
        MemberId = m.MemberId,
        KodeMember = m.KodeMember,
        NamaLengkap = m.NamaLengkap,
        NoTelepon = m.NoTelepon,
        Email = m.Email,
        Alamat = m.Alamat,
        TglLahir = m.TglLahir,
        JenisKelamin = m.JenisKelamin,
        KontakDarurat = m.KontakDarurat,
        CatatanKesehatan = m.CatatanKesehatan,
        TglBergabung = m.TglBergabung,
        IsActive = m.IsActive,
    };

    public static MembershipPlan Copy(MembershipPlan p) => new()
    {
        PlanId = p.PlanId,
        NamaPaket = p.NamaPaket,
        DurasiHari = p.DurasiHari,
        Harga = p.Harga,
        Deskripsi = p.Deskripsi,
        IsActive = p.IsActive,
    };

    public static Trainer Copy(Trainer t) => new()
    {
        TrainerId = t.TrainerId,
        UserId = t.UserId,
        Nama = t.Nama,
        NoTelepon = t.NoTelepon,
        Keahlian = t.Keahlian,
        IsActive = t.IsActive,
    };
}
