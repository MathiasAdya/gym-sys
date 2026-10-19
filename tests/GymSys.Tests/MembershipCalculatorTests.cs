using GymSys.Core.Entities;
using GymSys.Core.Services;

namespace GymSys.Tests;

public class MembershipCalculatorTests
{
    private static readonly DateOnly HariIni = new(2026, 11, 10);

    private static DateOnly D(string iso) => DateOnly.Parse(iso);

    [Theory]
    [InlineData("2026-11-01", 30, "2026-11-30")] // contoh PRD 7.1
    [InlineData("2026-11-01", 1, "2026-11-01")]
    [InlineData("2026-01-01", 365, "2026-12-31")]
    [InlineData("2028-02-01", 29, "2028-02-29")] // tahun kabisat
    public void HitungTglBerakhir_Inklusif(string mulai, int durasi, string berakhir) =>
        Assert.Equal(D(berakhir), MembershipCalculator.HitungTglBerakhir(D(mulai), durasi));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void HitungTglBerakhir_DurasiTidakPositif_Throw(int durasi) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MembershipCalculator.HitungTglBerakhir(HariIni, durasi));

    [Fact]
    public void TglBerakhirTerakhir_AbaikanTransaksiBatal()
    {
        var trx = new[]
        {
            new Transaction { TglBerakhir = D("2026-11-30"), Status = StatusTransaksi.Lunas },
            new Transaction { TglBerakhir = D("2027-02-28"), Status = StatusTransaksi.Batal },
            new Transaction { TglBerakhir = D("2026-10-31"), Status = StatusTransaksi.Lunas },
        };

        Assert.Equal(D("2026-11-30"), MembershipCalculator.TglBerakhirTerakhir(trx));
        Assert.Null(MembershipCalculator.TglBerakhirTerakhir([trx[1]]));
        Assert.Null(MembershipCalculator.TglBerakhirTerakhir([]));
    }

    [Theory]
    [InlineData(null, "2026-11-10")]         // member baru
    [InlineData("2026-11-09", "2026-11-10")] // kedaluwarsa kemarin
    [InlineData("2026-11-10", "2026-11-11")] // hari terakhir aktif = hari ini
    [InlineData("2026-12-05", "2026-12-06")] // perpanjangan saat masih aktif
    public void HitungTglMulaiDefault(string? berakhir, string expected) =>
        Assert.Equal(D(expected), MembershipCalculator.HitungTglMulaiDefault(berakhir is null ? null : D(berakhir), HariIni));

    [Theory]
    [InlineData("2026-11-10", null, false, true)]
    [InlineData("2026-12-10", null, false, true)]          // +30 hari: batas
    [InlineData("2026-12-11", null, false, false)]         // +31 hari
    [InlineData("2026-11-09", null, false, false)]         // backdate oleh resepsionis
    [InlineData("2026-11-01", null, true, true)]           // backdate oleh Admin
    [InlineData("2026-11-20", "2026-12-05", false, false)] // tumpang tindih masa aktif
    [InlineData("2026-12-06", "2026-12-05", false, true)]
    [InlineData("2027-01-05", "2026-12-05", false, true)]  // default + 30
    [InlineData("2027-01-06", "2026-12-05", true, false)]  // batas atas berlaku juga untuk Admin
    public void ValidasiTglMulai(string mulai, string? berakhir, bool isAdmin, bool valid)
    {
        var error = MembershipCalculator.ValidasiTglMulai(D(mulai), berakhir is null ? null : D(berakhir), HariIni, isAdmin);

        Assert.Equal(valid, error is null);
    }

    [Theory]
    [InlineData(true, null, MemberStatus.BelumBerlangganan)]
    [InlineData(true, "2026-11-09", MemberStatus.Kedaluwarsa)] // sehari setelah TglBerakhir
    [InlineData(true, "2026-11-10", MemberStatus.AkanHabis)]   // hari terakhir masih boleh masuk
    [InlineData(true, "2026-11-16", MemberStatus.AkanHabis)]   // berakhir - 7 = kemarin
    [InlineData(true, "2026-11-17", MemberStatus.Aktif)]       // berakhir - 7 = hari ini
    [InlineData(true, "2027-11-10", MemberStatus.Aktif)]
    [InlineData(false, "2027-11-10", MemberStatus.Nonaktif)]   // Nonaktif mengalahkan status lain
    [InlineData(false, null, MemberStatus.Nonaktif)]
    public void HitungStatus(bool isActive, string? berakhir, MemberStatus expected) =>
        Assert.Equal(expected, MembershipCalculator.HitungStatus(isActive, berakhir is null ? null : D(berakhir), HariIni));

    [Theory]
    [InlineData(null, 0)]
    [InlineData("2026-11-09", 0)]
    [InlineData("2026-11-10", 1)]
    [InlineData("2026-11-30", 21)]
    public void HitungSisaHari(string? berakhir, int expected) =>
        Assert.Equal(expected, MembershipCalculator.HitungSisaHari(berakhir is null ? null : D(berakhir), HariIni));

    [Theory]
    [InlineData(250000, 0, 250000)]
    [InlineData(250000, 50000, 200000)]
    [InlineData(250000, 250000, 0)]
    public void HitungTotalBayar(decimal harga, decimal diskon, decimal expected) =>
        Assert.Equal(expected, MembershipCalculator.HitungTotalBayar(harga, diskon));

    [Theory]
    [InlineData(250000, 250001)]
    [InlineData(250000, -1)]
    public void HitungTotalBayar_DiskonTidakValid_Throw(decimal harga, decimal diskon) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MembershipCalculator.HitungTotalBayar(harga, diskon));

    [Theory]
    [InlineData(MemberStatus.BelumBerlangganan, "Belum Berlangganan")]
    [InlineData(MemberStatus.Aktif, "Aktif")]
    [InlineData(MemberStatus.AkanHabis, "Akan Habis")]
    [InlineData(MemberStatus.Kedaluwarsa, "Kedaluwarsa")]
    [InlineData(MemberStatus.Nonaktif, "Nonaktif")]
    public void Label_SamaDenganView(MemberStatus status, string label)
    {
        Assert.Equal(label, status.ToLabel());
        Assert.Equal(status, MembershipCalculator.ParseLabel(label));
    }
}
