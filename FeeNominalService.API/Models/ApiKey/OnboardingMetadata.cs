using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.ApiKey;

/// <summary>
/// Metadata about the onboarding process for API keys
/// </summary>
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