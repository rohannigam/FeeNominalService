using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FeeNominalService.Models.ApiKey.Converters;

namespace FeeNominalService.Models.ApiKey.Requests;

/// <summary>
/// Request model for generating a new API key
/// </summary>
public class GenerateApiKeyRequest
{
    /// <summary>
    /// The external ID of the merchant to generate the API key for (e.g., 'DEV001')
    /// </summary>
    [Required]
    [StringLength(50)]
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// The merchant name to generate the API key for
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Optional description for the API key
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional rate limit for the API key
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? RateLimit { get; set; }

    /// <summary>
    /// List of allowed endpoints (comma-separated string or array)
    /// </summary>
    [JsonConverter(typeof(AllowedEndpointsConverter))]
    public string[]? AllowedEndpoints { get; set; }

    /// <summary>
    /// Metadata about the onboarding process
    /// </summary>
    [Required]
    public OnboardingMetadata OnboardingMetadata { get; set; } = new();
}

public class OnboardingMetadata
{
    /// <summary>
    /// The admin user ID for the API key
    /// </summary>
    [Required]
    public string AdminUserId { get; set; } = string.Empty;

    /// <summary>
    /// The onboarding reference for the API key
    /// </summary>
    [Required]
    public string OnboardingReference { get; set; } = string.Empty;

    /// <summary>
    /// The onboarding timestamp for the API key
    /// </summary>
    public DateTime OnboardingTimestamp { get; set; } = DateTime.UtcNow;
} 