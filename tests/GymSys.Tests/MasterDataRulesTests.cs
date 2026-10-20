using GymSys.Core.Services;

namespace GymSys.Tests;

public class MasterDataRulesTests
{
    private static readonly DateOnly HariIni = new(2026, 11, 10);

    [Fact]
    public void KodeMember_PertamaDiTahunItu() =>
        Assert.Equal("GYM-2026-0001", MasterDataRules.KodeMemberBerikutnya(2026, null));

    [Theory]
    [InlineData("GYM-2026-0001", "GYM-2026-0002")]
    [InlineData("GYM-2026-0099", "GYM-2026-0100")]
    public void KodeMember_LanjutDariTerakhir(string terakhir, string berikutnya) =>
        Assert.Equal(berikutnya, MasterDataRules.KodeMemberBerikutnya(2026, terakhir));

    [Theory]
    [InlineData("GYM-2025-0007")] // tahun lain
    [InlineData("M0001")]          // format lama
    public void KodeMember_FormatTidakSesuai_Throw(string terakhir) =>
        Assert.Throws<ArgumentException>(() => MasterDataRules.KodeMemberBerikutnya(2026, terakhir));

    [Fact]
    public void KodeMember_Habis_Throw() =>
        Assert.Throws<InvalidOperationException>(() => MasterDataRules.KodeMemberBerikutnya(2026, "GYM-2026-9999"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Nama_Kosong_Error(string? nama) =>
        Assert.NotNull(MasterDataRules.ValidasiNama(nama));

    [Fact]
    public void Nama_Valid() =>
        Assert.Null(MasterDataRules.ValidasiNama("Budi Santoso"));

    [Theory]
    [InlineData("0812345678")]      // 10 digit
    [InlineData("081234567890123")] // 15 digit
    [InlineData(" 081234567890 ")]  // spasi di ujung diabaikan
    public void NoTelepon_Valid(string no) =>
        Assert.Null(MasterDataRules.ValidasiNoTelepon(no));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("081234567")]        // 9 digit
    [InlineData("0812345678901234")] // 16 digit
    [InlineData("0812-3456-7890")]
    [InlineData("+6281234567890")]
    public void NoTelepon_TidakValid(string? no) =>
        Assert.NotNull(MasterDataRules.ValidasiNoTelepon(no));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("budi@gmail.com")]
    public void Email_KosongAtauValid(string? email) =>
        Assert.Null(MasterDataRules.ValidasiEmail(email));

    [Theory]
    [InlineData("budi")]
    [InlineData("budi@")]
    [InlineData("@gmail.com")]
    public void Email_TidakValid(string email) =>
        Assert.NotNull(MasterDataRules.ValidasiEmail(email));

    [Fact]
    public void TglLahir_SetelahHariIni_Error() =>
        Assert.NotNull(MasterDataRules.ValidasiTglLahir(HariIni.AddDays(1), HariIni));

    [Fact]
    public void TglLahir_KosongAtauLampau_Valid()
    {
        Assert.Null(MasterDataRules.ValidasiTglLahir(null, HariIni));
        Assert.Null(MasterDataRules.ValidasiTglLahir(new DateOnly(2000, 5, 1), HariIni));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void DurasiHari_TidakPositif_Error(int durasi) =>
        Assert.NotNull(MasterDataRules.ValidasiDurasiHari(durasi));

    [Fact]
    public void Harga_Negatif_Error() =>
        Assert.NotNull(MasterDataRules.ValidasiHarga(-1m));

    [Fact]
    public void Harga_Nol_Valid() =>
        Assert.Null(MasterDataRules.ValidasiHarga(0m));

    [Fact]
    public void NamaPaket_Kosong_Error() =>
        Assert.NotNull(MasterDataRules.ValidasiNamaPaket(" "));
}
