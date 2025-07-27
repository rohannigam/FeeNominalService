using System.Text.RegularExpressions;

namespace FeeNominalService.Utils
{
    /// <summary>
    /// Utility class for sanitizing input data before logging to prevent Log Forging attacks
    /// </summary>
    public static class LogSanitizer
    {
        private static readonly Regex ValidGuidPattern = new(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
        private static readonly Regex ValidMerchantIdPattern = new(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$", RegexOptions.Compiled);
        
        /// <summary>
        /// Sanitizes a merchant ID for logging by validating it's a proper GUID format
        /// </summary>
        /// <param name="merchantId">The merchant ID to sanitize</param>
        /// <returns>Sanitized merchant ID or "[INVALID_MERCHANT_ID]" if invalid</returns>
        public static string SanitizeMerchantId(string? merchantId)
        {
            if (string.IsNullOrWhiteSpace(merchantId))
                return "[NULL_MERCHANT_ID]";

            // Remove any control characters that could be used for log injection
            var sanitized = RemoveControlCharacters(merchantId);
            
            // Validate it's a proper GUID format
            if (!ValidMerchantIdPattern.IsMatch(sanitized))
                return "[INVALID_MERCHANT_ID]";

            return sanitized;
        }

        /// <summary>
        /// Sanitizes a GUID for logging
        /// </summary>
        /// <param name="guid">The GUID to sanitize</param>
        /// <returns>Sanitized GUID or "[INVALID_GUID]" if invalid</returns>
        public static string SanitizeGuid(string? guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
                return "[NULL_GUID]";

            // Remove any control characters
            var sanitized = RemoveControlCharacters(guid);
            
            // Validate it's a proper GUID format
            if (!ValidGuidPattern.IsMatch(sanitized))
                return "[INVALID_GUID]";

            return sanitized;
        }

        /// <summary>
        /// Sanitizes a GUID for logging
        /// </summary>
        /// <param name="guid">The GUID to sanitize</param>
        /// <returns>Sanitized GUID or "[NULL_GUID]" if null</returns>
        public static string SanitizeGuid(Guid? guid)
        {
            if (!guid.HasValue)
                return "[NULL_GUID]";

            return guid.Value.ToString();
        }

        /// <summary>
        /// Sanitizes a string for logging by removing control characters
        /// </summary>
        /// <param name="input">The string to sanitize</param>
        /// <returns>Sanitized string</returns>
        public static string SanitizeString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "[NULL_STRING]";

            return RemoveControlCharacters(input);
        }

        /// <summary>
        /// Removes control characters that could be used for log injection
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>String with control characters removed</returns>
        private static string RemoveControlCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Remove control characters (ASCII 0-31, except tab, newline, carriage return)
            // and other potentially dangerous characters
            return new string(input.Where(c => 
                c >= 32 || c == '\t' || c == '\n' || c == '\r'
            ).ToArray());
        }
    }
} 