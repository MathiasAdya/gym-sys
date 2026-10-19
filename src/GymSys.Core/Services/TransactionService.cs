using GymSys.Core.Entities;
using GymSys.Core.Interfaces;

namespace GymSys.Core.Services;

/// <summary>FR-06: transaksi baru. Semua aturan tanggal dan harga lewat MembershipCalculator.</summary>
public sealed class TransactionService(ITransactionRepository transactions, ICurrentUserService currentUser, TimeProvider time)
    : ITransactionService
{
    /// <summary>Sama dengan panjang kolom AlasanBatal.</summary>
    public const int MaksPanjangAlasan = 255;

    public async Task<TransactionResult> CreateAsync(NewTransactionRequest request, CancellationToken ct = default)
    {
        var kasir = currentUser.User;
        if (kasir is null || !currentUser.IsInRole(UserRole.Admin, UserRole.Resepsionis))
            return TransactionResult.Fail("Hanya Admin dan Resepsionis yang dapat mencatat transaksi.");
        if (!Enum.IsDefined(request.MetodeBayar))
            return TransactionResult.Fail("Metode bayar tidak valid.");

        var member = await transactions.GetMemberAsync(request.MemberId, ct);
        if (member is null || !member.IsActive)
            return TransactionResult.Fail("Member tidak ditemukan atau sudah nonaktif.");

        var plan = await transactions.GetPlanAsync(request.PlanId, ct);
        if (plan is null || !plan.IsActive)
            return TransactionResult.Fail("Paket tidak ditemukan atau sudah nonaktif.");

        if (request.Diskon < 0 || request.Diskon > plan.Harga)
            return TransactionResult.Fail($"Diskon harus antara 0 dan harga paket ({plan.Harga:N0}).");

        var sekarang = time.GetLocalNow().DateTime;
        var hariIni = DateOnly.FromDateTime(sekarang);
        var berakhirTerakhir = await transactions.GetTglBerakhirTerakhirAsync(member.MemberId, ct);
        var isAdmin = currentUser.IsInRole(UserRole.Admin);
        if (MembershipCalculator.ValidasiTglMulai(request.TglMulai, berakhirTerakhir, hariIni, isAdmin) is { } error)
            return TransactionResult.Fail(error);

        var transaksi = new Transaction
        {
            MemberId = member.MemberId,
            PlanId = plan.PlanId,
            UserIdKasir = kasir.UserId,
            TglTransaksi = sekarang,
            TglMulaiAktif = request.TglMulai,
            TglBerakhir = MembershipCalculator.HitungTglBerakhir(request.TglMulai, plan.DurasiHari),
            HargaPaket = plan.Harga,
            Diskon = request.Diskon,
            TotalBayar = MembershipCalculator.HitungTotalBayar(plan.Harga, request.Diskon),
            MetodeBayar = request.MetodeBayar,
            Status = StatusTransaksi.Lunas,
        };

        if (!await transactions.TryAddAsync(transaksi, berakhirTerakhir, ct))
            return TransactionResult.Fail("Masa aktif member baru saja berubah oleh transaksi lain. Pilih ulang member lalu coba lagi.");

        return TransactionResult.Ok($"Transaksi tersimpan dengan nomor kuitansi {transaksi.NoKuitansi}.", transaksi);
    }

    public async Task<OperationResult> CancelAsync(int transactionId, string alasan, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole(UserRole.Admin))
            return OperationResult.Fail("Hanya Admin yang dapat membatalkan transaksi.");

        alasan = alasan.Trim();
        if (alasan.Length == 0)
            return OperationResult.Fail("Alasan pembatalan wajib diisi.");
        if (alasan.Length > MaksPanjangAlasan)
            return OperationResult.Fail($"Alasan pembatalan maksimal {MaksPanjangAlasan} karakter.");

        return await transactions.TryCancelAsync(transactionId, alasan, ct)
            ? OperationResult.Ok("Transaksi dibatalkan. Masa aktif member dihitung ulang tanpa transaksi ini.")
            : OperationResult.Fail("Transaksi tidak ditemukan atau sudah dibatalkan.");
    }
}
