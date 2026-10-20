using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

/// <summary>Isi form member (FR-03). Teks kosong disimpan sebagai null.</summary>
public sealed record MemberInput(
    string NamaLengkap,
    string NoTelepon,
    string? Email,
    string? Alamat,
    DateOnly? TglLahir,
    string? JenisKelamin,
    string? KontakDarurat,
    string? CatatanKesehatan);

public sealed record MemberSaveResult(bool Success, string Message, Member? Member)
{
    public static MemberSaveResult Ok(string message, Member member) => new(true, message, member);
    public static MemberSaveResult Fail(string message) => new(false, message, null);
}

/// <summary>Penulisan data member (FR-03, FR-04). Hanya Admin dan Resepsionis; peran dicek di sini, bukan hanya di menu.</summary>
public interface IMemberService
{
    Task<MemberSaveResult> CreateAsync(MemberInput input, CancellationToken ct = default);

    Task<OperationResult> UpdateAsync(int memberId, MemberInput input, CancellationToken ct = default);

    /// <summary>Nonaktifkan (soft delete) atau aktifkan kembali.</summary>
    Task<OperationResult> SetActiveAsync(int memberId, bool isActive, CancellationToken ct = default);
}
