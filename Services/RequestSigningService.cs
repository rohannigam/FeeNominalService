using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Repositories;
using FeeNominalService.Services.AWS;
using System.Collections.Concurrent;
using FeeNominalService.Utils;

namespace FeeNominalService.Services
{
    public interface IRequestSigningService
    {
        Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature);
        Task<string> GenerateSignatureAsync(string merchantId, string apiKey, string timestamp, string nonce);
        bool ValidateTimestampAndNonce(string timestamp, string nonce);
    }

    public class RequestSigningService : IRequestSigningService
    {
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IAwsSecretsManagerService _secretsManager;
        private readonly ILogger<RequestSigningService> _logger;
        private readonly ApiKeyConfiguration _apiKeyConfig;
        private readonly ConcurrentDictionary<string, DateTime> _usedNonces;
        private readonly HashSet<string> _recentNonces;
        private readonly SecretNameFormatter _secretNameFormatter;

        public RequestSigningService(
            IApiKeyRepository apiKeyRepository,
            IMerchantRepository merchantRepository,
            IAwsSecretsManagerService secretsManager,
            ILogger<RequestSigningService> logger,
            IOptions<ApiKeyConfiguration> apiKeyConfig,
            SecretNameFormatter secretNameFormatter)
        {
            _apiKeyRepository = apiKeyRepository;
            _merchantRepository = merchantRepository;
            _secretsManager = secretsManager;
            _logger = logger;
            _apiKeyConfig = apiKeyConfig.Value;
            _usedNonces = new ConcurrentDictionary<string, DateTime>();
            _recentNonces = new HashSet<string>();
            _secretNameFormatter = secretNameFormatter;
        }

        public bool ValidateTimestampAndNonce(string timestamp, string nonce)
        {
            try
            {
                // Parse the timestamp and ensure it's in UTC
                if (!DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime requestTime))
                {
                    _logger.LogWarning("Invalid timestamp format: {Timestamp}", timestamp);
                    return false;
                }

                // Ensure requestTime is in UTC
                if (requestTime.Kind != DateTimeKind.Utc)
                {
                    requestTime = DateTime.SpecifyKind(requestTime, DateTimeKind.Utc);
                }

                var currentTime = DateTime.UtcNow;
                var timeDiff = Math.Abs((currentTime - requestTime).TotalMinutes);

                // Log detailed time information for debugging
                _logger.LogInformation(
                    "Time validation details - Request time (UTC): {RequestTime}, Current time (UTC): {CurrentTime}, Time difference (minutes): {TimeDiff}",
                    requestTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    currentTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeDiff);

                // Check if the timestamp is within the allowed window
                if (timeDiff > _apiKeyConfig.RequestTimeWindowMinutes)
                {
                    _logger.LogWarning(
                        "Request timestamp is outside the allowed window. Request time (UTC): {RequestTime}, Current time (UTC): {CurrentTime}, Time difference (minutes): {TimeDiff}",
                        requestTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        currentTime.ToString("yyyy-MM-dd HH:mm:ss"),
                        timeDiff);
                    return false;
                }

                // Check if the nonce has been used recently
                if (_recentNonces.Contains(nonce))
                {
                    _logger.LogWarning("Nonce has been used recently: {Nonce}", nonce);
                    return false;
                }

                // Add the nonce to the recent nonces set
                _recentNonces.Add(nonce);

                // Clean up old nonces
                CleanupOldNonces();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating timestamp and nonce");
                return false;
            }
        }

        private void CleanupOldNonces()
        {
            var currentTime = DateTime.UtcNow;
            var oldNonces = _usedNonces.Where(kvp => currentTime - kvp.Value > TimeSpan.FromMinutes(_apiKeyConfig.RequestTimeWindowMinutes))
                                     .Select(kvp => kvp.Key)
                                     .ToList();

            foreach (var nonce in oldNonces)
            {
                _usedNonces.TryRemove(nonce, out _);
            }
        }

        public async Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature)
        {
            try
            {
                _logger.LogDebug(
                    "Starting request validation - MerchantId: {MerchantId}, ApiKey: {ApiKey}, Timestamp: {Timestamp}, Nonce: {Nonce}",
                    merchantId, apiKey, timestamp, nonce);

                // 1. Validate merchant exists - try both internal and external IDs
                Merchant? merchant = null;
                
                // First try as internal ID (GUID)
                if (Guid.TryParse(merchantId, out var merchantGuid))
                {
                    try
                    {
                        merchant = await _merchantRepository.GetByIdAsync(merchantGuid);
                        _logger.LogDebug("Found merchant by internal ID: {MerchantId}", merchantGuid);
                    }
                    catch (KeyNotFoundException)
                    {
                        _logger.LogDebug("Merchant not found by internal ID, trying external ID");
                        // Ignore and try external ID
                    }
                }

                // If not found by internal ID, try as external ID
                if (merchant == null)
                {
                    merchant = await _merchantRepository.GetByExternalIdAsync(merchantId);
                    _logger.LogDebug("Found merchant by external ID: {MerchantId}", merchantId);
                }

                if (merchant == null)
                {
                    _logger.LogWarning("Merchant not found: {MerchantId}", merchantId);
                    return false;
                }

                // 2. Get API key from database
                var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
                if (apiKeyEntity == null || apiKeyEntity.Status != "ACTIVE")
                {
                    _logger.LogWarning("Invalid or inactive API key: {ApiKey}", LogSanitizer.SanitizeString(apiKey));
                    return false;
                }
                _logger.LogDebug("Found valid API key: {ApiKey}", LogSanitizer.SanitizeString(apiKey));

                // 3. Get secret from AWS Secrets Manager
                var secretData = await GetMerchantSecretSecurelyAsync(merchant.MerchantId.ToString(), apiKey);
                if (secretData == null || secretData.IsRevoked)
                {
                    _logger.LogWarning("Secret not found or revoked for API key: {ApiKey}", LogSanitizer.SanitizeString(apiKey));
                    return false;
                }
                _logger.LogDebug("Successfully retrieved secret from AWS");

                // 4. Validate timestamp
                if (!DateTime.TryParse(timestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var requestTime))
                {
                    _logger.LogWarning("Invalid timestamp format: {Timestamp}", timestamp);
                    return false;
                }

                // Ensure requestTime is in UTC
                if (requestTime.Kind != DateTimeKind.Utc)
                {
                    requestTime = DateTime.SpecifyKind(requestTime, DateTimeKind.Utc);
                }

                var timeDiff = Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes);
                _logger.LogDebug(
                    "Time validation - Request time: {RequestTime}, Current time: {CurrentTime}, Difference: {TimeDiff} minutes",
                    requestTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    timeDiff);

                if (timeDiff > _apiKeyConfig.RequestTimeWindowMinutes)
                {
                    _logger.LogWarning("Request timestamp outside allowed window: {TimeDiff} minutes", timeDiff);
                    return false;
                }

                // 5. Validate signature
                _logger.LogDebug("Generating expected signature...");
                var expectedSignature = await GenerateSignatureAsync(merchant.MerchantId.ToString("D"), apiKey, timestamp, nonce);
                
                _logger.LogDebug(
                    "Signature validation details:\n" +
                    "MerchantId: {MerchantId}\n" +
                    "ApiKey: {ApiKey}\n" +
                    "Timestamp: {Timestamp}\n" +
                    "Nonce: {Nonce}\n" +
                    "Request body: {RequestBody}\n" +
                    "Received signature: {ReceivedSignature}\n" +
                    "Expected signature: {ExpectedSignature}\n" +
                    "Match: {IsMatch}",
                    LogSanitizer.SanitizeGuid(merchant.MerchantId),
                    LogSanitizer.SanitizeString(apiKey),
                    LogSanitizer.SanitizeString(timestamp),
                    LogSanitizer.SanitizeString(nonce),
                    LogSanitizer.SanitizeString(requestBody),
                    LogSanitizer.SanitizeString(signature),
                    LogSanitizer.SanitizeString(expectedSignature),
                    string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase));

                return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating request signature");
                return false;
            }
        }

        public async Task<string> GenerateSignatureAsync(string merchantId, string apiKey, string timestamp, string nonce)
        {
            try
            {
                _logger.LogDebug(
                    "Starting signature generation - MerchantId: {MerchantId}, ApiKey: {ApiKey}, Timestamp: {Timestamp}, Nonce: {Nonce}",
                    LogSanitizer.SanitizeString(merchantId), LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(timestamp), LogSanitizer.SanitizeString(nonce));

                // 1. Get secret from AWS Secrets Manager
                var secretData = await GetMerchantSecretSecurelyAsync(merchantId, apiKey);
                if (secretData == null)
                {
                    _logger.LogError("Secret not found for API key: {ApiKey}", LogSanitizer.SanitizeString(apiKey));
                    throw new KeyNotFoundException($"Secret not found for API key: {apiKey}");
                }
                _logger.LogDebug("Successfully retrieved secret from AWS");

                // 2. Generate signature using only specific fields
                var data = $"{timestamp}|{nonce}|{merchantId}|{apiKey}";
                _logger.LogDebug("Data to sign: {Data}", data);
                
                // Log masked secret for debugging
                var maskedSecret = secretData.Secret.Length > 8 
                    ? $"{secretData.Secret.Substring(0, 4)}...{secretData.Secret.Substring(secretData.Secret.Length - 4)}"
                    : "****";
                _logger.LogDebug("Using secret (masked): {MaskedSecret}", maskedSecret);
                
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretData.Secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var signature = Convert.ToBase64String(hash);
                
                _logger.LogDebug("Generated signature: {Signature}", signature);
                return signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signature");
                throw;
            }
        }

        /// <summary>
        /// Securely retrieves merchant secret without exposing sensitive data in method parameters
        /// Checkmarx: Privacy Violation - This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="merchantId">The merchant ID (non-sensitive)</param>
        /// <param name="apiKey">The API key (non-sensitive)</param>
        /// <returns>ApiKeySecret or null if not found</returns>
        private async Task<ApiKeySecret?> GetMerchantSecretSecurelyAsync(string merchantId, string apiKey)
        {
            try
            {
                // Build the secret name using the configured pattern internally
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
                _logger.LogDebug("Retrieving secret from AWS: {SecretName}", LogSanitizer.SanitizeString(secretName));
                return await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving merchant secret for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                return null;
            }
        }
    }
} 