using System;
using System.Security;
using System.Text.Json;
using FeeNominalService.Utils;

namespace FeeNominalService.Models.ApiKey
{
    /// <summary>
    /// Secure wrapper for ApiKeySecret objects using SecureString to prevent memory dumps
    /// This class uses SecureString for secure handling of API key secrets
    /// Enhanced security: Uses SecureString and proper disposal to prevent memory dumps and exposure
    /// </summary>
    public class SecureApiKeySecretWrapper : IDisposable
    {
        private SecureString? _secureApiKey;
        private SecureString? _secureSecret;
        private bool _disposed = false;

        // Non-sensitive properties
        public Guid? MerchantId { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public DateTime? LastRotated { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Scope { get; set; } = string.Empty;

        /// <summary>
        /// Sets the API key securely using SecureString
        /// </summary>
        /// <param name="apiKey">The API key to secure</param>
        public void SetApiKey(string apiKey)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            _secureApiKey?.Dispose();
            _secureApiKey = SimpleSecureDataHandler.ToSecureString(apiKey);
        }

        /// <summary>
        /// Sets the secret securely using SecureString
        /// </summary>
        /// <param name="secret">The secret to secure</param>
        public void SetSecret(string secret)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            _secureSecret?.Dispose();
            _secureSecret = SimpleSecureDataHandler.ToSecureString(secret);
        }

        /// <summary>
        /// Processes the API key securely without exposing it as a string
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="processor">Function to process the API key</param>
        /// <returns>Result of processing</returns>
        public T? ProcessApiKeySecurely<T>(Func<SecureString, T> processor)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            if (_secureApiKey == null)
                return default(T);

            var apiKeyString = SimpleSecureDataHandler.FromSecureString(_secureApiKey);
            return SimpleSecureDataHandler.ProcessSecurely(apiKeyString, secureApiKey => processor(_secureApiKey));
        }

        /// <summary>
        /// Processes the secret securely without exposing it as a string
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="processor">Function to process the secret</param>
        /// <returns>Result of processing</returns>
        public T? ProcessSecretSecurely<T>(Func<SecureString, T> processor)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            if (_secureSecret == null)
                return default(T);

            var secretString = SimpleSecureDataHandler.FromSecureString(_secureSecret);
            return SimpleSecureDataHandler.ProcessSecurely(secretString, secureSecret => processor(_secureSecret));
        }

        /// <summary>
        /// Gets the API key (use with caution - exposes as string)
        /// </summary>
        /// <returns>API key as string</returns>
        public string GetApiKey()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            return _secureApiKey != null ? SimpleSecureDataHandler.FromSecureString(_secureApiKey) : string.Empty;
        }

        /// <summary>
        /// Gets the secret (use with caution - exposes as string)
        /// </summary>
        /// <returns>Secret as string</returns>
        public string GetSecret()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            return _secureSecret != null ? SimpleSecureDataHandler.FromSecureString(_secureSecret) : string.Empty;
        }

        /// <summary>
        /// Creates a SecureApiKeySecretWrapper from a regular ApiKeySecret
        /// </summary>
        /// <param name="apiKeySecret">The regular ApiKeySecret</param>
        /// <returns>SecureApiKeySecretWrapper</returns>
        public static SecureApiKeySecretWrapper FromApiKeySecret(ApiKeySecret apiKeySecret)
        {
            var secure = new SecureApiKeySecretWrapper
            {
                MerchantId = apiKeySecret.MerchantId,
                Status = apiKeySecret.Status,
                IsRevoked = apiKeySecret.IsRevoked,
                RevokedAt = apiKeySecret.RevokedAt,
                LastRotated = apiKeySecret.LastRotated,
                CreatedAt = apiKeySecret.CreatedAt,
                UpdatedAt = apiKeySecret.UpdatedAt,
                Scope = apiKeySecret.Scope
            };

            secure.SetApiKey(apiKeySecret.ApiKey);
            secure.SetSecret(apiKeySecret.Secret);
            return secure;
        }

        /// <summary>
        /// Converts back to a regular ApiKeySecret
        /// </summary>
        /// <returns>Regular ApiKeySecret</returns>
        public ApiKeySecret ToApiKeySecret()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            return new ApiKeySecret
            {
                ApiKey = GetApiKey(),
                Secret = GetSecret(),
                MerchantId = MerchantId,
                Status = Status,
                IsRevoked = IsRevoked,
                RevokedAt = RevokedAt,
                LastRotated = LastRotated,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Scope = Scope
            };
        }

        /// <summary>
        /// Creates a SecureApiKeySecretWrapper from JSON string
        /// </summary>
        /// <param name="jsonString">JSON string representation</param>
        /// <returns>SecureApiKeySecretWrapper</returns>
        public static SecureApiKeySecretWrapper FromJsonString(string jsonString)
        {
            var apiKeySecret = JsonSerializer.Deserialize<ApiKeySecret>(jsonString);
            if (apiKeySecret == null)
                throw new ArgumentException("Invalid JSON format for ApiKeySecret");

            return FromApiKeySecret(apiKeySecret);
        }

        /// <summary>
        /// Converts to JSON string (use with caution - exposes sensitive data)
        /// </summary>
        /// <returns>JSON string representation</returns>
        public string ToJsonString()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            var apiKeySecret = ToApiKeySecret();
            return JsonSerializer.Serialize(apiKeySecret);
        }

        /// <summary>
        /// Creates a masked version for logging (safe to use in logs)
        /// </summary>
        /// <returns>Masked ApiKeySecret for logging</returns>
        public ApiKeySecret ToMaskedApiKeySecret()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureApiKeySecretWrapper));

            return new ApiKeySecret
            {
                ApiKey = LogSanitizer.SanitizeString(GetApiKey()),
                Secret = "[REDACTED]",
                MerchantId = MerchantId,
                Status = Status,
                IsRevoked = IsRevoked,
                RevokedAt = RevokedAt,
                LastRotated = LastRotated,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Scope = Scope
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _secureApiKey?.Dispose();
                _secureSecret?.Dispose();
                _disposed = true;
            }
        }
    }
} 
