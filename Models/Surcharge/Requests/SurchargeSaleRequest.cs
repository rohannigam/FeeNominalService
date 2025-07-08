using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Request model for surcharge sale operations
/// </summary>
public class SurchargeSaleRequest
{
    /// <summary>
    /// Bank Identification Number (BIN) value for the transaction
    /// </summary>
    [Required(ErrorMessage = "BIN value is required")]
    public required string BinValue { get; set; }

    /// <summary>
    /// Surcharge processor configuration identifier
    /// </summary>
    [Required(ErrorMessage = "Surcharge processor is required")]
    public required string SurchargeProcessor { get; set; }

    /// <summary>
    /// Transaction amount in cents
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Total transaction amount including surcharge
    /// </summary>
    [Required(ErrorMessage = "Total amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Country code for the transaction (e.g., USA, CAN)
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [StringLength(3, MinimumLength = 2, ErrorMessage = "Country must be 2-3 characters")]
    public required string Country { get; set; }

    /// <summary>
    /// Postal code for the transaction (e.g., US ZIP, Canadian Postal, etc.)
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Campaign identifiers for surcharge calculation
    /// </summary>
    public List<string>? Campaign { get; set; }

    /// <summary>
    /// Additional data for surcharge calculation
    /// </summary>
    public List<string>? Data { get; set; }

    /// <summary>
    /// Tokenized card information
    /// </summary>
    public string? CardToken { get; set; }

    /// <summary>
    /// Entry method for the transaction
    /// </summary>
    public string? EntryMethod { get; set; }

    /// <summary>
    /// Non-surchargable amount in cents
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Non-surchargable amount cannot be negative")]
    public decimal? NonSurchargableAmount { get; set; }

    /// <summary>
    /// Provider transaction ID for follow-up operations
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Provider code for the surcharge provider
    /// </summary>
    [Required(ErrorMessage = "Provider code is required")]
    public required string ProviderCode { get; set; }

    /// <summary>
    /// Correlation identifier for linking related transactions
    /// </summary>
    [Required(ErrorMessage = "Correlation ID is required")]
    public required string CorrelationId { get; set; }

    /// <summary>
    /// Merchant transaction identifier
    /// </summary>
    public string? MerchantTransactionId { get; set; }
}
