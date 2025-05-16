using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models;

/// <summary>
/// Represents a request to calculate surcharge fees for a transaction
/// </summary>
public class SurchargeRequest
{
    /// <summary>
    /// Network Interchange Card Number (NICN) for the transaction
    /// </summary>
    [Required(ErrorMessage = "NICN is required")]
    public string? nicn { get; set; }

    /// <summary>
    /// Payment processor for the transaction (e.g., VISA, MASTERCARD)
    /// </summary>
    [Required(ErrorMessage = "Processor is required")]
    public string? processor { get; set; }

    /// <summary>
    /// Transaction amount before surcharge
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? amount { get; set; }

    /// <summary>
    /// Total transaction amount including surcharge
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "Total amount must be greater than 0")]
    public decimal? totalAmount { get; set; }

    /// <summary>
    /// Country code for the transaction
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Country must be a 2-letter code")]
    public string? country { get; set; }

    /// <summary>
    /// Region or state code for the transaction
    /// </summary>
    [StringLength(2, MinimumLength = 2, ErrorMessage = "Region must be a 2-letter code")]
    public string? region { get; set; }

    /// <summary>
    /// List of campaign identifiers associated with the transaction
    /// </summary>
    public List<string>? campaign { get; set; }

    /// <summary>
    /// Additional data points for the transaction
    /// </summary>
    public List<string>? data { get; set; }

    /// <summary>
    /// System transaction identifier
    /// </summary>
    [Required(ErrorMessage = "System transaction ID is required")]
    public string? sTxId { get; set; }

    /// <summary>
    /// Merchant transaction identifier
    /// </summary>
    public string? mTxId { get; set; }

    /// <summary>
    /// Tokenized card information
    /// </summary>
    public string? cardToken { get; set; }

    /// <summary>
    /// Method used to enter the card information
    /// </summary>
    [Required(ErrorMessage = "Entry method is required")]
    public EntryMethod? entryMethod { get; set; }

    /// <summary>
    /// Amount that should not be subject to surcharge
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Non-surchargable amount cannot be negative")]
    public decimal? nonSurchargableAmount { get; set; }
} 