using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Core.Services;

/// <summary>FR-09: kelola paket membership. Hanya Admin.</summary>
public sealed class PlanService(IPlanRepository plans, ICurrentUserService currentUser) : IPlanService
{
    /// <summary>Sama dengan panjang kolom Deskripsi.</summary>
    public const int MaksPanjangDeskripsi = 255;

    private const string HanyaAdmin = "Hanya Admin yang dapat mengelola paket.";

    public async Task<OperationResult> CreateAsync(PlanInput input, CancellationToken ct = default)
    {
        if (!IsAdmin()) return OperationResult.Fail(HanyaAdmin);
        if (Validasi(input) is { } error) return OperationResult.Fail(error);

        var nama = input.NamaPaket.Trim();
        if (await plans.IsNamaTakenAsync(nama, null, ct))
            return OperationResult.Fail("Nama paket sudah dipakai.");

        await plans.AddAsync(new MembershipPlan
        {
            NamaPaket = nama,
            DurasiHari = input.DurasiHari,
            Harga = input.Harga,
            Deskripsi = Bersihkan(input.Deskripsi),
            IsActive = true,
        }, ct);
        return OperationResult.Ok($"Paket {nama} berhasil ditambahkan.");
    }

    public async Task<OperationResult> UpdateAsync(int planId, PlanInput input, CancellationToken ct = default)
    {
        if (!IsAdmin()) return OperationResult.Fail(HanyaAdmin);
        if (Validasi(input) is { } error) return OperationResult.Fail(error);

        var plan = await plans.GetByIdAsync(planId, ct);
        if (plan is null) return OperationResult.Fail("Paket tidak ditemukan.");

        var nama = input.NamaPaket.Trim();
        if (await plans.IsNamaTakenAsync(nama, planId, ct))
            return OperationResult.Fail("Nama paket sudah dipakai.");

        // Riwayat transaksi tidak berubah: HargaPaket dan TglBerakhir sudah tersalin di tiap transaksi (PRD 7.4).
        plan.NamaPaket = nama;
        plan.DurasiHari = input.DurasiHari;
        plan.Harga = input.Harga;
        plan.Deskripsi = Bersihkan(input.Deskripsi);
        await plans.UpdateAsync(plan, ct);
        return OperationResult.Ok("Paket berhasil disimpan.");
    }

    public async Task<OperationResult> SetActiveAsync(int planId, bool isActive, CancellationToken ct = default)
    {
        if (!IsAdmin()) return OperationResult.Fail(HanyaAdmin);
        if (!await plans.SetActiveAsync(planId, isActive, ct))
            return OperationResult.Fail("Paket tidak ditemukan.");
        return OperationResult.Ok(isActive
            ? "Paket diaktifkan kembali."
            : "Paket dinonaktifkan dan tidak lagi muncul di transaksi baru.");
    }

    public async Task<OperationResult> DeleteAsync(int planId, CancellationToken ct = default)
    {
        if (!IsAdmin()) return OperationResult.Fail(HanyaAdmin);
        if (await plans.IsUsedAsync(planId, ct))
            return OperationResult.Fail("Paket sudah dipakai transaksi, jadi tidak bisa dihapus. Nonaktifkan saja.");
        if (!await plans.TryDeleteAsync(planId, ct))
            return OperationResult.Fail("Paket tidak ditemukan atau sudah dipakai transaksi.");
        return OperationResult.Ok("Paket dihapus.");
    }

    private static string? Validasi(PlanInput input) =>
        MasterDataRules.ValidasiNamaPaket(input.NamaPaket)
        ?? MasterDataRules.ValidasiDurasiHari(input.DurasiHari)
        ?? MasterDataRules.ValidasiHarga(input.Harga)
        ?? (input.Deskripsi?.Trim().Length > MaksPanjangDeskripsi
            ? $"Deskripsi maksimal {MaksPanjangDeskripsi} karakter."
            : null);

    private static string? Bersihkan(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private bool IsAdmin() => currentUser.IsInRole(UserRole.Admin);
}
