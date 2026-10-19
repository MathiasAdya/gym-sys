using GymSys.Core.Entities;
using GymSys.Core.Interfaces;
using GymSys.Core.Services;

namespace GymSys.Tests;

public class TransactionServiceTests
{
    private static readonly DateOnly HariIni = new(2026, 10, 19);

    private readonly FakeTransactionRepository _repo = new();
    private readonly CurrentUserService _currentUser = new();
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _service = new TransactionService(_repo, _currentUser, new FixedTime(new DateTime(2026, 10, 19, 9, 30, 0)));
        _repo.Members.Add(new Member { MemberId = 1, KodeMember = "M0001", NamaLengkap = "Budi", NoTelepon = "0811" });
        _repo.Plans.Add(new MembershipPlan { PlanId = 1, NamaPaket = "Bronze", DurasiHari = 30, Harga = 250_000m, IsActive = true });
        SignIn(UserRole.Resepsionis);
    }

    [Fact]
    public async Task Create_MemberBaru_MulaiHariIniHargaDisalinNomorUrut()
    {
        var result = await _service.CreateAsync(new(1, 1, HariIni, 50_000m, MetodeBayar.Tunai));

        Assert.True(result.Success, result.Message);
        var t = result.Transaction!;
        Assert.Equal("INV-20261019-0001", t.NoKuitansi);
        Assert.Equal(HariIni, t.TglMulaiAktif);
        Assert.Equal(new DateOnly(2026, 11, 17), t.TglBerakhir);
        Assert.Equal(250_000m, t.HargaPaket);
        Assert.Equal(200_000m, t.TotalBayar);
        Assert.Equal(7, t.UserIdKasir);
        Assert.Equal(StatusTransaksi.Lunas, t.Status);
    }

    [Fact]
    public async Task Create_DuaTransaksiSehari_NomorKuitansiBerurutan()
    {
        _repo.Members.Add(new Member { MemberId = 2, KodeMember = "M0002", NamaLengkap = "Sari", NoTelepon = "0812" });

        await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai));
        var kedua = await _service.CreateAsync(new(2, 1, HariIni, 0, MetodeBayar.Transfer));

        Assert.Equal("INV-20261019-0002", kedua.Transaction!.NoKuitansi);
    }

    [Fact]
    public async Task Create_PerpanjangSaatAktif_HarusMulaiSetelahBerakhir()
    {
        _repo.Transactions.Add(Lunas(berakhir: new DateOnly(2026, 10, 25)));

        var terlaluAwal = await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai));
        var tepat = await _service.CreateAsync(new(1, 1, new DateOnly(2026, 10, 26), 0, MetodeBayar.Tunai));

        Assert.False(terlaluAwal.Success);
        Assert.True(tepat.Success, tepat.Message);
    }

    [Fact]
    public async Task Create_TransaksiBatalTidakDihitung()
    {
        _repo.Transactions.Add(Lunas(berakhir: new DateOnly(2026, 10, 25), status: StatusTransaksi.Batal));

        var result = await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai));

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task Create_Backdate_HanyaAdmin()
    {
        var kemarin = HariIni.AddDays(-1);

        Assert.False((await _service.CreateAsync(new(1, 1, kemarin, 0, MetodeBayar.Tunai))).Success);
        SignIn(UserRole.Admin);
        Assert.True((await _service.CreateAsync(new(1, 1, kemarin, 0, MetodeBayar.Tunai))).Success);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(250_001)]
    public async Task Create_DiskonDiLuarRentang_Ditolak(decimal diskon)
    {
        var result = await _service.CreateAsync(new(1, 1, HariIni, diskon, MetodeBayar.Tunai));

        Assert.False(result.Success);
        Assert.Empty(_repo.Transactions);
    }

    [Fact]
    public async Task Create_Instruktur_Ditolak()
    {
        SignIn(UserRole.Instruktur);

        var result = await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai));

        Assert.False(result.Success);
        Assert.Empty(_repo.Transactions);
    }

    [Fact]
    public async Task Create_MemberAtauPaketNonaktif_Ditolak()
    {
        _repo.Members[0].IsActive = false;
        Assert.False((await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai))).Success);

        _repo.Members[0].IsActive = true;
        _repo.Plans[0].IsActive = false;
        Assert.False((await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai))).Success);
        Assert.Empty(_repo.Transactions);
    }

    [Fact]
    public async Task Create_KasirLainMemperpanjangBersamaan_DitolakTanpaMenyimpan()
    {
        _repo.BeforeAdd = () => _repo.Transactions.Add(Lunas(berakhir: new DateOnly(2026, 11, 17)));

        var result = await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai));

        Assert.False(result.Success);
        Assert.Single(_repo.Transactions);
    }

    [Fact]
    public async Task Cancel_Admin_StatusBatalAlasanDisimpanMasaAktifDihitungUlang()
    {
        var aktif = Lunas(berakhir: new DateOnly(2026, 10, 25));
        _repo.Transactions.Add(aktif);
        SignIn(UserRole.Admin);

        var result = await _service.CancelAsync(aktif.TransactionId, "  Salah pilih paket  ");

        Assert.True(result.Success, result.Message);
        Assert.Equal(StatusTransaksi.Batal, aktif.Status);
        Assert.Equal("Salah pilih paket", aktif.AlasanBatal);
        Assert.Single(_repo.Transactions);
        // Tanpa transaksi yang dibatalkan, member kembali bisa mulai hari ini.
        Assert.True((await _service.CreateAsync(new(1, 1, HariIni, 0, MetodeBayar.Tunai))).Success);
    }

    [Fact]
    public async Task Cancel_Resepsionis_Ditolak()
    {
        var aktif = Lunas(berakhir: new DateOnly(2026, 10, 25));
        _repo.Transactions.Add(aktif);

        var result = await _service.CancelAsync(aktif.TransactionId, "alasan");

        Assert.False(result.Success);
        Assert.Equal(StatusTransaksi.Lunas, aktif.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Cancel_AlasanKosong_Ditolak(string alasan)
    {
        var aktif = Lunas(berakhir: new DateOnly(2026, 10, 25));
        _repo.Transactions.Add(aktif);
        SignIn(UserRole.Admin);

        Assert.False((await _service.CancelAsync(aktif.TransactionId, alasan)).Success);
        Assert.Equal(StatusTransaksi.Lunas, aktif.Status);
    }

    [Fact]
    public async Task Cancel_AlasanTerlaluPanjang_Ditolak()
    {
        var aktif = Lunas(berakhir: new DateOnly(2026, 10, 25));
        _repo.Transactions.Add(aktif);
        SignIn(UserRole.Admin);

        var result = await _service.CancelAsync(aktif.TransactionId, new string('x', TransactionService.MaksPanjangAlasan + 1));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Cancel_SudahBatal_Ditolak()
    {
        var batal = Lunas(berakhir: new DateOnly(2026, 10, 25), StatusTransaksi.Batal);
        _repo.Transactions.Add(batal);
        SignIn(UserRole.Admin);

        Assert.False((await _service.CancelAsync(batal.TransactionId, "lagi")).Success);
    }

    private Transaction Lunas(DateOnly berakhir, StatusTransaksi status = StatusTransaksi.Lunas) => new()
    {
        TransactionId = 100 + _repo.Transactions.Count,
        NoKuitansi = $"INV-20260101-{_repo.Transactions.Count + 1:D4}",
        MemberId = 1,
        PlanId = 1,
        TglMulaiAktif = berakhir.AddDays(-29),
        TglBerakhir = berakhir,
        Status = status,
    };

    private void SignIn(UserRole role) => _currentUser.SignIn(new UserInfo(7, "kasir", "Kasir", role));

    private sealed class FixedTime(DateTime local) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(local, DateTimeKind.Utc));
    }
}
