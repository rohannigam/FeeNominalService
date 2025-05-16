using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Configuration;

/// <summary>
/// Configuration model for API key settings
/// </summary>
public class ApiKeyConfiguration
{
    /// <summary>
    /// The name of the secret in AWS Secrets Manager
    /// </summary>
    [Required]
    public string SecretName { get; set; } = string.Empty;

    /// <summary>
    /// The AWS region where secrets are stored
    /// </summary>
    [Required]
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// The VPC endpoint ID for AWS Secrets Manager
    /// </summary>
    public string VpcEndpointId { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of failed authentication attempts before lockout
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxFailedAttempts { get; set; }

    /// <summary>
    /// Duration of lockout in minutes after max failed attempts
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int LockoutDurationMinutes { get; set; }

    /// <summary>
    /// Number of days before API key rotation is required
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int KeyRotationDays { get; set; }

    /// <summary>
    /// Whether rate limiting is enabled
    /// </summary>
    [Required]
    public bool EnableRateLimiting { get; set; }

    /// <summary>
    /// Default rate limit for new API keys
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int DefaultRateLimit { get; set; }

    /// <summary>
    /// Request time window in minutes
    /// </summary>
    public int RequestTimeWindowMinutes { get; set; } = 5;
} 