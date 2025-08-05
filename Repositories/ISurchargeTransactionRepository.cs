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
    /// Gets a surcharge transaction by its ID with merchant filtering (for secure access)
    /// </summary>
    Task<SurchargeTransaction?> GetByIdForMerchantAsync(Guid id, Guid merchantId);

    /// <summary>
    /// Gets a surcharge transaction by correlation ID
    /// </summary>
    Task<SurchargeTransaction?> GetByCorrelationIdAsync(string correlationId);

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
    /// Checks if a correlation ID already exists
    /// </summary>
    Task<bool> ExistsByCorrelationIdAsync(string correlationId);

    /// <summary>
    /// Gets transaction statistics for a merchant
    /// </summary>
    Task<object> GetTransactionStatisticsAsync(Guid merchantId, DateTime? fromDate = null, DateTime? toDate = null);

    /// <summary>
    /// Gets a surcharge transaction by provider transaction ID
    /// </summary>
    Task<SurchargeTransaction?> GetByProviderTransactionIdAsync(string providerTransactionId);

    /// <summary>
    /// Gets a surcharge transaction by provider transaction ID and correlation ID (for follow-up validation)
    /// </summary>
    Task<SurchargeTransaction?> GetByProviderTransactionIdAndCorrelationIdAsync(string providerTransactionId, string correlationId);

    /// <summary>
    /// Gets a surcharge transaction by provider transaction ID and correlation ID with merchant filtering (for secure follow-up validation)
    /// </summary>
    Task<SurchargeTransaction?> GetByProviderTransactionIdAndCorrelationIdForMerchantAsync(string providerTransactionId, string correlationId, Guid merchantId);

    /// <summary>
    /// Gets the latest transaction in the original_surcharge_trans_id chain for a given transaction
    /// </summary>
    Task<SurchargeTransaction?> GetLatestInOriginalChainAsync(Guid rootTransactionId, Guid merchantId);

    /// <summary>
    /// Gets the latest transaction in the original_surcharge_trans_id chain for a given providerTransactionId
    /// </summary>
    Task<SurchargeTransaction?> GetLatestInProviderTransactionChainAsync(string providerTransactionId, Guid merchantId);

    /// <summary>
    /// Gets a surcharge transaction by its ID with provider configuration eager loading
    /// </summary>
    Task<SurchargeTransaction?> GetByIdWithProviderConfigAsync(Guid id);

    /// <summary>
    /// Gets all refund transactions that point to a specific original transaction ID
    /// </summary>
    Task<List<SurchargeTransaction>> GetRefundsByOriginalTransactionIdAsync(Guid originalTransactionId);
}
