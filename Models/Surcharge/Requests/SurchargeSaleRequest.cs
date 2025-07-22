using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Surcharge.Requests;

/// <summary>
/// Custom validation attribute to ensure either SurchargeTransactionId is provided, or all required fields are present
/// </summary>
public class RequireSurchargeSaleIdentifiersAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is not SurchargeSaleRequest request)
        {
            return new ValidationResult("Invalid request object type.");
        }
        // If SurchargeTransactionId is provided, all other fields are optional
        if (request.SurchargeTransactionId.HasValue)
        {
            return ValidationResult.Success;
        }
        // Otherwise, require CorrelationId, ProviderCode, ProviderType
        if (string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            return new ValidationResult("CorrelationId is required if SurchargeTransactionId is not provided.");
        }
        if (string.IsNullOrWhiteSpace(request.ProviderCode))
        {
            return new ValidationResult("ProviderCode is required if SurchargeTransactionId is not provided.");
        }
        if (string.IsNullOrWhiteSpace(request.ProviderType))
        {
            return new ValidationResult("ProviderType is required if SurchargeTransactionId is not provided.");
        }
        return ValidationResult.Success;
    }
}

/// <summary>
/// Request to complete a surcharge sale transaction.
/// </summary>
/// <remarks>
/// If <c>surchargeTransactionId</c> is provided, all other fields become optional and will be looked up from the database.
/// If <c>surchargeTransactionId</c> is not provided, the following fields are required: <c>providerTransactionId</c>, <c>providerCode</c>, <c>providerType</c>, <c>correlationId</c>.
/// </remarks>
/// <example>
/// // Using surchargeTransactionId (preferred):
/// {
///   "surchargeTransactionId": "b1e2c3d4-e5f6-7890-abcd-1234567890ef"
/// }
/// // Using provider info:
/// {
///   "providerTransactionId": "ip-tx-001",
///   "providerCode": "INTERPAY",
///   "providerType": "INTERPAYMENTS",
///   "correlationId": "sale-123456"
/// }
/// </example>
[RequireSurchargeSaleIdentifiers]
public class SurchargeSaleRequest
{
    /// <summary>
    /// Surcharge transaction ID (auth) to complete (optional)
    /// </summary>
    public Guid? SurchargeTransactionId { get; set; }

    /// <summary>
    /// Correlation identifier for linking related transactions (optional if SurchargeTransactionId is provided)
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Merchant transaction identifier (optional)
    /// </summary>
    public string? MerchantTransactionId { get; set; }

    /// <summary>
    /// Provider transaction ID (e.g., InterPayments sTxId) for follow-up operations (optional)
    /// </summary>
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// Provider type for validation (optional if SurchargeTransactionId is provided)
    /// </summary>
    public string? ProviderType { get; set; }

    /// <summary>
    /// Provider code for validation (optional if SurchargeTransactionId is provided)
    /// </summary>
    public string? ProviderCode { get; set; }
}
