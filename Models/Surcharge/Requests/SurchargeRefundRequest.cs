using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Request model for refund surcharge operations
/// </summary>
public class SurchargeRefundRequest
{
    /// <summary>
    /// Transaction amount to refund
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Correlation identifier for linking related transactions
    /// </summary>
    [Required(ErrorMessage = "Correlation ID is required")]
    public required string CorrelationId { get; set; }

    /// <summary>
    /// Merchant transaction identifier for the refund
    /// </summary>
    public string? MerchantTransactionId { get; set; }

    /// <summary>
    /// Original transaction ID that is being refunded
    /// </summary>
    [Required(ErrorMessage = "Original transaction ID is required")]
    public required string OriginalTransactionId { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Tokenized card information
    /// </summary>
    public string? CardToken { get; set; }
}
