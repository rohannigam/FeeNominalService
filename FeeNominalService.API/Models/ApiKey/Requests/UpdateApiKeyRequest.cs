using System.ComponentModel.DataAnnotations;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Models.ApiKey.Requests;

/// <summary>
/// Request model for updating an API key
/// </summary>
public class UpdateApiKeyRequest
{
    /// <summary>
    /// The merchant ID associated with the API key to update
    /// </summary>
    [Required]
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// The API key to update
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Description of the API key
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Rate limit for the API key
    /// </summary>
    [Range(1, 10000)]
    public int? RateLimit { get; set; }

    /// <summary>
    /// List of allowed endpoints
    /// </summary>
    public string[]? AllowedEndpoints { get; set; }

    /// <summary>
    /// Onboarding metadata for the API key
    /// </summary>
    [Required]
    public OnboardingMetadata OnboardingMetadata { get; set; } = new();
} 