namespace FeeNominalService.Models.Surcharge.Responses;

/// <summary>
/// Response model for authorization surcharge operations
/// </summary>
public class SurchargeAuthResponse
{
    /// <summary>
    /// Unique identifier for the surcharge transaction
    /// </summary>
    public required Guid SurchargeTransactionId { get; set; }

    /// <summary>
    /// Correlation identifier for linking related transactions
    /// </summary>
    public required string CorrelationId { get; set; }

    /// <summary>
    /// Merchant transaction identifier
    /// </summary>
    public string? MerchantTransactionId { get; set; }

    /// <summary>
    /// Provider transaction identifier (e.g., Interpayments sTxId)
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Original transaction amount before surcharge
    /// </summary>
    public required decimal OriginalAmount { get; set; }

    /// <summary>
    /// Surcharge amount calculated
    /// </summary>
    public required decimal SurchargeAmount { get; set; }

    /// <summary>
    /// Total amount including surcharge
    /// </summary>
    public required decimal TotalAmount { get; set; }

    /// <summary>
    /// Status of the surcharge transaction
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Provider that processed the surcharge (legacy field for backward compatibility)
    /// </summary>
    public required string Provider { get; set; }

    /// <summary>
    /// Provider type (e.g., 'INTERPAYMENTS', 'OTHERPROVIDER')
    /// </summary>
    public required string ProviderType { get; set; }

    /// <summary>
    /// Provider code (e.g., 'INTERPAYMENTS', 'XIPAY')
    /// </summary>
    public required string ProviderCode { get; set; }

    /// <summary>
    /// When the surcharge was processed
    /// </summary>
    public required DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Percent fee charged by the provider (if available)
    /// </summary>
    public decimal? SurchargeFeePercent { get; set; }
}
