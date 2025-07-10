using System.ComponentModel.DataAnnotations;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Models.ApiKey.Requests;

public class GenerateInitialApiKeyRequest
{
    [Required]
    [StringLength(50, ErrorMessage = "ExternalMerchantId cannot exceed 50 characters")]
    public string ExternalMerchantId { get; set; } = string.Empty;

    [Required]
    [StringLength(255, ErrorMessage = "MerchantName cannot exceed 255 characters")]
    public string MerchantName { get; set; } = string.Empty;

    public Guid? ExternalMerchantGuid { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [Range(1, 10000, ErrorMessage = "RateLimit must be between 1 and 10000")]
    public int? RateLimit { get; set; }

    public string[]? AllowedEndpoints { get; set; }

    [StringLength(255, ErrorMessage = "Purpose cannot exceed 255 characters")]
    public string? Purpose { get; set; }

    /// <summary>
    /// Number of days until the API key expires (default: 365)
    /// </summary>
    [Range(1, 3650, ErrorMessage = "ExpirationDays must be between 1 and 3650 days")]
    public int? ExpirationDays { get; set; }

    /// <summary>
    /// Metadata about the onboarding process
    /// </summary>
    [Required]
    public OnboardingMetadata OnboardingMetadata { get; set; } = new();
} 