using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FeeNominalService.Models.ApiKey.Converters;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Models.ApiKey.Requests;

/// <summary>
/// Request model for generating a new API key
/// </summary>
public class GenerateApiKeyRequest
{
    /// <summary>
    /// The internal merchant ID (GUID) to generate the API key for (null for admin keys)
    /// </summary>
    [JsonConverter(typeof(NullableGuidConverter))]
    public Guid? MerchantId { get; set; }

    /// <summary>
    /// Optional description for the API key
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional rate limit for the API key
    /// </summary>
    [Range(1, 10000)]
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
    /// Optional unique name for the API key (must be unique per merchant)
    /// </summary>
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// Number of days until the API key expires (default: 365)
    /// </summary>
    [Range(1, 3650, ErrorMessage = "ExpirationDays must be between 1 and 3650 days")]
    public int? ExpirationDays { get; set; }

    /// <summary>
    /// Whether this API key is an admin/superuser key (global cross-merchant access)
    /// </summary>
    public bool IsAdmin { get; set; } = false;

    /// <summary>
    /// Metadata about the onboarding process (not required for admin API keys)
    /// </summary>
    public OnboardingMetadata? OnboardingMetadata { get; set; } = null;

    /// <summary>
    /// Service name for admin API keys (used in secret naming)
    /// </summary>
    [StringLength(50)]
    public string? ServiceName { get; set; }
} 