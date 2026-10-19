using GymSys.Core.Entities;

namespace GymSys.Core.Interfaces;

public sealed record NewTransactionRequest(int MemberId, int PlanId, DateOnly TglMulai, decimal Diskon, MetodeBayar MetodeBayar);

public sealed record TransactionResult(bool Success, string Message, Transaction? Transaction)
{
    public static TransactionResult Ok(string message, Transaction transaction) => new(true, message, transaction);
    public static TransactionResult Fail(string message) => new(false, message, null);
}

/// <summary>Aturan transaksi PRD Bagian 7 (FR-06). Peran dicek di sini, bukan hanya di menu.</summary>
public interface ITransactionService
{
    Task<TransactionResult> CreateAsync(NewTransactionRequest request, CancellationToken ct = default);

    /// <summary>
    /// FR-07: hanya Admin, alasan wajib. Status menjadi Batal; masa aktif member otomatis dihitung ulang
    /// karena status member hanya memakai transaksi Lunas (PRD 7.3).
    /// </summary>
    Task<OperationResult> CancelAsync(int transactionId, string alasan, CancellationToken ct = default);
}
