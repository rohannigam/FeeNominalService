using System;
using System.Security;
using System.Text.Json;
using FeeNominalService.Utils;

namespace FeeNominalService.Models.ApiKey
{
    /// <summary>
    /// Secure wrapper for ApiKeySecret that uses SecureString for sensitive data
    /// Checkmarx: This class provides secure handling of API key secrets to prevent memory dumps
    /// </summary>
    public class SecureApiKeySecret : IDisposable
    {
        private SecureString? _secureSecret;
        private bool _isDisposed = false;

        public string ApiKey { get; set; } = string.Empty;
        public Guid? MerchantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastRotated { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public string? Scope { get; set; }

        /// <summary>
        /// Gets the secret value securely
        /// </summary>
        /// <returns>The secret value or empty string if disposed</returns>
        public string GetSecret()
        {
            if (_isDisposed)
                return string.Empty;

            if (_secureSecret != null)
            {
                return SimpleSecureDataHandler.FromSecureString(_secureSecret);
            }

            return string.Empty;
        }

        /// <summary>
        /// Sets the secret value securely
        /// </summary>
        /// <param name="secret">The secret value</param>
        public void SetSecret(string? secret)
        {
            if (_isDisposed)
                return;

            // Clear existing secure data
            _secureSecret?.Dispose();
            _secureSecret = null;

            if (!string.IsNullOrEmpty(secret))
            {
                _secureSecret = SimpleSecureDataHandler.ToSecureString(secret);
            }
        }

        /// <summary>
        /// Processes the secret securely without exposing it as a string
        /// </summary>
        /// <param name="processor">Action to perform with the secure secret</param>
        public void ProcessSecretSecurely(Action<SecureString>? processor)
        {
            if (_isDisposed || processor == null)
                return;

            if (_secureSecret != null)
            {
                processor(_secureSecret);
            }
        }

        /// <summary>
        /// Processes the secret securely and returns a result
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="processor">Function to perform with the secure secret</param>
        /// <returns>The result of processing</returns>
        public T? ProcessSecretSecurely<T>(Func<SecureString, T>? processor)
        {
            if (_isDisposed || processor == null)
                return default(T?);

            if (_secureSecret != null)
            {
                return processor(_secureSecret);
            }

            return default(T?);
        }

        /// <summary>
        /// Converts from regular ApiKeySecret to SecureApiKeySecret
        /// </summary>
        /// <param name="apiKeySecret">The regular ApiKeySecret</param>
        /// <returns>SecureApiKeySecret</returns>
        public static SecureApiKeySecret FromApiKeySecret(ApiKeySecret apiKeySecret)
        {
            var secure = new SecureApiKeySecret
            {
                ApiKey = apiKeySecret.ApiKey,
                MerchantId = apiKeySecret.MerchantId,
                CreatedAt = apiKeySecret.CreatedAt,
                LastRotated = apiKeySecret.LastRotated,
                IsRevoked = apiKeySecret.IsRevoked,
                RevokedAt = apiKeySecret.RevokedAt,
                Status = apiKeySecret.Status,
                Scope = apiKeySecret.Scope ?? "merchant"
            };

            secure.SetSecret(apiKeySecret.Secret ?? string.Empty);
            return secure;
        }

        /// <summary>
        /// Converts to regular ApiKeySecret (use with caution)
        /// </summary>
        /// <returns>ApiKeySecret</returns>
        public ApiKeySecret ToApiKeySecret()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecret));

            return new ApiKeySecret
            {
                ApiKey = this.ApiKey,
                Secret = GetSecret(), // This exposes the secret - use carefully
                MerchantId = this.MerchantId,
                CreatedAt = this.CreatedAt,
                LastRotated = this.LastRotated,
                IsRevoked = this.IsRevoked,
                RevokedAt = this.RevokedAt,
                Status = this.Status,
                Scope = this.Scope ?? "merchant"
            };
        }

        /// <summary>
        /// Creates a safe copy for API responses (without the actual secret)
        /// </summary>
        /// <returns>ApiKeyInfo without sensitive data</returns>
        public ApiKeyInfo ToApiKeyInfo()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecret));

            return new ApiKeyInfo
            {
                ApiKey = this.ApiKey,
                MerchantId = this.MerchantId,
                Status = this.Status,
                CreatedAt = this.CreatedAt,
                LastRotatedAt = this.LastRotated,
                RevokedAt = this.RevokedAt,
                IsRevoked = this.IsRevoked,
                ExpiresAt = null, // Not available in this context
                Description = string.Empty,
                RateLimit = 0,
                AllowedEndpoints = Array.Empty<string>(),
                LastUsedAt = null,
                UsageCount = 0,
                Scope = this.Scope ?? "merchant",
                IsAdmin = this.Scope == "admin"
            };
        }

        /// <summary>
        /// Serializes to JSON without exposing the secret
        /// </summary>
        /// <returns>JSON string without sensitive data</returns>
        public string ToJson()
        {
            if (_isDisposed)
                return "{}";

            var safeObject = new
            {
                ApiKey = this.ApiKey,
                MerchantId = this.MerchantId,
                CreatedAt = this.CreatedAt,
                LastRotated = this.LastRotated,
                IsRevoked = this.IsRevoked,
                RevokedAt = this.RevokedAt,
                Status = this.Status,
                Scope = this.Scope,
                Secret = "[PROTECTED]" // Never expose the actual secret
            };

            return JsonSerializer.Serialize(safeObject);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _secureSecret?.Dispose();
                _secureSecret = null;
                _isDisposed = true;
            }
        }
    }
} 