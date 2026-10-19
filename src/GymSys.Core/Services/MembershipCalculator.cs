using GymSys.Core.Entities;

namespace GymSys.Core.Services;

/// <summary>
/// Satu-satunya tempat aturan bisnis PRD Bagian 7. UI, service, dan laporan wajib memakai kelas ini.
/// "Hari ini" selalu dioper sebagai parameter supaya bisa diuji.
/// Logika status harus sama persis dengan view vw_MemberStatus (lihat migration InitialCreate).
/// </summary>
public static class MembershipCalculator
{
    public const int HariAkanHabis = 7;
    public const int MaksHariKeDepan = 30;

    /// <summary>PRD 7.1: masa aktif inklusif, TglBerakhir = TglMulai + (DurasiHari - 1).</summary>
    public static DateOnly HitungTglBerakhir(DateOnly tglMulai, int durasiHari)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durasiHari);
        return tglMulai.AddDays(durasiHari - 1);
    }

    /// <summary>TglBerakhir terbesar dari transaksi Lunas, atau null kalau belum pernah berlangganan.</summary>
    public static DateOnly? TglBerakhirTerakhir(IEnumerable<Transaction> transaksi) =>
        transaksi.Where(t => t.Status == StatusTransaksi.Lunas)
                 .Select(t => (DateOnly?)t.TglBerakhir)
                 .Max();

    /// <summary>PRD 7.2: member masih aktif mulai sehari setelah TglBerakhir terakhir, selain itu hari ini.</summary>
    public static DateOnly HitungTglMulaiDefault(DateOnly? tglBerakhirTerakhir, DateOnly hariIni) =>
        tglBerakhirTerakhir >= hariIni ? tglBerakhirTerakhir.Value.AddDays(1) : hariIni;

    /// <summary>
    /// PRD 7.2: tanggal pilihan resepsionis paling awal = tanggal default (tidak ada backdate dan
    /// sisa masa aktif tidak hilang), paling lambat default + 30 hari. Admin boleh lebih awal.
    /// </summary>
    /// <returns>Pesan error untuk ditampilkan, atau null kalau valid.</returns>
    public static string? ValidasiTglMulai(DateOnly tglMulai, DateOnly? tglBerakhirTerakhir, DateOnly hariIni, bool isAdmin)
    {
        var tglDefault = HitungTglMulaiDefault(tglBerakhirTerakhir, hariIni);
        if (tglMulai < tglDefault && !isAdmin)
            return tglDefault == hariIni
                ? "Tanggal mulai tidak boleh sebelum hari ini."
                : $"Member masih aktif sampai {tglBerakhirTerakhir:dd MMM yyyy}; tanggal mulai paling awal {tglDefault:dd MMM yyyy}.";
        if (tglMulai > tglDefault.AddDays(MaksHariKeDepan))
            return $"Tanggal mulai paling lambat {tglDefault.AddDays(MaksHariKeDepan):dd MMM yyyy}.";
        return null;
    }

    /// <summary>PRD 7.3. Nonaktif mengalahkan status lain.</summary>
    public static MemberStatus HitungStatus(bool isActive, DateOnly? tglBerakhirTerakhir, DateOnly hariIni)
    {
        if (!isActive) return MemberStatus.Nonaktif;
        if (tglBerakhirTerakhir is not { } berakhir) return MemberStatus.BelumBerlangganan;
        if (hariIni > berakhir) return MemberStatus.Kedaluwarsa;
        return hariIni <= berakhir.AddDays(-HariAkanHabis) ? MemberStatus.Aktif : MemberStatus.AkanHabis;
    }

    /// <summary>
    /// Hari tersisa setelah hari ini, sama dengan DATEDIFF di DashboardQueries: 0 di hari terakhir,
    /// juga 0 kalau sudah lewat atau belum berlangganan.
    /// </summary>
    public static int HitungSisaHari(DateOnly? tglBerakhirTerakhir, DateOnly hariIni) =>
        tglBerakhirTerakhir is { } berakhir ? Math.Max(0, berakhir.DayNumber - hariIni.DayNumber) : 0;

    /// <summary>PRD 7.4: TotalBayar = HargaPaket - Diskon, dengan 0 ≤ Diskon ≤ HargaPaket.</summary>
    public static decimal HitungTotalBayar(decimal hargaPaket, decimal diskon)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(hargaPaket);
        ArgumentOutOfRangeException.ThrowIfNegative(diskon);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(diskon, hargaPaket);
        return hargaPaket - diskon;
    }

    /// <summary>PRD 7.4: awalan nomor kuitansi untuk satu hari, "INV-YYYYMMDD-".</summary>
    public static string AwalanKuitansi(DateOnly tanggal) => $"INV-{tanggal:yyyyMMdd}-";

    /// <summary>
    /// PRD 7.4: nomor kuitansi berikutnya, urut per hari (INV-YYYYMMDD-NNNN).
    /// <paramref name="nomorTerakhirHariItu"/> = nomor terbesar yang sudah ada untuk tanggal itu, atau null.
    /// </summary>
    public static string NoKuitansiBerikutnya(DateOnly tanggal, string? nomorTerakhirHariItu)
    {
        var awalan = AwalanKuitansi(tanggal);
        var urut = 1;
        if (nomorTerakhirHariItu is not null)
        {
            if (!nomorTerakhirHariItu.StartsWith(awalan, StringComparison.Ordinal)
                || !int.TryParse(nomorTerakhirHariItu.AsSpan(awalan.Length), out var terakhir))
                throw new ArgumentException($"Nomor kuitansi tidak valid untuk {tanggal:yyyy-MM-dd}: {nomorTerakhirHariItu}", nameof(nomorTerakhirHariItu));
            urut = terakhir + 1;
        }
        if (urut > 9999)
            throw new InvalidOperationException($"Nomor kuitansi untuk {tanggal:yyyy-MM-dd} sudah habis (maksimal 9999 per hari).");
        return $"{awalan}{urut:D4}";
    }

    /// <summary>Teks status sama seperti kolom Status di vw_MemberStatus.</summary>
    public static string ToLabel(this MemberStatus status) => status switch
    {
        MemberStatus.BelumBerlangganan => "Belum Berlangganan",
        MemberStatus.AkanHabis => "Akan Habis",
        _ => status.ToString(),
    };

    public static MemberStatus ParseLabel(string label) =>
        Enum.GetValues<MemberStatus>().First(s => s.ToLabel() == label);
}
