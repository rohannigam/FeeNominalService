using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.ApiKey.Requests;

/// <summary>
/// Request model for validating an API key
/// </summary>
public class ValidateApiKeyRequest
{
    /// <summary>
    /// The merchant ID to validate
    /// </summary>
    [Required]
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp of the request
    /// </summary>
    [Required]
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>
    /// The nonce for the request
    /// </summary>
    [Required]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// The request body
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// The signature to validate
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;
} 