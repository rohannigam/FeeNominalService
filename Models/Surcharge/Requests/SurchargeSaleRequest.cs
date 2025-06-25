using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Request model for sale surcharge operations
/// </summary>
public class SurchargeSaleRequest
{
    /// <summary>
    /// Network Interchange Card Number (NICN) for the transaction
    /// </summary>
    [Required(ErrorMessage = "NICN is required")]
    public required string Nicn { get; set; }

    /// <summary>
    /// Processor configuration identifier
    /// </summary>
    [Required(ErrorMessage = "Processor is required")]
    public required string Processor { get; set; }

    /// <summary>
    /// Transaction amount before surcharge
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Total transaction amount including surcharge
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Country code for the transaction
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a 2-letter code")]
    public required string Country { get; set; }

    /// <summary>
    /// Region or state code for the transaction
    /// </summary>
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Region must be a 2-letter code")]
    public string? Region { get; set; }

    /// <summary>
    /// System transaction identifier
    /// </summary>
    [Required(ErrorMessage = "System transaction ID is required")]
    public required string SystemTransactionId { get; set; }

    /// <summary>
    /// Merchant transaction identifier
    /// </summary>
    public string? MerchantTransactionId { get; set; }

    /// <summary>
    /// Tokenized card information
    /// </summary>
    public string? CardToken { get; set; }

    /// <summary>
    /// Method used to enter the card information
    /// </summary>
    [Required(ErrorMessage = "Entry method is required")]
    public EntryMethod EntryMethod { get; set; }

    /// <summary>
    /// Authorization transaction ID (for sale operations)
    /// </summary>
    [Required(ErrorMessage = "Authorization transaction ID is required for sale operations")]
    public required string AuthorizationTransactionId { get; set; }
}
