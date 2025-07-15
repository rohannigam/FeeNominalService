using System.Text.Json;
using System.Text.RegularExpressions;

namespace FeeNominalService.Utils
{
    /// <summary>
    /// Utility class for masking sensitive information in logs
    /// </summary>
    public static class SensitiveDataMasker
    {
        private static readonly string[] SensitiveFieldNames = {
            "jwt_token", "jwtToken", "token", "password", "secret", "key", "api_key", "apiKey", "client_secret", "clientSecret",
            "access_token", "accessToken", "refresh_token", "refreshToken", "private_key", "privateKey", "certificate", "credentials"
        };

        /// <summary>
        /// Masks sensitive information in a JSON string
        /// </summary>
        /// <param name="jsonString">The JSON string to mask</param>
        /// <returns>Masked JSON string</returns>
        public static string MaskSensitiveData(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return jsonString;

            try
            {
                // First, check if this is a JWT token (individual string)
                if (IsJwtToken(jsonString))
                {
                    return MaskSensitiveValue(jsonString);
                }

                // Try to parse as JSON
                var jsonDoc = JsonDocument.Parse(jsonString);
                // If it's valid JSON, process it
                var maskedJson = jsonString;
                
                // First, mask JWT tokens (this should take precedence)
                maskedJson = MaskJwtTokens(maskedJson);
                
                // Then, mask other sensitive fields
                maskedJson = MaskSensitiveFields(maskedJson);
                
                return maskedJson;
            }
            catch
            {
                // If JSON parsing fails, check if it's a JWT token
                if (IsJwtToken(jsonString))
                {
                    return MaskSensitiveValue(jsonString);
                }
                
                // If it's not JSON and not a JWT, return as-is
                return jsonString;
            }
        }

        /// <summary>
        /// Masks sensitive information in an object by serializing to JSON first
        /// </summary>
        /// <param name="obj">The object to mask</param>
        /// <returns>Masked JSON string</returns>
        public static string MaskSensitiveData(object obj)
        {
            if (obj == null)
                return "null";

            try
            {
                // Serialize with original property names to ensure masking works
                var jsonString = JsonSerializer.Serialize(obj, new JsonSerializerOptions { 
                    WriteIndented = true
                });
                
                // Mask the sensitive data in the JSON string
                return MaskSensitiveData(jsonString);
            }
            catch
            {
                return "[Object serialization failed]";
            }
        }

        /// <summary>
        /// Masks a sensitive value
        /// </summary>
        /// <param name="value">The value to mask</param>
        /// <returns>Masked value</returns>
        private static string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (value.Length <= 8)
            {
                return new string('*', value.Length);
            }

            // For all sensitive values, show first 4 and last 4 characters
            return $"{value.Substring(0, 4)}...{value.Substring(value.Length - 4)}";
        }

        /// <summary>
        /// Checks if a value looks like a JWT token
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns>True if it looks like a JWT token</returns>
        private static bool IsJwtToken(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // JWT tokens typically have 3 parts separated by dots
            var parts = value.Split('.');
            return parts.Length == 3 && parts.All(part => !string.IsNullOrEmpty(part));
        }

        /// <summary>
        /// Masks JWT tokens in a JSON string
        /// </summary>
        /// <param name="jsonString">The JSON string to process</param>
        /// <returns>JSON string with masked JWT tokens</returns>
        private static string MaskJwtTokens(string jsonString)
        {
            // Pattern to match JWT tokens (3 parts separated by dots)
            var jwtPattern = @"([A-Za-z0-9+/=]+\.[A-Za-z0-9+/=]+\.[A-Za-z0-9+/=]+)";
            return Regex.Replace(jsonString, jwtPattern, match =>
            {
                var token = match.Value;
                if (token.Length <= 8)
                    return new string('*', token.Length);
                return $"{token.Substring(0, 4)}...{token.Substring(token.Length - 4)}";
            });
        }

        /// <summary>
        /// Masks sensitive fields in a JSON string
        /// </summary>
        /// <param name="jsonString">The JSON string to process</param>
        /// <returns>JSON string with masked sensitive fields</returns>
        private static string MaskSensitiveFields(string jsonString)
        {
            foreach (var fieldName in SensitiveFieldNames)
            {
                // Create patterns for different field name formats
                var fieldVariations = new[]
                {
                    fieldName,                                    // original
                    fieldName.ToLowerInvariant(),                 // lowercase
                    fieldName.ToUpperInvariant(),                 // uppercase
                    fieldName.Replace("_", ""),                   // camelCase
                    fieldName.Replace("_", "").ToLowerInvariant(), // camelCase lowercase
                    fieldName.Replace("_", "").ToUpperInvariant()  // camelCase uppercase
                };

                foreach (var variation in fieldVariations)
                {
                    // Pattern to match: "fieldName": "value"
                    var pattern = $@"""{Regex.Escape(variation)}""\s*:\s*""([^""]+)""";
                    jsonString = Regex.Replace(jsonString, pattern, match =>
                    {
                        var value = match.Groups[1].Value;
                        if (string.IsNullOrEmpty(value))
                            return match.Value;
                        
                        var maskedValue = MaskSensitiveValue(value);
                        return $@"""{variation}"" : ""{maskedValue}""";
                    }, RegexOptions.IgnoreCase);
                }
            }
            return jsonString;
        }
    }
} 