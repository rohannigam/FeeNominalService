using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Configuration;

/// <summary>
/// Configuration model for AWS Secrets Manager settings
/// </summary>
public class AwsSecretsManagerConfiguration
{
    /// <summary>
    /// The AWS region where secrets are stored
    /// </summary>
    [Required]
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// The AWS profile to use for authentication
    /// </summary>
    public string Profile { get; set; } = "default";

    /// <summary>
    /// The format for merchant secret names
    /// Supports placeholders: {merchantId}, {apiKey}
    /// </summary>
    [Required]
    public string MerchantSecretNameFormat { get; set; } = "feenominal/merchants/{merchantId}/apikeys/{apiKey}";

    /// <summary>
    /// The format for admin secret names
    /// Supports placeholders: {serviceName}
    /// </summary>
    [Required]
    public string AdminSecretNameFormat { get; set; } = "feenominal/admin/apikeys/{serviceName}-admin-api-key-secret";
} 