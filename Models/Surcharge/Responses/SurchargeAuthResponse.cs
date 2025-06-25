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
    /// System transaction identifier
    /// </summary>
    public required string SystemTransactionId { get; set; }

    /// <summary>
    /// Merchant transaction identifier
    /// </summary>
    public string? MerchantTransactionId { get; set; }

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
    /// Currency code
    /// </summary>
    public required string Currency { get; set; }

    /// <summary>
    /// Status of the surcharge transaction
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Provider that processed the surcharge
    /// </summary>
    public required string Provider { get; set; }

    /// <summary>
    /// When the surcharge was processed
    /// </summary>
    public required DateTime ProcessedAt { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
