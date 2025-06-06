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
    /// The internal merchant ID (GUID) to generate the API key for
    /// </summary>
    [Required]
    [JsonConverter(typeof(GuidConverter))]
    public Guid MerchantId { get; set; }

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
    /// Purpose of the API key (e.g., 'PRODUCTION', 'TESTING', 'INTEGRATION')
    /// </summary>
    [StringLength(50)]
    public string? Purpose { get; set; }

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