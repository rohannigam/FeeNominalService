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
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Repositories;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Data;
using FeeNominalService.Services.AWS;

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

        public ApiKeyService(
            IAwsSecretsManagerService secretsManager,
            IApiKeyRepository apiKeyRepository,
            IMerchantRepository merchantRepository,
            ILogger<ApiKeyService> logger,
            IOptions<ApiKeyConfiguration> settings,
            ApplicationDbContext context,
            IApiKeyGenerator apiKeyGenerator)
        {
            _secretsManager = secretsManager;
            _apiKeyRepository = apiKeyRepository;
            _merchantRepository = merchantRepository;
            _logger = logger;
            _settings = settings.Value;
            _context = context;
            _apiKeyGenerator = apiKeyGenerator;
        }

        /// <summary>
        /// Validates an API key and its signature
        /// </summary>
        /// <param name="merchantId">The internal merchant ID (GUID)</param>
        /// <param name="apiKey">The API key to validate</param>
        /// <param name="timestamp">Request timestamp</param>
        /// <param name="nonce">Request nonce</param>
        /// <param name="signature">Request signature</param>
        /// <returns>True if valid, false otherwise</returns>
        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey, string timestamp, string nonce, string signature)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(merchantId))
            {
                _logger.LogWarning("MerchantId is null or empty");
                return false;
            }

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
                _logger.LogWarning("API key {ApiKey} not found", apiKey);
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
                _logger.LogWarning("API key {ApiKey} has expired.", apiKey);
                return false;
            }

            // 2. Get secret from AWS Secrets Manager using internal merchant ID
            var secretName = $"feenominal/merchants/{merchantId:D}/apikeys/{apiKey}";
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

            if (secret == null)
            {
                _logger.LogWarning("Secret not found for API key {ApiKey}", apiKey);
                return false;
            }

            // Allow revoked secrets to pass authentication (business logic will handle them)
            // Only reject if secret doesn't exist

            // Debug log for secret used in signature calculation (masked)
            if (!string.IsNullOrEmpty(secret.Secret) && secret.Secret.Length > 8)
            {
                var maskedSecret = $"{secret.Secret.Substring(0, 4)}...{secret.Secret.Substring(secret.Secret.Length - 4)}";
                _logger.LogDebug("Using masked secret for signature calculation: {MaskedSecret}", maskedSecret);
            }
            else
            {
                _logger.LogDebug("Using secret for signature calculation: {MaskedSecret}", secret.Secret);
            }

            // 3. Validate signature
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
            _logger.LogInformation("Getting API key info for merchant {MerchantId}", merchantId);

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
            var secretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{activeKey.Key}";
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
                MerchantId = merchant.MerchantId,
                Description = activeKey.Description ?? string.Empty,
                RateLimit = activeKey.RateLimit,
                AllowedEndpoints = activeKey.AllowedEndpoints,
                Status = activeKey.Status,
                CreatedAt = activeKey.CreatedAt,
                LastRotatedAt = activeKey.LastRotatedAt,
                LastUsedAt = activeKey.LastUsedAt,
                RevokedAt = activeKey.RevokedAt,
                ExpiresAt = activeKey.ExpiresAt,
                IsRevoked = secret.IsRevoked,
                IsExpired = activeKey.ExpiresAt.HasValue && activeKey.ExpiresAt.Value < DateTime.UtcNow,
                UsageCount = usageCount
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
            _logger.LogInformation("Updating API key for merchant {MerchantId}", request.MerchantId);

            // 1. First check if merchant exists
            var merchant = await _merchantRepository.GetByIdAsync(Guid.Parse(request.MerchantId));
            if (merchant == null)
            {
                _logger.LogWarning("Merchant {MerchantId} not found during API key update", request.MerchantId);
                throw new KeyNotFoundException($"Merchant {request.MerchantId} not found");
            }

            // 2. Get the specific API key to update
            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(request.ApiKey);
            if (apiKeyEntity == null)
            {
                _logger.LogWarning("API key {ApiKey} not found", request.ApiKey);
                throw new KeyNotFoundException($"API key {request.ApiKey} not found");
            }

            // 3. Validate the API key belongs to the merchant
            if (apiKeyEntity.MerchantId != merchant.MerchantId)
            {
                _logger.LogWarning("API key {ApiKey} does not belong to merchant {MerchantId}", request.ApiKey, request.MerchantId);
                throw new InvalidOperationException($"API key {request.ApiKey} does not belong to merchant {request.MerchantId}");
            }

            // 4. Check if the API key is active
            if (apiKeyEntity.Status != "ACTIVE")
            {
                _logger.LogWarning("API key {ApiKey} is not active (status: {Status})", request.ApiKey, apiKeyEntity.Status);
                throw new InvalidOperationException($"API key {request.ApiKey} is not active (status: {apiKeyEntity.Status})");
            }

            // 4. Validate allowed endpoints
            if (request.AllowedEndpoints != null && request.AllowedEndpoints.Any())
            {
                // Validate each endpoint
                foreach (var endpoint in request.AllowedEndpoints)
                {
                    if (!endpoint.StartsWith("/api/"))
                    {
                        throw new ArgumentException($"Invalid endpoint format: {endpoint}. Endpoints must start with /api/");
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
                _logger.LogInformation("Successfully updated API key for merchant {MerchantId}", request.MerchantId);

                // 6. Get the secret from AWS Secrets Manager using internal merchant ID
                var secretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{request.ApiKey}";
                var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                if (secret == null)
                {
                    _logger.LogWarning("Secret not found for API key {ApiKey} of merchant {MerchantId}", 
                        apiKeyEntity.Key, request.MerchantId);
                    throw new KeyNotFoundException($"Secret not found for API key of merchant {request.MerchantId}");
                }

                return new ApiKeyInfo
                {
                    ApiKey = apiKeyEntity.Key,
                    MerchantId = merchant.MerchantId,   
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
                _logger.LogError(ex, "Error updating API key for merchant {MerchantId}", request.MerchantId);
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
                request.MerchantId, request.ApiKey);

            // 1. Validate merchant exists
            var merchant = await _merchantRepository.GetByIdAsync(Guid.Parse(request.MerchantId));
            if (merchant == null)
            {
                _logger.LogWarning("Merchant {MerchantId} not found during API key revocation", request.MerchantId);
                throw new KeyNotFoundException($"Merchant {request.MerchantId} not found");
            }

            // 2. Get API key from database
            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(request.ApiKey);
            if (apiKeyEntity == null)
            {
                _logger.LogWarning("API key {ApiKey} not found during revocation", request.ApiKey);
                throw new KeyNotFoundException($"API key {request.ApiKey} not found");
            }

            // 3. Validate API key belongs to merchant
            if (apiKeyEntity.MerchantId != merchant.MerchantId)
            {
                _logger.LogWarning("API key {ApiKey} does not belong to merchant {MerchantId}", request.ApiKey, request.MerchantId);
                throw new InvalidOperationException($"API key {request.ApiKey} does not belong to merchant {request.MerchantId}");
            }

            // 4. Update API key status
            apiKeyEntity.Status = "REVOKED";
            apiKeyEntity.RevokedAt = DateTime.UtcNow;
            apiKeyEntity.IsActiveInDb = false;  // Set is_active to false in database
            await _apiKeyRepository.UpdateAsync(apiKeyEntity);

            // 5. Update secret in AWS Secrets Manager
            var secretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{request.ApiKey}";
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
            if (secret != null)
            {
                secret.Status = "REVOKED";
                secret.IsRevoked = true;
                secret.RevokedAt = DateTime.UtcNow;
                await _secretsManager.UpdateSecretAsync(secretName, secret); // <-- Use update, not create
            }

            _logger.LogInformation("Successfully revoked API key {ApiKey} for merchant {MerchantId}", request.ApiKey, request.MerchantId);
            return true;
        }

        /// <inheritdoc />
        public async Task<GenerateApiKeyResponse> RotateApiKeyAsync(string merchantId, OnboardingMetadata onboardingMetadata, string apiKey)
        {
            _logger.LogInformation("Rotating API key for merchant {MerchantId}", merchantId);

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

            var newSecretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{newApiKey}";
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
                AllowedEndpoints = newApiKeyEntity.AllowedEndpoints,
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
            _logger.LogInformation("Getting all API keys for merchant {MerchantId}", merchantId);

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
                    var secretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{apiKey.Key}";
                    var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

                    // Get total usage count
                    var usageCount = await _context.ApiKeyUsages
                        .Where(u => u.ApiKeyId == apiKey.Id)
                        .SumAsync(u => u.RequestCount);

                    result.Add(new ApiKeyInfo
                    {
                        ApiKey = apiKey.Key,
                        MerchantId = apiKey.MerchantId,
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
                    _logger.LogWarning(ex, "Failed to get secret info for API key {ApiKey}, using database info only", apiKey.Key);
                    // If we can't get the secret, just use the database info
                    var usageCount = await _context.ApiKeyUsages
                        .Where(u => u.ApiKeyId == apiKey.Id)
                        .SumAsync(u => u.RequestCount);

                    result.Add(new ApiKeyInfo
                    {
                        ApiKey = apiKey.Key,
                        MerchantId = apiKey.MerchantId,
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

            _logger.LogInformation("Found {Count} API keys for merchant {MerchantId}", result.Count, merchantId);
            return result;
        }

        /// <inheritdoc />
        public async Task<ApiKeyInfo> GetApiKeyInfoAsync(string apiKey)
        {
            _logger.LogInformation("Getting API key info for key {ApiKey}", apiKey);

            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
            if (apiKeyEntity == null)
            {
                throw new KeyNotFoundException($"API key {apiKey} not found");
            }

            var merchant = await _merchantRepository.GetByIdAsync(apiKeyEntity.MerchantId);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant not found for API key {apiKey}");
            }

            var secretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{apiKey}";
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

            // Get total usage count
            var usageCount = await _context.ApiKeyUsages
                .Where(u => u.ApiKeyId == apiKeyEntity.Id)
                .SumAsync(u => u.RequestCount);

            return new ApiKeyInfo
            {
                ApiKey = apiKeyEntity.Key,
                MerchantId = apiKeyEntity.MerchantId,
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
                UsageCount = usageCount
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
            var data = $"{timestamp}|{nonce}|{merchantId}|{apiKey}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
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
            _logger.LogInformation("Generating new API key for merchant {MerchantId}", request.MerchantId);

            // Get merchant using internal merchant ID
            var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId);
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

            // Determine a unique, human-readable name for the API key. The database enforces
            // a UNIQUE constraint on (merchant_id, name) so we must avoid collisions with any
            // existing key (active OR historical).

            var baseName = merchant.Name?.Trim();
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = "APIKEY"; // sensible fallback – should never occur in practice
            }

            // Gather **all** existing names (case-insensitive) for this merchant
            var existingNames = (await _apiKeyRepository.GetByMerchantIdAsync(merchant.MerchantId))
                .Select(k => k.Name?.ToLowerInvariant())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet();

            var uniqueName = baseName;
            var suffix = 1;
            while (existingNames.Contains(uniqueName.ToLowerInvariant()))
            {
                uniqueName = $"{baseName}-{suffix}";
                suffix++;
            }

            // Generate new API key
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
                Name = uniqueName,
                Description = request.Description ?? "API Key for merchant " + merchant.Name,
                MerchantId = request.MerchantId,
                CreatedBy = request.OnboardingMetadata.AdminUserId,
                OnboardingReference = request.OnboardingMetadata.OnboardingReference,
                ExpirationDays = request.ExpirationDays ?? 365 // Use request value or default to 1 year
            };

            await _apiKeyRepository.CreateAsync(apiKey);

            // Store secret in AWS Secrets Manager
            var newSecretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{apiKey.Key}";
            var secretValue = new ApiKeySecret
            {
                ApiKey = apiKey.Key,
                Secret = GenerateSecret(),
                MerchantId = merchant.MerchantId,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            };
            await _secretsManager.StoreSecretAsync(newSecretName, JsonSerializer.Serialize(secretValue));

            _logger.LogInformation("Generated new API key {ApiKeyId} for merchant {MerchantId}", apiKey.Id, merchant.MerchantId);

            return new GenerateApiKeyResponse
            {
                MerchantId = request.MerchantId,  // Return the same MerchantId from request
                ExternalMerchantId = merchant.ExternalMerchantId,  // Include the external ID
                MerchantName = merchant.Name??string.Empty,
                ApiKey = apiKey.Key,
                Secret = secretValue.Secret,
                ExpiresAt = apiKey.ExpiresAt ?? DateTime.UtcNow.AddDays(request.ExpirationDays ?? 365),
                RateLimit = apiKey.RateLimit,
                AllowedEndpoints = apiKey.AllowedEndpoints,
                Purpose = apiKey.Purpose,
                Description = apiKey.Description,
                OnboardingMetadata = request.OnboardingMetadata
            };
        }

        /// <summary>
        /// Regenerates the secret for a merchant's API keys
        /// </summary>
        public async Task<ApiKeyResponse> RegenerateSecretAsync(string merchantId)
        {
            _logger.LogInformation("Regenerating secret for merchant {MerchantId}", merchantId);

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
            var secretName = $"feenominal/merchants/{merchant.MerchantId:D}/apikeys/{activeKeys.First().Key}";
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

            _logger.LogInformation("Successfully regenerated secret and created new API key for merchant {MerchantId}", merchantId);

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
                _logger.LogInformation("Generating initial API key for merchant {MerchantId}", merchantId);

                var merchant = await _merchantRepository.GetByIdAsync(merchantId);
                if (merchant == null)
                {
                    throw new KeyNotFoundException($"Merchant not found with ID {merchantId}");
                }

                // Generate API key and secret
                var apiKey = Guid.NewGuid().ToString("N");
                var secret = Guid.NewGuid().ToString("N");

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
                    Name = request.Description ?? "Initial API Key",
                    CreatedBy = request.OnboardingMetadata.AdminUserId,
                    OnboardingReference = request.OnboardingMetadata.OnboardingReference,
                    ExpirationDays = request.ExpirationDays ?? 365 // Use request value or default to 1 year
                };

                var createdApiKey = await _apiKeyRepository.CreateAsync(apiKeyEntity);

                // Store secret in AWS Secrets Manager
                var secretName = $"feenominal/merchants/{merchantId:D}/apikeys/{apiKey}";
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
                    MerchantName = merchant.Name,
                    StatusId = 1, // Active
                    StatusCode = "ACTIVE",
                    StatusName = "Active",
                    ApiKey = apiKey,
                    ApiKeyId = createdApiKey.Id,
                    ExpiresAt = apiKeyEntity.ExpiresAt ?? DateTime.UtcNow.AddDays(request.ExpirationDays ?? 365),
                    Secret = secret,
                    CreatedAt = apiKeyEntity.CreatedAt,
                    RateLimit = apiKeyEntity.RateLimit,
                    AllowedEndpoints = apiKeyEntity.AllowedEndpoints,
                    Description = apiKeyEntity.Description,
                    Purpose = apiKeyEntity.Purpose,
                    OnboardingMetadata = request.OnboardingMetadata
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating initial API key for merchant {MerchantId}", merchantId);
                throw;
            }
        }
    }
} 