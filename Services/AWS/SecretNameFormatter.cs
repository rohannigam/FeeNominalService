using FeeNominalService.Models.Configuration;
using Microsoft.Extensions.Options;

namespace FeeNominalService.Services.AWS;

/// <summary>
/// Utility service for formatting secret names according to configuration patterns
/// </summary>
public class SecretNameFormatter
{
    private readonly AwsSecretsManagerConfiguration _config;

    public SecretNameFormatter(IOptions<AwsSecretsManagerConfiguration> config)
    {
        _config = config.Value;
    }

    /// <summary>
    /// Formats a merchant secret name using the configured pattern
    /// </summary>
    /// <param name="merchantId">The merchant ID (GUID)</param>
    /// <param name="apiKey">The API key</param>
    /// <returns>Formatted secret name</returns>
    public string FormatMerchantSecretName(Guid merchantId, string apiKey)
    {
        return _config.MerchantSecretNameFormat
            .Replace("{merchantId}", merchantId.ToString("D"))
            .Replace("{apiKey}", apiKey);
    }

    /// <summary>
    /// Formats a merchant secret name using the configured pattern (string merchantId)
    /// </summary>
    /// <param name="merchantId">The merchant ID (string)</param>
    /// <param name="apiKey">The API key</param>
    /// <returns>Formatted secret name</returns>
    public string FormatMerchantSecretName(string merchantId, string apiKey)
    {
        return _config.MerchantSecretNameFormat
            .Replace("{merchantId}", merchantId)
            .Replace("{apiKey}", apiKey);
    }

    /// <summary>
    /// Formats an admin secret name using the configured pattern
    /// </summary>
    /// <param name="serviceName">The service name</param>
    /// <returns>Formatted secret name</returns>
    public string FormatAdminSecretName(string serviceName)
    {
        return _config.AdminSecretNameFormat
            .Replace("{serviceName}", serviceName);
    }

    /// <summary>
    /// Extracts the API key from a merchant secret name
    /// </summary>
    /// <param name="secretName">The secret name</param>
    /// <returns>The API key if found, null otherwise</returns>
    public string? ExtractApiKeyFromMerchantSecretName(string secretName)
    {
        // Parse the pattern to understand the structure
        var pattern = _config.MerchantSecretNameFormat;
        var parts = pattern.Split('/');
        
        // Find the position of {apiKey} in the pattern
        var apiKeyIndex = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "{apiKey}")
            {
                apiKeyIndex = i;
                break;
            }
        }

        if (apiKeyIndex == -1)
        {
            return null;
        }

        // Extract the corresponding part from the actual secret name
        var secretParts = secretName.Split('/');
        if (secretParts.Length > apiKeyIndex)
        {
            return secretParts[apiKeyIndex];
        }

        return null;
    }

    /// <summary>
    /// Extracts the service name from an admin secret name
    /// </summary>
    /// <param name="secretName">The secret name</param>
    /// <returns>The service name if found, null otherwise</returns>
    public string? ExtractServiceNameFromAdminSecretName(string secretName)
    {
        // Parse the pattern to understand the structure
        var pattern = _config.AdminSecretNameFormat;
        var parts = pattern.Split('/');
        
        // Find the position of {serviceName} in the pattern
        var serviceNameIndex = -1;
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Contains("{serviceName}"))
            {
                serviceNameIndex = i;
                break;
            }
        }

        if (serviceNameIndex == -1)
        {
            return null;
        }

        // Extract the corresponding part from the actual secret name
        var secretParts = secretName.Split('/');
        if (secretParts.Length > serviceNameIndex)
        {
            var part = secretParts[serviceNameIndex];
            // Remove the "-admin-api-key-secret" suffix if present
            if (part.EndsWith("-admin-api-key-secret"))
            {
                return part.Replace("-admin-api-key-secret", "");
            }
            return part;
        }

        return null;
    }

    /// <summary>
    /// Determines if a secret name matches the admin pattern
    /// </summary>
    /// <param name="secretName">The secret name to check</param>
    /// <returns>True if it's an admin secret, false otherwise</returns>
    public bool IsAdminSecretName(string secretName)
    {
        return secretName.Contains("/admin/") && secretName.EndsWith("-admin-api-key-secret");
    }

    /// <summary>
    /// Determines if a secret name matches the merchant pattern
    /// </summary>
    /// <param name="secretName">The secret name to check</param>
    /// <returns>True if it's a merchant secret, false otherwise</returns>
    public bool IsMerchantSecretName(string secretName)
    {
        return secretName.Contains("/merchants/") && secretName.Contains("/apikeys/");
    }
} 