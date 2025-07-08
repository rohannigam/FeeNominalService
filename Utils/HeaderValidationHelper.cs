using Microsoft.AspNetCore.Http;

namespace FeeNominalService.Utils
{
    public static class HeaderValidationHelper
    {
        public static (bool IsValid, string? Value, string? ErrorMessage) GetHeaderValue(IHeaderDictionary headers, string headerName)
        {
            if (headers == null)
            {
                return (false, null, "Headers dictionary is null");
            }

            // Try exact match first
            if (headers.TryGetValue(headerName, out var value))
            {
                var stringValue = value.ToString();
                if (string.IsNullOrEmpty(stringValue))
                {
                    return (false, null, $"Header {headerName} is empty");
                }
                return (true, stringValue, null);
            }

            // Try case-insensitive match
            var header = headers.FirstOrDefault(h => 
                string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase));
            
            if (header.Key == null)
            {
                return (false, null, $"Header {headerName} not found");
            }

            var caseInsensitiveValue = header.Value.ToString();
            if (string.IsNullOrEmpty(caseInsensitiveValue))
            {
                return (false, null, $"Header {headerName} is empty");
            }

            return (true, caseInsensitiveValue, null);
        }

        public static (bool IsValid, string? Value, string? ErrorMessage) ValidateRequiredHeader(IHeaderDictionary headers, string headerName)
        {
            var (isValid, value, errorMessage) = GetHeaderValue(headers, headerName);
            if (!isValid)
            {
                return (false, null, errorMessage ?? $"Header {headerName} is required");
            }
            return (true, value, null);
        }

        public static (bool IsValid, Guid Value, string? ErrorMessage) ValidateRequiredGuidHeader(IHeaderDictionary headers, string headerName)
        {
            var (isValid, value, errorMessage) = ValidateRequiredHeader(headers, headerName);
            if (!isValid)
            {
                return (false, Guid.Empty, errorMessage);
            }

            if (!Guid.TryParse(value, out var guidValue))
            {
                return (false, Guid.Empty, $"Header {headerName} is not a valid GUID");
            }

            return (true, guidValue, null);
        }
    }

    public static class Masker
    {
        public static string MaskSecret(string? secret, int showStart = 4, int showEnd = 6)
        {
            if (string.IsNullOrEmpty(secret)) return "<null>";
            if (secret.Length <= showStart + showEnd)
                return new string('*', secret.Length);
            return secret.Substring(0, showStart) + "..." + secret.Substring(secret.Length - showEnd);
        }
    }
} 