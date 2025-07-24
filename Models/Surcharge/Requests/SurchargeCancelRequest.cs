using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Custom validation attribute to ensure either SurchargeTransactionId is provided, or all of ProviderTransactionId, CorrelationId, ProviderType, and ProviderCode are present
/// </summary>
public class RequireSurchargeCancelIdentifiersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not SurchargeCancelRequest request)
        {
            return new ValidationResult("Invalid request object type.");
        }
        // If SurchargeTransactionId is provided, all other fields are optional
        if (request.SurchargeTransactionId.HasValue)
        {
            return ValidationResult.Success;
        }
        // If ProviderTransactionId is provided, require CorrelationId and ProviderCode (ProviderType is optional)
        if (!string.IsNullOrWhiteSpace(request.ProviderTransactionId))
        {
            if (string.IsNullOrWhiteSpace(request.CorrelationId))
                return new ValidationResult("CorrelationId is required if ProviderTransactionId is provided.");
            if (string.IsNullOrWhiteSpace(request.ProviderCode))
                return new ValidationResult("ProviderCode is required if ProviderTransactionId is provided.");
            // ProviderType is optional
            return ValidationResult.Success;
        }
        return new ValidationResult("Either surchargeTransactionId or all of providerTransactionId, correlationId, providerType, and providerCode must be provided.");
    }
}

/// <summary>
/// Request model for cancel surcharge operations
/// </summary>
/// <remarks>
/// If <c>surchargeTransactionId</c> is provided, all other fields are optional and will be looked up from the database.
/// If <c>providerTransactionId</c> is provided, the following fields are required: <c>correlationId</c>, <c>providerCode</c>, <c>providerType</c>.
/// </remarks>
[RequireSurchargeCancelIdentifiers]
public class SurchargeCancelRequest
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
    /// Merchant transaction identifier (optional)
    /// </summary>
    public string? MerchantTransactionId { get; set; }

    /// <summary>
    /// Provider code for validation (required if ProviderTransactionId is provided)
    /// </summary>
    public string? ProviderCode { get; set; }

    /// <summary>
    /// Provider type for validation (required if ProviderTransactionId is provided)
    /// </summary>
    public string? ProviderType { get; set; }

    /// <summary>
    /// Card token (optional)
    /// </summary>
    public string? CardToken { get; set; }

    /// <summary>
    /// Reason code for cancellation (optional)
    /// </summary>
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Additional data for cancellation (optional)
    /// </summary>
    public List<string>? Data { get; set; }

    /// <summary>
    /// Auth code (optional)
    /// </summary>
    public string? AuthCode { get; set; }
}

