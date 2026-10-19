namespace GymSys.Core.Interfaces;

// KONTRAK USULAN: query agregat dashboard (PRD Bagian 11 & 12.2).
// Implementasi asli: ADO.NET + view vw_MemberStatus di GymSys.Data/Queries/DashboardQueries.cs.

public sealed record DashboardSummary(
    int TotalMemberAktif,
    int MemberBaruBulanIni,
    int AkanHabis7Hari,
    decimal PendapatanBulanIni);

/// <summary>Jumlah pendaftaran per bulan untuk grafik tren. Label contoh: "Okt".</summary>
public sealed record MonthlyCount(string Label, int Count);

public sealed record ExpiringMemberDto(
    string KodeMember,
    string NamaLengkap,
    string NoTelepon,
    DateTime TglBerakhir,
    int SisaHari);

public interface IDashboardQueries
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken ct = default);

    /// <summary>Urut dari bulan terlama ke terbaru, sebanyak <paramref name="months"/> bulan.</summary>
    Task<IReadOnlyList<MonthlyCount>> GetRegistrationTrendAsync(int months, CancellationToken ct = default);

    /// <summary>Member yang masa aktifnya habis dalam <paramref name="withinDays"/> hari, urut dari yang terdekat.</summary>
    Task<IReadOnlyList<ExpiringMemberDto>> GetExpiringMembersAsync(int withinDays, CancellationToken ct = default);
}
