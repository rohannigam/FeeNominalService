using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Request model for cancel surcharge operations
/// </summary>
public class SurchargeCancelRequest
{
    /// <summary>
    /// Correlation identifier for linking related transactions
    /// </summary>
    [Required(ErrorMessage = "Correlation ID is required")]
    public required string CorrelationId { get; set; }

    /// <summary>
    /// Merchant transaction identifier for the cancellation
    /// </summary>
    public string? MerchantTransactionId { get; set; }

    /// <summary>
    /// Original transaction ID that is being cancelled
    /// </summary>
    [Required(ErrorMessage = "Original transaction ID is required")]
    public required string OriginalTransactionId { get; set; }

    /// <summary>
    /// Reason for the cancellation
    /// </summary>
    public string? CancelReason { get; set; }
}
