using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Repositories;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Data;
using FeeNominalService.Services.AWS;
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Utils;

namespace FeeNominalService.Services
{
    /// <summary>
    /// Service for managing API keys
    /// </summary>
    public class ApiKeyService : IApiKeyService
    {
        private readonly IAwsSecretsManagerService _secretsManager;
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly ILogger<ApiKeyService> _logger;
        private readonly ApiKeyConfiguration _settings;
        private readonly ApplicationDbContext _context;
        private readonly IApiKeyGenerator _apiKeyGenerator;
        private readonly SecretNameFormatter _secretNameFormatter;

        public ApiKeyService(
            IAwsSecretsManagerService secretsManager,
            IApiKeyRepository apiKeyRepository,
            IMerchantRepository merchantRepository,
            ILogger<ApiKeyService> logger,
            IOptions<ApiKeyConfiguration> settings,
            ApplicationDbContext context,
            IApiKeyGenerator apiKeyGenerator,
            SecretNameFormatter secretNameFormatter)
        {
            _secretsManager = secretsManager;
            _apiKeyRepository = apiKeyRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
            _settings = settings.Value;
            _context = context;
            _apiKeyGenerator = apiKeyGenerator;
            _secretNameFormatter = secretNameFormatter;
        }

        /// <summary>
        /// Validates an API key and its signature
        /// </summary>
        /// <param name="merchantId">The internal merchant ID (GUID)</param>
        /// <param name="apiKey">The API key to validate</param>
        /// <param name="timestamp">Request timestamp</param>
        /// <param name="nonce">Request nonce</param>
        /// <param name="signature">Request signature</param>
        /// <param name="serviceName">The name of the service requesting the API key (e.g., "feenominal", "merchant-service")</param>
        /// <returns>True if valid, false otherwise</returns>
        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey, string timestamp, string nonce, string signature, string serviceName)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("ApiKey is null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(timestamp))
            {
                _logger.LogWarning("Timestamp is null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(nonce))
            {
                _logger.LogWarning("Nonce is null or empty");
                return false;
            }

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Signature is null or empty");
                return false;
            }

            // 1. Get API key from database
            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
            if (apiKeyEntity == null)
            {
                _logger.LogWarning("API key {ApiKey} not found", LogSanitizer.SanitizeString(apiKey));
                return false;
            }

            // Allow revoked keys to pass authentication (business logic will handle them)
            // Only reject if key doesn't exist or is expired
            if (apiKeyEntity.Status == "EXPIRED" || 
                (apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow))
            {
                // Mark as expired if not already
                if (apiKeyEntity.Status != "EXPIRED")
                {
                    apiKeyEntity.Status = "EXPIRED";
                    apiKeyEntity.UpdatedAt = DateTime.UtcNow;
                    await _apiKeyRepository.UpdateAsync(apiKeyEntity);
                }
                _logger.LogWarning("API key {ApiKey} has expired.", LogSanitizer.SanitizeString(apiKey));
                return false;
            }

            // Check if this is an admin API key
            var isAdminKey = apiKeyEntity.IsAdmin || apiKeyEntity.MerchantId == null;

            if (isAdminKey)
            {
                return await ValidateAdminApiKeyAsync(apiKey, timestamp, nonce, signature, serviceName);
            }
            else
            {
                // For merchant keys, merchant ID is required
                if (string.IsNullOrEmpty(merchantId))
                {
                    _logger.LogWarning("MerchantId is required for non-admin API keys");
                    return false;
                }

                return await ValidateMerchantApiKeyAsync(merchantId, apiKey, timestamp, nonce, signature, serviceName);
            }
        }

        private async Task<bool> ValidateAdminApiKeyAsync(string apiKey, string timestamp, string nonce, string signature, string serviceName)
        {
            _logger.LogInformation("=== ADMIN API KEY VALIDATION DEBUG ===");
            _logger.LogInformation("Input parameters: ApiKey={ApiKey}, Timestamp={Timestamp}, Nonce={Nonce}, Signature={Signature}", 
                LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(timestamp), LogSanitizer.SanitizeString(nonce), LogSanitizer.SanitizeString(signature));

            // Get admin secret from AWS Secrets Manager
            var adminSecretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
            _logger.LogInformation("Looking for admin secret with name: {AdminSecretName}", LogSanitizer.SanitizeString(adminSecretName));
            
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(adminSecretName);

            if (secret == null)
            {
                _logger.LogWarning("Admin secret not found for API key {ApiKey}", LogSanitizer.SanitizeString(apiKey));
                return false;
            }

            _logger.LogInformation("Admin secret found: ApiKey={SecretApiKey}, IsRevoked={IsRevoked}, Status={Status}, Scope={Scope}", 
                LogSanitizer.SanitizeString(secret.ApiKey), secret.IsRevoked, secret.Status, LogSanitizer.SanitizeString(secret.Scope));

            // Debug log for secret used in signature calculation (masked)
            if (!string.IsNullOrEmpty(secret.Secret) && secret.Secret.Length > 8)
            {
                var maskedSecret = $"{secret.Secret.Substring(0, 4)}...{secret.Secret.Substring(secret.Secret.Length - 4)}";
                _logger.LogInformation("Using masked admin secret for signature calculation: {MaskedSecret}", maskedSecret);
            }
            else
            {
                _logger.LogInformation("Using admin secret for signature calculation: {MaskedSecret}", LogSanitizer.SanitizeString(secret.Secret));
            }

            // For admin keys, use empty string as merchant ID in signature
            var merchantIdForSignature = string.Empty;
            _logger.LogInformation("Signature calculation inputs: Secret={MaskedSecret}, Timestamp={Timestamp}, Nonce={Nonce}, MerchantId={MerchantId}, ApiKey={ApiKey}", 
                secret.Secret.Length > 8 ? $"{secret.Secret.Substring(0, 4)}...{secret.Secret.Substring(secret.Secret.Length - 4)}" : secret.Secret,
                LogSanitizer.SanitizeString(timestamp), LogSanitizer.SanitizeString(nonce), LogSanitizer.SanitizeString(merchantIdForSignature), LogSanitizer.SanitizeString(apiKey));

            var expectedSignature = GenerateSignature(secret.Secret, timestamp, nonce, merchantIdForSignature, apiKey);
            _logger.LogInformation("Expected signature: {ExpectedSignature}", LogSanitizer.SanitizeString(expectedSignature));
            _logger.LogInformation("Received signature: {ReceivedSignature}", LogSanitizer.SanitizeString(signature));
            _logger.LogInformation("Signatures match: {SignaturesMatch}", string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase));
            _logger.LogInformation("=== END ADMIN API KEY VALIDATION DEBUG ===");

            return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> ValidateMerchantApiKeyAsync(string merchantId, string apiKey, string timestamp, string nonce, string signature, string serviceName)
        {
            // Get secret from AWS Secrets Manager using internal merchant ID
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

            if (secret == null)
            {
                _logger.LogWarning("Secret not found for API key {ApiKey}", LogSanitizer.SanitizeString(apiKey));
                return false;
            }

            // Debug log for secret used in signature calculation (masked)
            if (!string.IsNullOrEmpty(secret.Secret) && secret.Secret.Length > 8)
            {
                var maskedSecret = $"{secret.Secret.Substring(0, 4)}...{secret.Secret.Substring(secret.Secret.Length - 4)}";
                _logger.LogDebug("Using masked secret for signature calculation: {MaskedSecret}", maskedSecret);
            }
            else
            {
                _logger.LogDebug("Using secret for signature calculation: {MaskedSecret}", LogSanitizer.SanitizeString(secret.Secret));
            }

            // Validate signature
            var expectedSignature = GenerateSignature(secret.Secret, timestamp, nonce, merchantId, apiKey);
            return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets API key information for a merchant
        /// </summary>
        /// <param name="merchantId">The internal merchant ID (GUID)</param>
        /// <returns>API key information</returns>
        public async Task<ApiKeyInfo> GetApiKeyAsync(string merchantId)
        {
            _logger.LogInformation("Getting API key info for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));

            // Check if merchant exists
            var merchantGuid = Guid.Parse(merchantId);
            var merchant = await _merchantRepository.GetByIdAsync(merchantGuid);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant {merchantId} not found");
            }

            // Get the API key from database
            var apiKeyEntity = await _apiKeyRepository.GetByMerchantIdAsync(merchant.MerchantId);
            if (!apiKeyEntity.Any())
            {
                throw new KeyNotFoundException($"No API key found for merchant {merchantId}");
            }

            var activeKey = apiKeyEntity.FirstOrDefault(k => k.Status == "ACTIVE");
            if (activeKey == null)
            {
                throw new KeyNotFoundException($"No active API key found for merchant {merchantId}");
            }

            // Get the secret from AWS Secrets Manager using internal merchant ID
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchant.MerchantId, activeKey.Key);
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
            if (secret == null)
            {
                throw new KeyNotFoundException($"API key {activeKey.Key} not found for merchant {merchantId}");
            }

            // Get total usage count
            var usageCount = await _context.ApiKeyUsages
                .Where(u => u.ApiKeyId == activeKey.Id)
                .SumAsync(u => u.RequestCount);

            return new ApiKeyInfo
            {
                ApiKey = activeKey.Key,
                MerchantId = activeKey.MerchantId, // Now nullable, no fallback needed
                Status = activeKey.Status,
                CreatedAt = activeKey.CreatedAt,
                LastRotatedAt = activeKey.LastRotatedAt,
                RevokedAt = activeKey.RevokedAt,
                IsRevoked = activeKey.Status == "REVOKED" || activeKey.RevokedAt.HasValue,
                IsExpired = activeKey.ExpiresAt.HasValue && activeKey.ExpiresAt.Value < DateTime.UtcNow,
                Description = activeKey.Description ?? string.Empty,
                RateLimit = activeKey.RateLimit,
                AllowedEndpoints = activeKey.AllowedEndpoints,
                ExpiresAt = activeKey.ExpiresAt,
                LastUsedAt = activeKey.LastUsedAt,
                UsageCount = 0 // TODO: Implement usage tracking
            };
        }

        /// <summary>
        /// Updates an existing API key
        /// </summary>
        /// <param name="request">The update request containing the internal merchant ID (GUID)</param>
        /// <param name="onboardingMetadata">Metadata about the onboarding/update event (admin, reference, timestamp)</param>
        /// <returns>Updated API key information</returns>
        public async Task<ApiKeyInfo> UpdateApiKeyAsync(UpdateApiKeyRequest request, OnboardingMetadata onboardingMetadata)
        {
            _logger.LogInformation("Updating API key for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(request.MerchantId));

            // 1. First check if merchant exists
            var merchant = await _merchantRepository.GetByIdAsync(Guid.Parse(request.MerchantId));
            if (merchant == null)
            {
                _logger.LogWarning("Merchant {MerchantId} not found during API key update", LogSanitizer.SanitizeMerchantId(request.MerchantId));
                throw new KeyNotFoundException($"Merchant {request.MerchantId} not found");
            }

            // 2. Get the specific API key to update
            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(request.ApiKey);
            if (apiKeyEntity == null)
            {
                _logger.LogWarning("API key {ApiKey} not found", LogSanitizer.SanitizeString(request.ApiKey));
                throw new KeyNotFoundException($"API key {request.ApiKey} not found");
            }

            // 3. Validate the API key belongs to the merchant
            if (apiKeyEntity.MerchantId != merchant.MerchantId)
            {
                _logger.LogWarning("API key {ApiKey} does not belong to merchant {MerchantId}", LogSanitizer.SanitizeString(request.ApiKey), LogSanitizer.SanitizeMerchantId(request.MerchantId));
                throw new InvalidOperationException($"API key {request.ApiKey} does not belong to merchant {request.MerchantId}");
            }

            // 4. Check if the API key is active
            if (apiKeyEntity.Status != "ACTIVE")
            {
                _logger.LogWarning("API key {ApiKey} is not active (status: {Status})", LogSanitizer.SanitizeString(request.ApiKey), LogSanitizer.SanitizeString(apiKeyEntity.Status));
                throw new InvalidOperationException($"API key {request.ApiKey} is not active (status: {apiKeyEntity.Status})");
            }

            // 4. Validate allowed endpoints
            if (!apiKeyEntity.IsAdmin && request.AllowedEndpoints != null && request.AllowedEndpoints.Any())
            {
                foreach (var endpoint in request.AllowedEndpoints)
                {
                    if (endpoint.StartsWith("/api/v1/admin/", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"Merchant API keys cannot include admin endpoints: {endpoint}");
                    }
                }
            }

            try
            {
                // 5. Update API key
                apiKeyEntity.Description = request.Description ?? apiKeyEntity.Description ?? string.Empty;
                apiKeyEntity.RateLimit = request.RateLimit ?? apiKeyEntity.RateLimit;
                apiKeyEntity.AllowedEndpoints = request.AllowedEndpoints ?? apiKeyEntity.AllowedEndpoints;
                apiKeyEntity.OnboardingReference = onboardingMetadata.OnboardingReference;
                apiKeyEntity.OnboardingTimestamp = onboardingMetadata.OnboardingTimestamp == default ? DateTime.UtcNow : onboardingMetadata.OnboardingTimestamp;
                await _apiKeyRepository.UpdateAsync(apiKeyEntity);
                _logger.LogInformation("Successfully updated API key for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(request.MerchantId));

                // 6. Get the secret from AWS Secrets Manager using internal merchant ID
                var secretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{request.ApiKey}";
                var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                if (secret == null)
                {
                    _logger.LogWarning("Secret not found for API key {ApiKey} of merchant {MerchantId}", 
                        LogSanitizer.SanitizeString(apiKeyEntity.Key), LogSanitizer.SanitizeMerchantId(request.MerchantId));
                    throw new KeyNotFoundException($"Secret not found for API key of merchant {request.MerchantId}");
                }

                return new ApiKeyInfo
                {
                    ApiKey = apiKeyEntity.Key,
                    MerchantId = apiKeyEntity.MerchantId, // Now nullable, no fallback needed
                    Description = apiKeyEntity.Description ?? string.Empty,
                    RateLimit = apiKeyEntity.RateLimit,
                    AllowedEndpoints = apiKeyEntity.AllowedEndpoints,
                    Status = apiKeyEntity.Status,
                    CreatedAt = apiKeyEntity.CreatedAt,
                    LastRotatedAt = apiKeyEntity.LastRotatedAt,
                    LastUsedAt = apiKeyEntity.LastUsedAt,
                    RevokedAt = apiKeyEntity.RevokedAt,
                    ExpiresAt = apiKeyEntity.ExpiresAt,
                    IsRevoked = secret?.IsRevoked ?? false,
                    IsExpired = apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow,
                    OnboardingMetadata = new OnboardingMetadata
                    {
                        AdminUserId = apiKeyEntity.CreatedBy ?? string.Empty,
                        OnboardingReference = apiKeyEntity.OnboardingReference ?? string.Empty,
                        OnboardingTimestamp = apiKeyEntity.OnboardingTimestamp ?? apiKeyEntity.CreatedAt
                    }
                };
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                _logger.LogError(ex, "Error updating API key for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(request.MerchantId));
                throw;
            }
        }

        /// <summary>
        /// Revokes an API key
        /// </summary>
        /// <param name="request">The revocation request containing the internal merchant ID (GUID)</param>
        /// <returns>True if successful</returns>
        public async Task<bool> RevokeApiKeyAsync(RevokeApiKeyRequest request)
        {
            _logger.LogInformation("Starting API key revocation process for merchant {MerchantId}, API key {ApiKey}", 
                LogSanitizer.SanitizeMerchantId(request.MerchantId), LogSanitizer.SanitizeString(request.ApiKey));

            // 1. Validate merchant exists
            var merchant = await _merchantRepository.GetByIdAsync(Guid.Parse(request.MerchantId));
            if (merchant == null)
            {
                _logger.LogWarning("Merchant {MerchantId} not found during API key revocation", LogSanitizer.SanitizeMerchantId(request.MerchantId));
                throw new KeyNotFoundException($"Merchant {request.MerchantId} not found");
            }

            // 2. Get API key from database
            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(request.ApiKey);
            if (apiKeyEntity == null)
            {
                _logger.LogWarning("API key {ApiKey} not found during revocation", LogSanitizer.SanitizeString(request.ApiKey));
                throw new KeyNotFoundException($"API key {request.ApiKey} not found");
            }

            // 3. Validate API key belongs to merchant
            if (apiKeyEntity.MerchantId != merchant.MerchantId)
            {
                _logger.LogWarning("API key {ApiKey} does not belong to merchant {MerchantId}", LogSanitizer.SanitizeString(request.ApiKey), LogSanitizer.SanitizeMerchantId(request.MerchantId));
                throw new InvalidOperationException($"API key {request.ApiKey} does not belong to merchant {request.MerchantId}");
            }

            // 4. Update API key status
            apiKeyEntity.Status = "REVOKED";
            apiKeyEntity.RevokedAt = DateTime.UtcNow;
            apiKeyEntity.IsActiveInDb = false;  // Set is_active to false in database
            await _apiKeyRepository.UpdateAsync(apiKeyEntity);

            // 5. Update secret in AWS Secrets Manager
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchant.MerchantId, request.ApiKey);
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
            if (secret != null)
            {
                secret.Status = "REVOKED";
                secret.IsRevoked = true;
                secret.RevokedAt = DateTime.UtcNow;
                await _secretsManager.UpdateSecretAsync(secretName, secret); // <-- Use update, not create
            }

            _logger.LogInformation("Successfully revoked API key {ApiKey} for merchant {MerchantId}", LogSanitizer.SanitizeString(request.ApiKey), LogSanitizer.SanitizeMerchantId(request.MerchantId));
            return true;
        }

        /// <inheritdoc />
        public async Task<GenerateApiKeyResponse> RotateApiKeyAsync(string merchantId, OnboardingMetadata onboardingMetadata, string apiKey)
        {
            _logger.LogInformation("Rotating API key for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));

            // Get merchant using internal merchant ID
            var merchant = await _merchantRepository.GetByIdAsync(Guid.Parse(merchantId));
            if (merchant == null)
                throw new KeyNotFoundException($"Merchant with ID {merchantId} not found");

            // Get the API key entity by key
            var keyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
            if (keyEntity == null || keyEntity.MerchantId != merchant.MerchantId)
                throw new InvalidOperationException($"API key does not belong to merchant or does not exist");

            // Prevent rotation of revoked key (service-level check)
            if (keyEntity.Status == "REVOKED")
                throw new InvalidOperationException("Cannot rotate a revoked API key.");

            // Get active API key
            var activeKeys = await _apiKeyRepository.GetByMerchantIdAsync(merchant.MerchantId);
            var activeKey = activeKeys.FirstOrDefault(k => k.Status == "ACTIVE");
            if (activeKey == null)
                throw new InvalidOperationException($"No active API key found for merchant {merchantId}");

            // Generate new API key and secret
            var newApiKey = GenerateApiKey();
            var newSecret = GenerateSecret();

            var originalName = string.IsNullOrWhiteSpace(activeKey.Name)
                ? $"APIKEY_{DateTime.UtcNow:yyyyMMddHHmmss}"
                : activeKey.Name;

            activeKey.Status = "ROTATED";
            activeKey.LastRotatedAt = DateTime.UtcNow;
            activeKey.Name = $"{originalName}_ROTATED_{DateTime.UtcNow:yyyyMMddHHmmss}";
            activeKey.IsActiveInDb = false;  // Set is_active to false for rotated key
            await _apiKeyRepository.UpdateAsync(activeKey);

            var newApiKeyEntity = new Models.ApiKey.ApiKey
            {
                MerchantId = merchant.MerchantId,
                Key = newApiKey,
                Name = originalName,
                Description = activeKey.Description,
                RateLimit = activeKey.RateLimit,
                AllowedEndpoints = activeKey.AllowedEndpoints,
                Status = "ACTIVE",
                IsActiveInDb = true,  // Set is_active to true for new active key
                CreatedBy = onboardingMetadata.AdminUserId,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                ExpirationDays = activeKey.ExpirationDays > 0 ? activeKey.ExpirationDays : 365,
                Purpose = activeKey.Purpose,
                LastRotatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                OnboardingReference = onboardingMetadata.OnboardingReference,
                OnboardingTimestamp = onboardingMetadata.OnboardingTimestamp == default ? DateTime.UtcNow : onboardingMetadata.OnboardingTimestamp
            };
            await _apiKeyRepository.CreateAsync(newApiKeyEntity);

            var newSecretName = _secretNameFormatter.FormatMerchantSecretName(merchant.MerchantId, newApiKey);
            var newSecretValue = new ApiKeySecret
            {
                ApiKey = newApiKey,
                Secret = newSecret,
                MerchantId = merchant.MerchantId,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            };
            await _secretsManager.StoreSecretAsync(newSecretName, JsonSerializer.Serialize(newSecretValue));

            return new GenerateApiKeyResponse
            {
                MerchantId = merchant.MerchantId,
                ExternalMerchantId = merchant.ExternalMerchantId,
                MerchantName = merchant.Name ?? string.Empty,
                ApiKey = newApiKey,
                Description = newApiKeyEntity.Description,
                Secret = newSecret,
                ExpiresAt = newApiKeyEntity.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                RateLimit = newApiKeyEntity.RateLimit,
                AllowedEndpoints = newApiKeyEntity.AllowedEndpoints ?? Array.Empty<string>(),
                Purpose = newApiKeyEntity.Purpose,
                OnboardingMetadata = new OnboardingMetadata
                {
                    AdminUserId = onboardingMetadata.AdminUserId,
                    OnboardingReference = onboardingMetadata.OnboardingReference,
                    OnboardingTimestamp = onboardingMetadata.OnboardingTimestamp == default ? DateTime.UtcNow : onboardingMetadata.OnboardingTimestamp
                }
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ApiKeyInfo>> GetMerchantApiKeysAsync(string merchantId)
        {
            _logger.LogInformation("Getting all API keys for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));

            var merchant = await _merchantRepository.GetByIdAsync(Guid.Parse(merchantId));
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant {merchantId} not found");
            }

            // Get API keys from database
            var apiKeys = await _apiKeyRepository.GetByMerchantIdAsync(merchant.MerchantId);
            var result = new List<ApiKeyInfo>();

            foreach (var apiKey in apiKeys)
            {
                try
                {
                    // Try to get additional info from Secrets Manager using internal merchant ID
                    var secretName = _secretNameFormatter.FormatMerchantSecretName(merchant.MerchantId, apiKey.Key);
                    var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

                    // Get total usage count
                    var usageCount = await _context.ApiKeyUsages
                        .Where(u => u.ApiKeyId == apiKey.Id)
                        .SumAsync(u => u.RequestCount);

                    result.Add(new ApiKeyInfo
                    {
                        ApiKey = apiKey.Key,
                        MerchantId = apiKey.MerchantId, // Now nullable, no fallback needed
                        Description = apiKey.Description ?? string.Empty,
                        RateLimit = apiKey.RateLimit,
                        AllowedEndpoints = apiKey.AllowedEndpoints,
                        Status = apiKey.Status,
                        CreatedAt = apiKey.CreatedAt,
                        LastRotatedAt = apiKey.LastRotatedAt,
                        LastUsedAt = apiKey.LastUsedAt,
                        RevokedAt = apiKey.RevokedAt,
                        ExpiresAt = apiKey.ExpiresAt,
                        IsRevoked = secret?.IsRevoked ?? false,
                        IsExpired = apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow,
                        UsageCount = usageCount,
                        OnboardingMetadata = new OnboardingMetadata
                        {
                            AdminUserId = apiKey.CreatedBy,
                            OnboardingReference = apiKey.OnboardingReference ?? apiKey.Key,
                            OnboardingTimestamp = apiKey.OnboardingTimestamp ?? apiKey.CreatedAt
                        }
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get secret info for API key {ApiKey}, using database info only", LogSanitizer.SanitizeString(apiKey.Key));
                    // If we can't get the secret, just use the database info
                    var usageCount = await _context.ApiKeyUsages
                        .Where(u => u.ApiKeyId == apiKey.Id)
                        .SumAsync(u => u.RequestCount);

                    result.Add(new ApiKeyInfo
                    {
                        ApiKey = apiKey.Key,
                        MerchantId = apiKey.MerchantId, // Now nullable, no fallback needed
                        Description = apiKey.Description ?? string.Empty,
                        RateLimit = apiKey.RateLimit,
                        AllowedEndpoints = apiKey.AllowedEndpoints,
                        Status = apiKey.Status,
                        CreatedAt = apiKey.CreatedAt,
                        LastRotatedAt = apiKey.LastRotatedAt,
                        LastUsedAt = apiKey.LastUsedAt,
                        RevokedAt = apiKey.RevokedAt,
                        ExpiresAt = apiKey.ExpiresAt,
                        IsRevoked = apiKey.Status == "REVOKED",
                        IsExpired = apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow,
                        UsageCount = usageCount,
                        OnboardingMetadata = new OnboardingMetadata
                        {
                            AdminUserId = apiKey.CreatedBy,
                            OnboardingReference = apiKey.OnboardingReference ?? apiKey.Key,
                            OnboardingTimestamp = apiKey.OnboardingTimestamp ?? apiKey.CreatedAt
                        }
                    });
                }
            }

            _logger.LogInformation("Found {Count} API keys for merchant {MerchantId}", result.Count, LogSanitizer.SanitizeMerchantId(merchantId));
            return result;
        }

        /// <inheritdoc />
        public async Task<ApiKeyInfo> GetApiKeyInfoAsync(string apiKey)
        {
            _logger.LogInformation("Getting API key info for key {ApiKey}", LogSanitizer.SanitizeString(apiKey));

            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
            if (apiKeyEntity == null)
            {
                throw new KeyNotFoundException($"API key {apiKey} not found");
            }

            // For admin API keys, there's no merchant to look up
            if (!apiKeyEntity.MerchantId.HasValue)
            {
                // This is an admin key, return admin info
                return new ApiKeyInfo
                {
                    ApiKey = apiKeyEntity.Key,
                    MerchantId = null,
                    Description = apiKeyEntity.Description ?? string.Empty,
                    RateLimit = apiKeyEntity.RateLimit,
                    AllowedEndpoints = apiKeyEntity.AllowedEndpoints ?? Array.Empty<string>(),
                    Status = apiKeyEntity.Status,
                    CreatedAt = apiKeyEntity.CreatedAt,
                    LastRotatedAt = apiKeyEntity.LastRotatedAt,
                    LastUsedAt = apiKeyEntity.LastUsedAt,
                    RevokedAt = apiKeyEntity.RevokedAt,
                    ExpiresAt = apiKeyEntity.ExpiresAt,
                    IsRevoked = apiKeyEntity.Status == "REVOKED" || apiKeyEntity.RevokedAt.HasValue,
                    IsExpired = apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow,
                    Scope = apiKeyEntity.Scope ?? "admin",
                    IsAdmin = true
                };
            }

            var merchant = await _merchantRepository.GetByIdAsync(apiKeyEntity.MerchantId.Value);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant not found for API key {apiKey}");
            }

            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchant.MerchantId, apiKey);
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

            // Get total usage count
            var usageCount = await _context.ApiKeyUsages
                .Where(u => u.ApiKeyId == apiKeyEntity.Id)
                .SumAsync(u => u.RequestCount);

            return new ApiKeyInfo
            {
                ApiKey = apiKeyEntity.Key,
                MerchantId = apiKeyEntity.MerchantId, // Now nullable, no fallback needed
                Description = apiKeyEntity.Description ?? string.Empty,
                RateLimit = apiKeyEntity.RateLimit,
                AllowedEndpoints = apiKeyEntity.AllowedEndpoints,
                Status = apiKeyEntity.Status,
                CreatedAt = apiKeyEntity.CreatedAt,
                LastRotatedAt = apiKeyEntity.LastRotatedAt,
                LastUsedAt = apiKeyEntity.LastUsedAt,
                RevokedAt = apiKeyEntity.RevokedAt,
                ExpiresAt = apiKeyEntity.ExpiresAt,
                IsRevoked = secret?.IsRevoked ?? false,
                IsExpired = apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow,
                UsageCount = usageCount,
                Scope = apiKeyEntity.Scope ?? "merchant",
                IsAdmin = apiKeyEntity.IsAdmin
            };
        }

        private string GenerateSecureRandomString(int length)
        {
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes).Replace("+", "-").Replace("/", "_").Substring(0, length);
        }

        private string GenerateSignature(string secret, string timestamp, string nonce, string merchantId, string apiKey)
        {
            string data;
            if (string.IsNullOrEmpty(merchantId)) // For admin keys, omit merchantId field entirely
            {
                data = $"{timestamp}|{nonce}|{apiKey}";
            }
            else
            {
                data = $"{timestamp}|{nonce}|{merchantId}|{apiKey}";
            }
            _logger.LogInformation("=== SIGNATURE GENERATION DEBUG ===");
            _logger.LogInformation("Data string being hashed: '{Data}'", LogSanitizer.SanitizeString(data));
            _logger.LogInformation("Data components: Timestamp='{Timestamp}', Nonce='{Nonce}', MerchantId='{MerchantId}', ApiKey='{ApiKey}'", 
                LogSanitizer.SanitizeString(timestamp), LogSanitizer.SanitizeString(nonce), LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeString(apiKey));
            _logger.LogInformation("Secret length: {SecretLength}", secret?.Length ?? 0);
            
            if (string.IsNullOrEmpty(secret))
            {
                _logger.LogError("Secret is null or empty for signature generation");
                throw new ArgumentException("Secret cannot be null or empty for signature generation");
            }
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var signature = Convert.ToBase64String(hash);
            
            _logger.LogInformation("Generated signature: {Signature}", LogSanitizer.SanitizeString(signature));
            _logger.LogInformation("=== END SIGNATURE GENERATION DEBUG ===");
            
            return signature;
        }

        private string GenerateApiKey()
        {
            return GenerateSecureRandomString(32);
        }

        private string GenerateSecret()
        {
            return GenerateSecureRandomString(64);
        }

        public async Task<GenerateApiKeyResponse> GenerateApiKeyAsync(GenerateApiKeyRequest request)
        {
            _logger.LogInformation("Generating new API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));

            // For admin API keys, skip merchant validation
            if (request.IsAdmin)
            {
                // Generate admin API key
                var apiKey = new Models.ApiKey.ApiKey
                {
                    Id = Guid.NewGuid(),
                    Key = GenerateApiKey(),
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(request.ExpirationDays ?? 365),
                    RateLimit = request.RateLimit ?? 1000,
                    AllowedEndpoints = request.AllowedEndpoints ?? Array.Empty<string>(),
                    Purpose = request.Purpose,
                    Name = "ADMIN-API-KEY",
                    Description = request.Description ?? "Admin API Key",
                    MerchantId = null, // Admin keys don't belong to a specific merchant
                    CreatedBy = "admin",
                    ExpirationDays = request.ExpirationDays ?? 365,
                    IsAdmin = true,
                    Scope = "admin", // Admin scope for cross-merchant operations
                    ServiceName = request.ServiceName
                };

                await _apiKeyRepository.CreateAsync(apiKey);

                // Store secret in AWS Secrets Manager or local DB
                // NOTE: For admin secrets, scope must be 'admin', merchant_id must be null, and status must be 'ACTIVE'.
                var adminSecretName = _secretNameFormatter.FormatAdminSecretName(request.ServiceName ?? "feenominal");
                var secretValue = new ApiKeySecret
                {
                    ApiKey = apiKey.Key,
                    Secret = GenerateSecret(),
                    MerchantId = null, // Admin secrets don't belong to a specific merchant
                    CreatedAt = DateTime.UtcNow,
                    LastRotated = null,
                    IsRevoked = false,
                    RevokedAt = null,
                    Status = "ACTIVE",
                    Scope = "admin" // Admin scope for cross-merchant operations
                };
                await _secretsManager.StoreSecretAsync(adminSecretName, JsonSerializer.Serialize(secretValue));

                _logger.LogInformation("Generated admin API key {ApiKeyId}", LogSanitizer.SanitizeGuid(apiKey.Id));

                return new GenerateApiKeyResponse
                {
                    MerchantId = null, // Admin keys don't have a merchant
                    ExternalMerchantId = "ADMIN",
                    MerchantName = "Admin",
                    ApiKey = apiKey.Key,
                    Description = apiKey.Description,
                    Secret = secretValue.Secret,
                    ExpiresAt = apiKey.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                    RateLimit = apiKey.RateLimit,
                    AllowedEndpoints = apiKey.AllowedEndpoints,
                    Purpose = apiKey.Purpose,
                    IsAdmin = true
                };
            }

            // For merchant API keys, get merchant using internal merchant ID
            if (request.MerchantId == null)
            {
                throw new ArgumentException("Merchant ID is required for non-admin API keys");
            }

            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId.Value);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant with ID {request.MerchantId} not found");
            }

            // Check if merchant has reached maximum number of active keys
            var activeKeys = await _apiKeyRepository.GetByMerchantIdAsync(merchant.MerchantId);
            var activeKeysCount = activeKeys.Count(k => k.Status == "ACTIVE");
            if (activeKeysCount >= 5) // Maximum 5 active keys per merchant
            {
                throw new InvalidOperationException("Merchant has reached the maximum number of active API keys (5)");
            }

            // Gather **all** existing names (case-insensitive) for this merchant
            var existingNames = (await _apiKeyRepository.GetByMerchantIdAsync(merchant.MerchantId))
                .Select(k => k.Name?.ToLowerInvariant())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet();

            string uniqueName;
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                uniqueName = request.Name.Trim();
                var baseUniqueName = uniqueName;
                var suffix = 1;
                while (existingNames.Contains(uniqueName.ToLowerInvariant()))
                {
                    uniqueName = $"{baseUniqueName}-{suffix}";
                    suffix++;
                }
            }
            else
            {
                var baseName = merchant.Name?.Trim();
                if (string.IsNullOrWhiteSpace(baseName))
                {
                    baseName = "APIKEY";
                }
                uniqueName = baseName;
                var suffix = 1;
                while (existingNames.Contains(uniqueName.ToLowerInvariant()))
                {
                    uniqueName = $"{baseName}-{suffix}";
                    suffix++;
                }
            }

            // Generate new API key
            var newApiKey = new Models.ApiKey.ApiKey
            {
                Id = Guid.NewGuid(),
                Key = GenerateApiKey(),
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.ExpirationDays ?? 365),
                RateLimit = request.RateLimit ?? 1000,
                AllowedEndpoints = request.AllowedEndpoints ?? Array.Empty<string>(),
                Purpose = request.Purpose,
                Name = uniqueName,
                Description = request.Description ?? $"API Key for merchant {merchant.Name}",
                MerchantId = request.MerchantId,
                CreatedBy = request.IsAdmin ? "admin" : request.OnboardingMetadata?.AdminUserId ?? "unknown",
                ExpirationDays = request.ExpirationDays ?? 365, // Use request value or default to 1 year
                IsAdmin = request.IsAdmin, // Set admin/superuser flag
                Scope = "merchant", // Merchant scope for merchant-specific operations
                ServiceName = "feenominal"
            };
            newApiKey.OnboardingReference =
                (!request.IsAdmin && request.OnboardingMetadata != null && !string.IsNullOrEmpty(request.OnboardingMetadata.OnboardingReference))
                    ? request.OnboardingMetadata.OnboardingReference
                    : null;

            if (!request.IsAdmin && request.OnboardingMetadata == null)
            {
                throw new ArgumentException("OnboardingMetadata is required for non-admin API key requests.");
            }

            if (newApiKey.IsAdmin)
            {
                _logger.LogWarning("Admin/superuser API key generated for merchant {MerchantId} by {AdminUserId}", LogSanitizer.SanitizeGuid(request.MerchantId), LogSanitizer.SanitizeString(request.OnboardingMetadata?.AdminUserId ?? "admin"));
            }

            if (!request.IsAdmin && newApiKey.AllowedEndpoints != null && newApiKey.AllowedEndpoints.Any())
            {
                foreach (var endpoint in newApiKey.AllowedEndpoints)
                {
                    if (endpoint.StartsWith("/api/v1/admin/", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException($"Merchant API keys cannot include admin endpoints: {endpoint}");
                    }
                }
            }

            await _apiKeyRepository.CreateAsync(newApiKey);

            // Store secret in AWS Secrets Manager
            var merchantSecretName = _secretNameFormatter.FormatMerchantSecretName(merchant.MerchantId, newApiKey.Key);
            var newSecretValue = new ApiKeySecret
            {
                ApiKey = newApiKey.Key,
                Secret = GenerateSecret(),
                MerchantId = merchant.MerchantId,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE",
                Scope = "merchant" // Merchant scope for merchant-specific operations
            };
            await _secretsManager.StoreSecretAsync(merchantSecretName, JsonSerializer.Serialize(newSecretValue));

            _logger.LogInformation("Generated new API key {ApiKeyId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(newApiKey.Id), LogSanitizer.SanitizeGuid(merchant.MerchantId));

            return new GenerateApiKeyResponse
            {
                MerchantId = merchant.MerchantId,
                ExternalMerchantId = merchant.ExternalMerchantId,
                MerchantName = merchant.Name ?? string.Empty,
                ApiKey = newApiKey.Key,
                Description = newApiKey.Description,
                Secret = newSecretValue.Secret,
                ExpiresAt = newApiKey.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                RateLimit = newApiKey.RateLimit,
                AllowedEndpoints = newApiKey.AllowedEndpoints ?? Array.Empty<string>(),
                Purpose = newApiKey.Purpose,
                IsAdmin = request.IsAdmin
            };
        }

        /// <summary>
        /// Regenerates the secret for a merchant's API keys
        /// </summary>
        public async Task<ApiKeyResponse> RegenerateSecretAsync(string merchantId)
        {
            _logger.LogInformation("Regenerating secret for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));

            // Get merchant
            var merchant = await _merchantRepository.GetByExternalIdAsync(merchantId);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant with ID {merchantId} not found");
            }

            // Get all active API keys for the merchant
            var activeKeys = (await _apiKeyRepository.GetByMerchantIdAsync(merchant.MerchantId))
                .Where(k => k.Status == "ACTIVE")
                .ToList();
            if (!activeKeys.Any())
            {
                throw new InvalidOperationException($"No active API keys found for merchant {merchantId}");
            }

            // Generate new secret
            var newSecret = GenerateSecureRandomString(64);

            // Store new secret in AWS Secrets Manager
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchant.MerchantId, activeKeys.First().Key);
            var secretValue = new ApiKeySecret
            {
                ApiKey = activeKeys.First().Key,
                Secret = newSecret,
                MerchantId = merchant.MerchantId,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            };
            await _secretsManager.StoreSecretAsync(secretName, JsonSerializer.Serialize(secretValue));

            // Update all active API keys to mark them as rotated
            foreach (var key in activeKeys)
            {
                key.Status = "ROTATED";
                key.LastRotatedAt = DateTime.UtcNow;
                await _apiKeyRepository.UpdateAsync(key);
            }

            // Generate a new API key with the new secret
            var newApiKey = new Models.ApiKey.ApiKey
            {
                Id = Guid.NewGuid(),
                Key = GenerateApiKey(),
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(activeKeys.First().ExpirationDays > 0 ? activeKeys.First().ExpirationDays : 365),
                RateLimit = activeKeys.First().RateLimit,
                AllowedEndpoints = activeKeys.First().AllowedEndpoints,
                Purpose = activeKeys.First().Purpose,
                Name = activeKeys.First().Name,
                Description = "Regenerated after secret loss",
                MerchantId = merchant.MerchantId,
                ExpirationDays = activeKeys.First().ExpirationDays > 0 ? activeKeys.First().ExpirationDays : 365
            };

            await _apiKeyRepository.CreateAsync(newApiKey);

            _logger.LogInformation("Successfully regenerated secret and created new API key for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));

            return new ApiKeyResponse
            {
                ApiKey = newApiKey.Key,
                Secret = newSecret,
                ExpiresAt = newApiKey.ExpiresAt ?? DateTime.UtcNow.AddDays(activeKeys.First().ExpirationDays > 0 ? activeKeys.First().ExpirationDays : 365),
                RateLimit = newApiKey.RateLimit,
                AllowedEndpoints = newApiKey.AllowedEndpoints,
                Purpose = newApiKey.Purpose
            };
        }

        /// <summary>
        /// Generates the initial API key and secret for a merchant
        /// </summary>
        public async Task<GenerateInitialApiKeyResponse> GenerateInitialApiKeyAsync(Guid merchantId, GenerateInitialApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Generating initial API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));

                var merchant = await _merchantRepository.GetByIdAsync(merchantId);
                if (merchant == null)
                {
                    throw new KeyNotFoundException($"Merchant not found with ID {merchantId}");
                }

                // Generate API key and secret
                var apiKey = Guid.NewGuid().ToString("N");
                var secret = Guid.NewGuid().ToString("N");

                // Gather **all** existing names (case-insensitive) for this merchant
                var existingNames = (await _apiKeyRepository.GetByMerchantIdAsync(merchantId))
                    .Select(k => k.Name?.ToLowerInvariant())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToHashSet();

                string uniqueName;
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    uniqueName = request.Name.Trim();
                    var baseUniqueName = uniqueName;
                    var suffix = 1;
                    while (existingNames.Contains(uniqueName.ToLowerInvariant()))
                    {
                        uniqueName = $"{baseUniqueName}-{suffix}";
                        suffix++;
                    }
                }
                else
                {
                    var baseName = merchant.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(baseName))
                    {
                        baseName = "APIKEY";
                    }
                    uniqueName = baseName;
                    var suffix = 1;
                    while (existingNames.Contains(uniqueName.ToLowerInvariant()))
                    {
                        uniqueName = $"{baseName}-{suffix}";
                        suffix++;
                    }
                }

                // Create API key record
                var apiKeyEntity = new Models.ApiKey.ApiKey
                {
                    MerchantId = merchantId,
                    Key = apiKey,
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(request.ExpirationDays ?? 365),
                    RateLimit = request.RateLimit ?? 1000,
                    AllowedEndpoints = request.AllowedEndpoints ?? Array.Empty<string>(),
                    Description = request.Description,
                    Purpose = request.Purpose,
                    Name = uniqueName,
                    CreatedBy = request.OnboardingMetadata.AdminUserId,
                    OnboardingReference = request.OnboardingMetadata.OnboardingReference,
                    ExpirationDays = request.ExpirationDays ?? 365, // Use request value or default to 1 year
                    OnboardingTimestamp = request.OnboardingMetadata?.OnboardingTimestamp ?? DateTime.UtcNow
                };

                var createdApiKey = await _apiKeyRepository.CreateAsync(apiKeyEntity);

                // Store secret in AWS Secrets Manager
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
                var secretValue = new ApiKeySecret
                {
                    ApiKey = apiKey,
                    Secret = secret,
                    MerchantId = merchantId,
                    CreatedAt = DateTime.UtcNow,
                    LastRotated = null,
                    IsRevoked = false,
                    RevokedAt = null,
                    Status = "ACTIVE"
                };
                await _secretsManager.StoreSecretAsync(secretName, JsonSerializer.Serialize(secretValue));

                // Debug log for generated secret (masked)
                if (!string.IsNullOrEmpty(secret) && secret.Length > 8)
                {
                    var maskedSecret = $"{secret.Substring(0, 4)}...{secret.Substring(secret.Length - 4)}";
                    _logger.LogDebug("Initial API key generated with masked secret: {MaskedSecret}", maskedSecret);
                }
                else
                {
                    _logger.LogDebug("Initial API key generated with secret: {MaskedSecret}", secret);
                }

                return new GenerateInitialApiKeyResponse
                {
                    MerchantId = merchantId,
                    ExternalMerchantId = merchant.ExternalMerchantId,
                    ExternalMerchantGuid = merchant.ExternalMerchantGuid,
                    MerchantName = merchant.Name ?? string.Empty,
                    Status = merchant.Status?.Code ?? "ACTIVE",
                    ApiKey = apiKey,
                    ApiKeyId = createdApiKey.Id,
                    ExpiresAt = apiKeyEntity.ExpiresAt ?? DateTime.UtcNow.AddDays(request.ExpirationDays ?? 365),
                    Secret = secret,
                    CreatedAt = apiKeyEntity.CreatedAt,
                    RateLimit = apiKeyEntity.RateLimit,
                    AllowedEndpoints = apiKeyEntity.AllowedEndpoints ?? Array.Empty<string>(),
                    Description = apiKeyEntity.Description ?? string.Empty,
                    Purpose = apiKeyEntity.Purpose,
                    OnboardingMetadata = request.OnboardingMetadata ?? new OnboardingMetadata()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating initial API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                throw;
            }
        }

