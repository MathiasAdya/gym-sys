using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Core.Services;

/// <summary>FR-03 dan FR-04. Validasi memakai MasterDataRules yang sama dengan form member.</summary>
public sealed class MemberService(IMemberRepository members, ICurrentUserService currentUser, TimeProvider time)
    : IMemberService
{
    /// <summary>Sama dengan panjang kolom di tabel Members.</summary>
    public const int MaksPanjangAlamat = 255;
    public const int MaksPanjangKontakDarurat = 100;
    public const int MaksPanjangCatatanKesehatan = 1000;

    private const string BukanPetugas = "Hanya Admin dan Resepsionis yang dapat mengelola data member.";

    public async Task<MemberSaveResult> CreateAsync(MemberInput input, CancellationToken ct = default)
    {
        if (!BolehKelola()) return MemberSaveResult.Fail(BukanPetugas);
        if (Validasi(input) is { } error) return MemberSaveResult.Fail(error);

        if (await members.IsNoTeleponTakenAsync(input.NoTelepon.Trim(), null, ct))
            return MemberSaveResult.Fail("No. HP sudah dipakai member lain.");

        var member = new Member { TglBergabung = HariIni, IsActive = true };
        Salin(input, member);

        var saved = await members.AddAsync(member, ct);
        return MemberSaveResult.Ok($"Member {saved.KodeMember} berhasil ditambahkan.", saved);
    }

    public async Task<OperationResult> UpdateAsync(int memberId, MemberInput input, CancellationToken ct = default)
    {
        if (!BolehKelola()) return OperationResult.Fail(BukanPetugas);
        if (Validasi(input) is { } error) return OperationResult.Fail(error);

        var member = await members.GetByIdAsync(memberId, ct);
        if (member is null) return OperationResult.Fail("Member tidak ditemukan.");

        if (await members.IsNoTeleponTakenAsync(input.NoTelepon.Trim(), memberId, ct))
            return OperationResult.Fail("No. HP sudah dipakai member lain.");

        Salin(input, member);
        await members.UpdateAsync(member, ct);
        return OperationResult.Ok("Data member berhasil disimpan.");
    }

    public async Task<OperationResult> SetActiveAsync(int memberId, bool isActive, CancellationToken ct = default)
    {
        if (!BolehKelola()) return OperationResult.Fail(BukanPetugas);
        if (!await members.SetActiveAsync(memberId, isActive, ct))
            return OperationResult.Fail("Member tidak ditemukan.");
        return OperationResult.Ok(isActive ? "Member diaktifkan kembali." : "Member dinonaktifkan.");
    }

    private string? Validasi(MemberInput input) =>
        MasterDataRules.ValidasiNama(input.NamaLengkap)
        ?? MasterDataRules.ValidasiNoTelepon(input.NoTelepon)
        ?? MasterDataRules.ValidasiEmail(input.Email)
        ?? MasterDataRules.ValidasiTglLahir(input.TglLahir, HariIni)
        ?? (Bersihkan(input.JenisKelamin) is null or "L" or "P" ? null : "Jenis kelamin harus L atau P.")
        ?? PanjangMaks(input.Alamat, MaksPanjangAlamat, "Alamat")
        ?? PanjangMaks(input.KontakDarurat, MaksPanjangKontakDarurat, "Kontak darurat")
        ?? PanjangMaks(input.CatatanKesehatan, MaksPanjangCatatanKesehatan, "Catatan kesehatan");

    private static void Salin(MemberInput input, Member member)
    {
        member.NamaLengkap = input.NamaLengkap.Trim();
        member.NoTelepon = input.NoTelepon.Trim();
        member.Email = Bersihkan(input.Email);
        member.Alamat = Bersihkan(input.Alamat);
        member.TglLahir = input.TglLahir;
        member.JenisKelamin = Bersihkan(input.JenisKelamin);
        member.KontakDarurat = Bersihkan(input.KontakDarurat);
        member.CatatanKesehatan = Bersihkan(input.CatatanKesehatan);
    }

    private static string? Bersihkan(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? PanjangMaks(string? value, int maks, string label) =>
        value?.Trim().Length > maks ? $"{label} maksimal {maks} karakter." : null;

    private bool BolehKelola() => currentUser.IsInRole(UserRole.Admin, UserRole.Resepsionis);

    private DateOnly HariIni => DateOnly.FromDateTime(time.GetLocalNow().DateTime);
}
