using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Custom validation attribute to ensure either SurchargeTransactionId is provided, or all of ProviderTransactionId, CorrelationId, and ProviderCode are present
/// </summary>
public class RequireSurchargeRefundIdentifiersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not SurchargeRefundRequest request)
        {
            return new ValidationResult("Invalid request object type.");
        }
        // If SurchargeTransactionId is provided, all other fields are optional
        if (request.SurchargeTransactionId.HasValue)
        {
            return ValidationResult.Success;
        }
        // If ProviderTransactionId is provided, require CorrelationId and ProviderCode
        if (!string.IsNullOrWhiteSpace(request.ProviderTransactionId))
        {
            if (string.IsNullOrWhiteSpace(request.CorrelationId))
                return new ValidationResult("CorrelationId is required if ProviderTransactionId is provided.");
            if (string.IsNullOrWhiteSpace(request.ProviderCode))
                return new ValidationResult("ProviderCode is required if ProviderTransactionId is provided.");
            return ValidationResult.Success;
        }
        return new ValidationResult("Either surchargeTransactionId or all of providerTransactionId, correlationId, and providerCode must be provided.");
    }
}

/// <summary>
/// Request model for refund surcharge operations
/// </summary>
/// <remarks>
/// If <c>surchargeTransactionId</c> is provided, all other fields are optional and will be looked up from the database.
/// If <c>providerTransactionId</c> is provided, the following fields are required: <c>correlationId</c>, <c>providerCode</c>.
/// </remarks>
[RequireSurchargeRefundIdentifiers]
public class SurchargeRefundRequest
{
    /// <summary>
    /// Surcharge transaction ID (internal GUID, preferred)
    /// </summary>
    public Guid? SurchargeTransactionId { get; set; }

    /// <summary>
    /// Provider transaction ID (InterPayments sTxId, alternative lookup)
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Correlation identifier for linking related transactions (required if ProviderTransactionId is provided)
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Provider code for validation (required if ProviderTransactionId is provided)
    /// </summary>
    public string? ProviderCode { get; set; }

    /// <summary>
    /// Transaction amount to refund
    /// </summary>
    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Merchant transaction identifier for the refund
    /// </summary>
    public string? MerchantTransactionId { get; set; }

    /// <summary>
    /// Reason for the refund
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// Tokenized card information
    /// </summary>
    public string? CardToken { get; set; }

    /// <summary>
    /// Additional data for the refund (optional)
    /// </summary>
    public List<string>? Data { get; set; }
}
