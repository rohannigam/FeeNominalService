using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using FeeNominalService.Settings;

namespace FeeNominalService.Services
{
    /// <summary>
    /// Service for validating credential formats and content
    /// </summary>
    public class CredentialValidationService : ICredentialValidationService
    {
        private readonly ILogger<CredentialValidationService> _logger;
        private readonly SurchargeProviderValidationSettings _settings;

        public CredentialValidationService(
            ILogger<CredentialValidationService> logger,
            SurchargeProviderValidationSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public bool ValidateJwtFormat(string jwtToken)
        {
            if (!_settings.ValidateJwtFormat)
                return true;

            try
            {
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return false;

                // JWT tokens have 3 parts separated by dots
                var parts = jwtToken.Split('.');
                if (parts.Length != 3)
                    return false;

                // Each part should be base64url encoded
                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part))
                        return false;

                    // Check if it's valid base64url (no padding, only alphanumeric, -, _)
                    if (!Regex.IsMatch(part, @"^[A-Za-z0-9_-]+$"))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error validating JWT format");
                return false;
            }
        }

        public bool ValidateApiKeyFormat(string apiKey)
        {
            if (!_settings.ValidateApiKeyFormat)
                return true;

            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                    return false;

                // API keys should be alphanumeric with possible hyphens/underscores
                // and typically 32-64 characters long
                if (!Regex.IsMatch(apiKey, @"^[A-Za-z0-9_-]{16,128}$"))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error validating API key format");
                return false;
            }
        }

        public bool ValidateEmailFormat(string email)
        {
            if (!_settings.ValidateEmailFormat)
                return true;

            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                // Basic email validation regex
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error validating email format");
                return false;
            }
        }

        public bool ValidateUrlFormat(string url)
        {
            if (!_settings.ValidateUrlFormat)
                return true;

            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return false;

                // Basic URL validation - should start with http:// or https://
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Try to create a Uri to validate the format
                return Uri.TryCreate(url, UriKind.Absolute, out _);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error validating URL format");
                return false;
            }
        }

        public bool ValidateCredentialValue(string fieldType, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Check minimum length
            if (value.Length < _settings.MinCredentialValueLength)
                return false;

            // Check maximum length
            if (value.Length > _settings.MaxCredentialValueLength)
                return false;

            // Validate based on field type
            return fieldType.ToLowerInvariant() switch
            {
                "jwt" => ValidateJwtFormat(value),
                "api_key" => ValidateApiKeyFormat(value),
                "email" => ValidateEmailFormat(value),
                "url" => ValidateUrlFormat(value),
                "client_id" => ValidateApiKeyFormat(value), // Similar to API key
                "client_secret" => ValidateApiKeyFormat(value), // Similar to API key
                "access_token" => ValidateJwtFormat(value), // Could be JWT or similar
                "refresh_token" => ValidateApiKeyFormat(value), // Similar to API key
                "username" => !string.IsNullOrWhiteSpace(value) && value.Length <= 100,
                "password" => !string.IsNullOrWhiteSpace(value) && value.Length >= 8,
                "certificate" => value.Contains("-----BEGIN CERTIFICATE-----") && value.Contains("-----END CERTIFICATE-----"),
                "private_key" => value.Contains("-----BEGIN PRIVATE KEY-----") && value.Contains("-----END PRIVATE KEY-----"),
                "public_key" => value.Contains("-----BEGIN PUBLIC KEY-----") && value.Contains("-----END PUBLIC KEY-----"),
                "base64" => IsValidBase64(value),
                "json" => IsValidJson(value),
                _ => true // For other types, just check length
            };
        }

        public (bool IsValid, List<string> Errors) ValidateCredentialsObject(JsonDocument credentials, int maxSize, int maxValueLength, int minValueLength)
        {
            var errors = new List<string>();

            try
            {
                // Check total size
                var credentialsJson = credentials.RootElement.GetRawText();
                if (credentialsJson.Length > maxSize)
                {
                    errors.Add($"Credentials object size ({credentialsJson.Length} characters) exceeds maximum allowed size ({maxSize} characters)");
                }

                // Validate each credential value
                foreach (var property in credentials.RootElement.EnumerateObject())
                {
                    var value = property.Value;
                    if (value.ValueKind == JsonValueKind.String)
                    {
                        var stringValue = value.GetString() ?? string.Empty;
                        
                        // Check minimum length
                        if (stringValue.Length < minValueLength)
                        {
                            errors.Add($"Credential value '{property.Name}' length ({stringValue.Length}) is below minimum ({minValueLength})");
                        }

                        // Check maximum length
                        if (stringValue.Length > maxValueLength)
                        {
                            errors.Add($"Credential value '{property.Name}' length ({stringValue.Length}) exceeds maximum ({maxValueLength})");
                        }
                    }
                    else if (value.ValueKind == JsonValueKind.Object || value.ValueKind == JsonValueKind.Array)
                    {
                        // For complex objects, check their JSON size
                        var valueJson = value.GetRawText();
                        if (valueJson.Length > maxValueLength)
                        {
                            errors.Add($"Credential value '{property.Name}' size ({valueJson.Length} characters) exceeds maximum ({maxValueLength})");
                        }
                    }
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials object");
                errors.Add($"Error validating credentials object: {ex.Message}");
                return (false, errors);
            }
        }

        private bool IsValidBase64(string value)
        {
            try
            {
                // Remove padding if present
                var base64 = value.Replace("=", "");
                
                // Check if it's valid base64url (no padding, only alphanumeric, -, _)
                if (!Regex.IsMatch(base64, @"^[A-Za-z0-9_-]+$"))
                    return false;

                // Try to decode to verify it's valid
                var bytes = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/'));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidJson(string value)
        {
            try
            {
                JsonDocument.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
} 