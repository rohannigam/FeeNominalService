using FeeNominalService.Models;
using System.Text.Json;

namespace FeeNominalService.Repositories;

/// <summary>
/// Repository interface for managing surcharge transactions
/// </summary>
public interface ISurchargeTransactionRepository
{
    /// <summary>
    /// Creates a new surcharge transaction
    /// </summary>
    Task<SurchargeTransaction> CreateAsync(SurchargeTransaction transaction);

    /// <summary>
    /// Gets a surcharge transaction by its ID
    /// </summary>
    Task<SurchargeTransaction?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets a surcharge transaction by source transaction ID
    /// </summary>
    Task<SurchargeTransaction?> GetBySourceTransactionIdAsync(string sourceTransactionId);

    /// <summary>
    /// Gets surcharge transactions for a merchant with pagination
    /// </summary>
    Task<(List<SurchargeTransaction> Transactions, int TotalCount)> GetByMerchantIdAsync(
        Guid merchantId, 
        int page = 1, 
        int pageSize = 20,
        SurchargeOperationType? operationType = null,
        SurchargeTransactionStatus? status = null);

    /// <summary>
    /// Gets surcharge transactions by status
    /// </summary>
    Task<List<SurchargeTransaction>> GetByStatusAsync(SurchargeTransactionStatus status);

    /// <summary>
    /// Gets surcharge transactions by provider configuration ID
    /// </summary>
    Task<List<SurchargeTransaction>> GetByProviderConfigIdAsync(Guid providerConfigId);

    /// <summary>
    /// Updates a surcharge transaction
    /// </summary>
    Task<SurchargeTransaction> UpdateAsync(SurchargeTransaction transaction);

    /// <summary>
    /// Updates the status of a surcharge transaction
    /// </summary>
    Task<bool> UpdateStatusAsync(Guid id, SurchargeTransactionStatus status, string? errorMessage = null);

    /// <summary>
    /// Updates the response payload and processed timestamp
    /// </summary>
    Task<bool> UpdateResponseAsync(Guid id, JsonDocument responsePayload, DateTime processedAt);

    /// <summary>
    /// Checks if a source transaction ID already exists
    /// </summary>
    Task<bool> ExistsBySourceTransactionIdAsync(string sourceTransactionId);

    /// <summary>
    /// Gets transaction statistics for a merchant
    /// </summary>
    Task<object> GetTransactionStatisticsAsync(Guid merchantId, DateTime? fromDate = null, DateTime? toDate = null);
}
