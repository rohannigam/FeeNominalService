using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Request model for refund surcharge operations
/// </summary>
public class SurchargeRefundRequest
{
    /// <summary>
    /// System transaction identifier for the refund
    /// </summary>
    [Required(ErrorMessage = "System transaction ID is required")]
    public required string SystemTransactionId { get; set; }

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
    /// Refund amount
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code for the refund
    /// </summary>
    [Required(ErrorMessage = "Currency is required")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency must be a 3-letter code")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Tokenized card information
    /// </summary>
    public string? CardToken { get; set; }
}
