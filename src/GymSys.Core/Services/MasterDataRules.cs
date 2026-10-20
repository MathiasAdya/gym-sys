using System.Net.Mail;

namespace GymSys.Core.Services;

/// <summary>
/// Aturan validasi data master (FR-03, FR-09) di satu tempat, supaya form, service, dan test memakai
/// aturan yang sama. Setiap method mengembalikan pesan error untuk ditampilkan, atau null kalau valid.
/// </summary>
public static class MasterDataRules
{
    public const int MinDigitTelepon = 10;
    public const int MaksDigitTelepon = 15;
    public const int MaksPanjangNama = 100;
    public const int MaksPanjangEmail = 100;
    public const int MaksPanjangNamaPaket = 50;

    /// <summary>Awalan kode member untuk satu tahun, "GYM-YYYY-".</summary>
    public static string AwalanKodeMember(int tahun) => $"GYM-{tahun}-";

    /// <summary>
    /// FR-03: kode member berikutnya, urut per tahun (GYM-YYYY-NNNN).
    /// <paramref name="kodeTerakhirTahunItu"/> = kode terbesar yang sudah ada untuk tahun itu, atau null.
    /// </summary>
    public static string KodeMemberBerikutnya(int tahun, string? kodeTerakhirTahunItu)
    {
        var awalan = AwalanKodeMember(tahun);
        var urut = 1;
        if (kodeTerakhirTahunItu is not null)
        {
            if (!kodeTerakhirTahunItu.StartsWith(awalan, StringComparison.Ordinal)
                || !int.TryParse(kodeTerakhirTahunItu.AsSpan(awalan.Length), out var terakhir))
                throw new ArgumentException($"Kode member tidak valid untuk tahun {tahun}: {kodeTerakhirTahunItu}", nameof(kodeTerakhirTahunItu));
            urut = terakhir + 1;
        }
        if (urut > 9999)
            throw new InvalidOperationException($"Kode member untuk tahun {tahun} sudah habis (maksimal 9999 per tahun).");
        return $"{awalan}{urut:D4}";
    }

    public static string? ValidasiNama(string? nama)
    {
        if (string.IsNullOrWhiteSpace(nama))
            return "Nama wajib diisi.";
        if (nama.Trim().Length > MaksPanjangNama)
            return $"Nama maksimal {MaksPanjangNama} karakter.";
        return null;
    }

    /// <summary>FR-03: wajib, hanya angka, 10–15 digit. Keunikan dicek terpisah lewat repository.</summary>
    public static string? ValidasiNoTelepon(string? noTelepon)
    {
        if (string.IsNullOrWhiteSpace(noTelepon))
            return "No. HP wajib diisi.";
        var no = noTelepon.Trim();
        if (!no.All(char.IsAsciiDigit))
            return "No. HP hanya boleh berisi angka.";
        if (no.Length is < MinDigitTelepon or > MaksDigitTelepon)
            return $"No. HP harus {MinDigitTelepon}–{MaksDigitTelepon} digit.";
        return null;
    }

    /// <summary>FR-03: email boleh kosong, tapi harus valid bila diisi.</summary>
    public static string? ValidasiEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;
        var value = email.Trim();
        if (value.Length > MaksPanjangEmail)
            return $"Email maksimal {MaksPanjangEmail} karakter.";
        return MailAddress.TryCreate(value, out var address) && address.Address == value
            ? null
            : "Format email tidak valid.";
    }

    public static string? ValidasiTglLahir(DateOnly? tglLahir, DateOnly hariIni) =>
        tglLahir > hariIni ? "Tanggal lahir tidak boleh setelah hari ini." : null;

    /// <summary>FR-09: nama paket wajib dan maksimal 50 karakter. Keunikan dicek lewat repository.</summary>
    public static string? ValidasiNamaPaket(string? namaPaket)
    {
        if (string.IsNullOrWhiteSpace(namaPaket))
            return "Nama paket wajib diisi.";
        if (namaPaket.Trim().Length > MaksPanjangNamaPaket)
            return $"Nama paket maksimal {MaksPanjangNamaPaket} karakter.";
        return null;
    }

    /// <summary>FR-09: DurasiHari &gt; 0.</summary>
    public static string? ValidasiDurasiHari(int durasiHari) =>
        durasiHari > 0 ? null : "Durasi harus lebih dari 0 hari.";

    /// <summary>FR-09: Harga ≥ 0.</summary>
    public static string? ValidasiHarga(decimal harga) =>
        harga >= 0 ? null : "Harga tidak boleh negatif.";
}