        // Update RotateAdminApiKeyAsync to accept a serviceName parameter
        public async Task<GenerateApiKeyResponse> RotateAdminApiKeyAsync(string serviceName)
        {
            _logger.LogInformation("Rotating admin API key");
            var adminKey = await _apiKeyRepository.GetAdminKeyAsync();
            if (adminKey == null || adminKey.Status != "ACTIVE")
            {
                throw new InvalidOperationException("No active admin API key found to rotate.");
            }
            adminKey.Status = "REVOKED";
            adminKey.RevokedAt = DateTime.UtcNow;
            await _apiKeyRepository.UpdateAsync(adminKey);
            var newKey = GenerateApiKey();
            var newSecret = GenerateSecret();
            var newAdminKey = new Models.ApiKey.ApiKey
            {
                Id = Guid.NewGuid(),
                Key = newKey,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/bulk-sale-complete" },
                Purpose = "ADMIN",
                Name = "ADMIN-API-KEY",
                Description = "Rotated admin API key",
                MerchantId = null,
                CreatedBy = "admin",
                ExpirationDays = 365,
                IsAdmin = true,
                Scope = "admin",
                ServiceName = serviceName
            };
            await _apiKeyRepository.CreateAsync(newAdminKey);
            var adminSecretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
            var secretValue = new ApiKeySecret
            {
                ApiKey = newKey,
                Secret = newSecret,
                MerchantId = null,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE",
                Scope = "admin"
            };
            await _secretsManager.StoreSecretAsync(adminSecretName, JsonSerializer.Serialize(secretValue));
            _logger.LogInformation("Rotated admin API key. Old key revoked, new key generated.");
            return new GenerateApiKeyResponse
            {
                MerchantId = null,
                ExternalMerchantId = "ADMIN",
                MerchantName = "Admin",
                ApiKey = newKey,
                Description = newAdminKey.Description,
                Secret = newSecret,
                ExpiresAt = newAdminKey.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                RateLimit = newAdminKey.RateLimit,
                AllowedEndpoints = newAdminKey.AllowedEndpoints,
                Purpose = newAdminKey.Purpose,
                IsAdmin = true
            };
        }

        // Update RevokeAdminApiKeyAsync to accept a serviceName parameter
        public async Task<ApiKeyRevokeResponse> RevokeAdminApiKeyAsync(string serviceName)
        {
            _logger.LogInformation("Revoking admin API key");
            var adminKey = await _apiKeyRepository.GetAdminKeyAsync();
            if (adminKey == null || adminKey.Status != "ACTIVE")
            {
                throw new InvalidOperationException("No active admin API key found to revoke.");
            }
            adminKey.Status = "REVOKED";
            adminKey.RevokedAt = DateTime.UtcNow;
            await _apiKeyRepository.UpdateAsync(adminKey);
            var adminSecretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(adminSecretName);
            if (secret != null)
            {
                secret.Status = "REVOKED";
                secret.IsRevoked = true;
                secret.RevokedAt = DateTime.UtcNow;
                await _secretsManager.UpdateSecretAsync(adminSecretName, secret);
            }
            _logger.LogInformation("Admin API key revoked.");
            return new ApiKeyRevokeResponse
            {
                ApiKey = adminKey.Key,
                RevokedAt = adminKey.RevokedAt ?? DateTime.UtcNow,
                Status = adminKey.Status
            };
        }
    }
} 