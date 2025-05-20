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

        /// <inheritdoc />
        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature)
        {
            // 1. Get API key from database
            var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
            if (apiKeyEntity == null || apiKeyEntity.Status != "ACTIVE")
            {
                return false;
            }

            // Check for expiration
            if (apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow)
            {
                apiKeyEntity.Status = "EXPIRED";
                apiKeyEntity.UpdatedAt = DateTime.UtcNow;
                await _apiKeyRepository.UpdateAsync(apiKeyEntity);
                _logger.LogWarning("API key {ApiKey} has expired.", apiKey);
                return false;
            }

            // 2. Get secret from AWS Secrets Manager
            var secretName = $"feenominal/merchants/{merchantId}/apikeys/{apiKey}";
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

            if (secret == null || secret.IsRevoked)
            {
                return false;
            }

            // 3. Validate signature
            var expectedSignature = GenerateSignature(secret.Secret, timestamp, nonce, requestBody);
            return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<ApiKeyInfo> GetApiKeyAsync(string merchantId)
        {
            _logger.LogInformation("Getting API key info for merchant {MerchantId}", merchantId);

            // Check if merchant exists
            var merchant = await _merchantRepository.GetByExternalIdAsync(merchantId);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant {merchantId} not found");
            }

            // Get the API key from database
            var apiKeyEntity = await _apiKeyRepository.GetByMerchantIdAsync(merchant.Id);
            if (!apiKeyEntity.Any())
            {
                throw new KeyNotFoundException($"No API key found for merchant {merchantId}");
            }

            var activeKey = apiKeyEntity.FirstOrDefault(k => k.Status == "ACTIVE");
            if (activeKey == null)
            {
                throw new KeyNotFoundException($"No active API key found for merchant {merchantId}");
            }

            // Get the secret from AWS Secrets Manager
            var secretName = $"feenominal/merchants/{merchantId}/apikeys/{activeKey.Key}";
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
            if (secret == null)
            {
                throw new KeyNotFoundException($"API key {activeKey.Key} not found for merchant {merchantId}");
            }

            return new ApiKeyInfo
            {
                ApiKey = activeKey.Key,
                Description = activeKey.Description ?? string.Empty,
                RateLimit = activeKey.RateLimit,
                AllowedEndpoints = activeKey.AllowedEndpoints,
                Status = activeKey.Status,
                CreatedAt = activeKey.CreatedAt,
                LastRotatedAt = activeKey.LastRotatedAt,
                RevokedAt = activeKey.RevokedAt,
                Secret = secret.Secret
            };
        }

        /// <inheritdoc />
        public async Task<ApiKeyInfo> UpdateApiKeyAsync(UpdateApiKeyRequest request)
        {
            _logger.LogInformation("Updating API key for merchant {MerchantId}", request.MerchantId);

            // 1. First check if merchant exists
            var merchant = await _merchantRepository.GetByExternalIdAsync(request.MerchantId);
            if (merchant == null)
            {
                _logger.LogWarning("Merchant {MerchantId} not found during API key update", request.MerchantId);
                throw new KeyNotFoundException($"Merchant {request.MerchantId} not found");
            }

            // 2. Then check if API key exists for this merchant
            var apiKey = await _apiKeyRepository.GetByMerchantIdAsync(merchant.Id);
            if (!apiKey.Any())
            {
                _logger.LogWarning("No API keys found for merchant {MerchantId}", request.MerchantId);
                throw new KeyNotFoundException($"No API keys found for merchant {request.MerchantId}");
            }

            // 3. Check for active API key
            var activeKey = apiKey.FirstOrDefault(k => k.Status == "ACTIVE");
            if (activeKey == null)
            {
                _logger.LogWarning("No active API key found for merchant {MerchantId}", request.MerchantId);
                throw new KeyNotFoundException($"No active API key found for merchant {request.MerchantId}");
            }

            // 4. Validate allowed endpoints
            if (request.AllowedEndpoints != null && request.AllowedEndpoints.Any())
            {
                var validEndpoints = new[] 
                { 
                    "/api/v1/surchargefee/calculate",
                    "/api/v1/surchargefee/calculate-batch",
                    "/api/v1/refunds/process",
                    "/api/v1/refunds/process-batch",
                    "/api/v1/sales/process",
                    "/api/v1/sales/process-batch",
                    "/api/v1/cancel",
                    "/api/v1/cancel/batch"
                };
                var invalidEndpoints = request.AllowedEndpoints.Except(validEndpoints).ToList();
                if (invalidEndpoints.Any())
                {
                    _logger.LogWarning("Invalid endpoints requested for merchant {MerchantId}: {Endpoints}", 
                        request.MerchantId, string.Join(", ", invalidEndpoints));
                    throw new ArgumentException($"Invalid endpoints: {string.Join(", ", invalidEndpoints)}");
                }
            }

            try
            {
                // 5. Update API key
                activeKey.Description = request.Description ?? activeKey.Description ?? string.Empty;
                activeKey.RateLimit = request.RateLimit ?? activeKey.RateLimit;
                activeKey.AllowedEndpoints = request.AllowedEndpoints ?? activeKey.AllowedEndpoints;
                await _apiKeyRepository.UpdateAsync(activeKey);
                _logger.LogInformation("Successfully updated API key for merchant {MerchantId}", request.MerchantId);

                // 6. Get the secret from AWS Secrets Manager
                var secretName = $"feenominal/merchants/{request.MerchantId}/apikeys/{activeKey.Key}";
                var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                if (secret == null)
                {
                    _logger.LogWarning("Secret not found for API key {ApiKey} of merchant {MerchantId}", 
                        activeKey.Key, request.MerchantId);
                    throw new KeyNotFoundException($"Secret not found for API key of merchant {request.MerchantId}");
                }

                return new ApiKeyInfo
                {
                    ApiKey = activeKey.Key,
                    Description = activeKey.Description,
                    RateLimit = activeKey.RateLimit,
                    AllowedEndpoints = activeKey.AllowedEndpoints,
                    Status = activeKey.Status,
                    CreatedAt = activeKey.CreatedAt,
                    LastRotatedAt = activeKey.LastRotatedAt,
                    RevokedAt = activeKey.RevokedAt,
                    Secret = secret.Secret
                };
            }
            catch (Exception ex) when (ex is not KeyNotFoundException)
            {
                _logger.LogError(ex, "Error updating API key for merchant {MerchantId}", request.MerchantId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RevokeApiKeyAsync(RevokeApiKeyRequest request)
        {
            _logger.LogInformation("Starting API key revocation process for merchant {MerchantId}, API key {ApiKey}", 
                request.MerchantId, request.ApiKey);

            // 1. Validate merchant exists
            var merchant = await _merchantRepository.GetByExternalIdAsync(request.MerchantId);
            if (merchant == null)
            {
                _logger.LogWarning("Merchant {MerchantId} not found during API key revocation", request.MerchantId);
                throw new KeyNotFoundException($"Merchant {request.MerchantId} not found");
            }

            // 2. Get the specific API key
            var apiKey = await _apiKeyRepository.GetByKeyAsync(request.ApiKey);
            if (apiKey == null || apiKey.MerchantId != merchant.Id)
            {
                _logger.LogWarning("API key {ApiKey} not found for merchant {MerchantId}", request.ApiKey, request.MerchantId);
                throw new KeyNotFoundException($"API key {request.ApiKey} not found for merchant {request.MerchantId}");
            }

            if (apiKey.Status == "REVOKED")
            {
                _logger.LogInformation("API key {ApiKey} is already revoked for merchant {MerchantId}", 
                    request.ApiKey, request.MerchantId);
                return true;
            }

            try
            {
                // 3.1 Update database record
                apiKey.Status = "REVOKED";
                apiKey.RevokedAt = DateTime.UtcNow;
                await _apiKeyRepository.UpdateAsync(apiKey);
                _logger.LogInformation("Successfully updated database record for API key {ApiKey}", request.ApiKey);

                // 3.2 Update secret in AWS Secrets Manager
                var secretName = $"feenominal/merchants/{request.MerchantId}/apikeys/{request.ApiKey}";
                try
                {
                    var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                    if (secret != null)
                    {
                        secret.IsRevoked = true;
                        secret.RevokedAt = DateTime.UtcNow;
                        secret.Status = "REVOKED";

                        var updatedSecretJson = JsonSerializer.Serialize(secret);
                        await _secretsManager.StoreSecretAsync(secretName, updatedSecretJson);
                        _logger.LogInformation("Successfully updated secret for API key {ApiKey}", request.ApiKey);
                    }
                    else
                    {
                        _logger.LogWarning("Secret not found for API key {ApiKey} in Secrets Manager", request.ApiKey);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the entire operation
                    _logger.LogError(ex, 
                        "Failed to update secret for API key {ApiKey} in Secrets Manager. Database record was updated successfully.", 
                        request.ApiKey);
                }

                _logger.LogInformation("Completed API key revocation process for merchant {MerchantId}, API key {ApiKey}", 
                    request.MerchantId, request.ApiKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error processing API key {ApiKey} for merchant {MerchantId}", 
                    request.ApiKey, request.MerchantId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<ApiKeyInfo> RotateApiKeyAsync(string merchantId)
        {
            var merchant = await _merchantRepository.GetByExternalIdAsync(merchantId);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant {merchantId} not found");
            }

            var apiKey = await _apiKeyRepository.GetByMerchantIdAsync(merchant.Id);
            if (!apiKey.Any())
            {
                throw new KeyNotFoundException($"No API key found for merchant {merchantId}");
            }

            var activeKey = apiKey.FirstOrDefault(k => k.Status == "ACTIVE");
            if (activeKey == null)
            {
                throw new KeyNotFoundException($"No active API key found for merchant {merchantId}");
            }

            // Generate new API key and secret
            var newApiKey = GenerateSecureRandomString(32);
            var newSecret = GenerateSecureRandomString(64);

            // Create new API key
            var newApiKeyEntity = new ApiKey
            {
                MerchantId = merchant.Id,
                Key = newApiKey,
                Description = activeKey.Description,
                RateLimit = activeKey.RateLimit,
                AllowedEndpoints = activeKey.AllowedEndpoints,
                Status = "ACTIVE",
                CreatedBy = activeKey.CreatedBy
            };
            await _apiKeyRepository.CreateAsync(newApiKeyEntity);

            // Store new secret in AWS Secrets Manager
            var newSecretName = $"feenominal/merchants/{merchantId}/apikeys/{newApiKey}";
            var newSecretValue = new ApiKeySecret
            {
                ApiKey = newApiKey,
                Secret = newSecret,
                MerchantId = merchantId,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            };

            var newSecretJson = JsonSerializer.Serialize(newSecretValue);
            await _secretsManager.StoreSecretAsync(newSecretName, newSecretJson);

            // Update old API key
            activeKey.Status = "ROTATED";
            activeKey.LastRotatedAt = DateTime.UtcNow;
            await _apiKeyRepository.UpdateAsync(activeKey);

            return new ApiKeyInfo
            {
                ApiKey = newApiKey,
                Description = newApiKeyEntity.Description ?? string.Empty,
                RateLimit = newApiKeyEntity.RateLimit,
                AllowedEndpoints = newApiKeyEntity.AllowedEndpoints,
                Status = "ACTIVE",
                CreatedAt = newApiKeyEntity.CreatedAt,
                LastRotatedAt = null,
                RevokedAt = null,
                Secret = newSecret
            };
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ApiKeyInfo>> GetMerchantApiKeysAsync(string merchantId)
        {
            _logger.LogInformation("Getting all API keys for merchant {MerchantId}", merchantId);

            var merchant = await _merchantRepository.GetByExternalIdAsync(merchantId);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant {merchantId} not found");
            }

            // Get API keys from database
            var apiKeys = await _apiKeyRepository.GetByMerchantIdAsync(merchant.Id);
            var result = new List<ApiKeyInfo>();

            foreach (var apiKey in apiKeys)
            {
                try
                {
                    // Try to get additional info from Secrets Manager
                    var secretName = $"feenominal/merchants/{merchantId}/apikeys/{apiKey.Key}";
                    var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

                    result.Add(new ApiKeyInfo
                    {
                        ApiKey = apiKey.Key,
                        Description = apiKey.Description ?? string.Empty,
                        RateLimit = apiKey.RateLimit,
                        AllowedEndpoints = apiKey.AllowedEndpoints,
                        Status = apiKey.Status,
                        CreatedAt = apiKey.CreatedAt,
                        LastRotatedAt = apiKey.LastRotatedAt,
                        RevokedAt = apiKey.RevokedAt,
                        ExpiresAt = apiKey.ExpiresAt,
                        Secret = string.Empty, // For list operations, we don't return actual secrets
                        IsRevoked = secret?.IsRevoked ?? false,
                        IsExpired = apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get secret info for API key {ApiKey}, using database info only", apiKey.Key);
                    // If we can't get the secret, just use the database info
                    result.Add(new ApiKeyInfo
                    {
                        ApiKey = apiKey.Key,
                        Description = apiKey.Description ?? string.Empty,
                        RateLimit = apiKey.RateLimit,
                        AllowedEndpoints = apiKey.AllowedEndpoints,
                        Status = apiKey.Status,
                        CreatedAt = apiKey.CreatedAt,
                        LastRotatedAt = apiKey.LastRotatedAt,
                        RevokedAt = apiKey.RevokedAt,
                        ExpiresAt = apiKey.ExpiresAt,
                        Secret = string.Empty,
                        IsRevoked = apiKey.Status == "REVOKED",
                        IsExpired = apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow
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

            var secretName = $"feenominal/merchants/{merchant.ExternalId}/apikeys/{apiKey}";
            var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);

            return new ApiKeyInfo
            {
                ApiKey = apiKeyEntity.Key,
                Description = apiKeyEntity.Description ?? string.Empty,
                RateLimit = apiKeyEntity.RateLimit,
                AllowedEndpoints = apiKeyEntity.AllowedEndpoints,
                Status = apiKeyEntity.Status,
                CreatedAt = apiKeyEntity.CreatedAt,
                LastRotatedAt = apiKeyEntity.LastRotatedAt,
                RevokedAt = apiKeyEntity.RevokedAt,
                ExpiresAt = apiKeyEntity.ExpiresAt,
                Secret = string.Empty, // Don't return the actual secret
                IsRevoked = secret?.IsRevoked ?? false,
                IsExpired = apiKeyEntity.ExpiresAt.HasValue && apiKeyEntity.ExpiresAt.Value < DateTime.UtcNow
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

        private string GenerateSignature(string secret, string timestamp, string nonce, string requestBody)
        {
            var data = $"{timestamp}{nonce}{requestBody}";
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

            // Get merchant
            var merchant = await _merchantRepository.GetByExternalIdAsync(request.MerchantId);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant with ID {request.MerchantId} not found");
            }

            // Check if merchant has reached maximum number of active keys
            var activeKeys = await _apiKeyRepository.GetByMerchantIdAsync(merchant.Id);
            var activeKeysCount = activeKeys.Count(k => k.Status == "ACTIVE");
            if (activeKeysCount >= 5) // Maximum 5 active keys per merchant
            {
                throw new InvalidOperationException("Merchant has reached the maximum number of active API keys (5)");
            }

            // Get existing active key to reuse its secret
            var existingKey = activeKeys.FirstOrDefault(k => k.Status == "ACTIVE");
            string secret;
            if (existingKey != null)
            {
                // Reuse existing secret
                var secretName = $"feenominal/merchants/{request.MerchantId}/apikeys/{existingKey.Key}";
                var secretValue = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                if (secretValue == null)
                {
                    throw new KeyNotFoundException("Failed to retrieve existing API key secret");
                }
                secret = secretValue.Secret;
            }
            else
            {
                // Generate new secret for first-time setup
                secret = GenerateSecret();
            }

            // Generate new API key
            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = GenerateApiKey(),
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                RateLimit = request.RateLimit ?? 1000,
                AllowedEndpoints = request.AllowedEndpoints ?? Array.Empty<string>(),
                Purpose = request.Purpose,
                Name = request.MerchantName ?? merchant.Name,
                Description = request.Description,
                MerchantId = merchant.Id
            };

            await _apiKeyRepository.CreateAsync(apiKey);

            // Store secret in AWS Secrets Manager
            var newSecretName = $"feenominal/merchants/{request.MerchantId}/apikeys/{apiKey.Key}";
            var newSecretValue = new ApiKeySecret
            {
                ApiKey = apiKey.Key,
                Secret = secret,
                MerchantId = request.MerchantId,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            };
            await _secretsManager.StoreSecretAsync(newSecretName, JsonSerializer.Serialize(newSecretValue));

            _logger.LogInformation("Generated new API key {ApiKeyId} for merchant {MerchantId}", apiKey.Id, merchant.Id);

            return new GenerateApiKeyResponse
            {
                ApiKey = apiKey.Key,
                Secret = secret,
                ExpiresAt = apiKey.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                RateLimit = apiKey.RateLimit,
                AllowedEndpoints = apiKey.AllowedEndpoints,
                Purpose = apiKey.Purpose
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
            var activeKeys = (await _apiKeyRepository.GetByMerchantIdAsync(merchant.Id))
                .Where(k => k.Status == "ACTIVE")
                .ToList();
            if (!activeKeys.Any())
            {
                throw new InvalidOperationException($"No active API keys found for merchant {merchantId}");
            }

            // Generate new secret
            var newSecret = GenerateSecureRandomString(64);

            // Store new secret in AWS Secrets Manager
            await _secretsManager.StoreSecretAsync($"apikey-secret-{merchant.Id}", newSecret);

            // Update all active API keys to mark them as rotated
            foreach (var key in activeKeys)
            {
                key.Status = "ROTATED";
                key.LastRotatedAt = DateTime.UtcNow;
                await _apiKeyRepository.UpdateAsync(key);
            }

            // Generate a new API key with the new secret
            var newApiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = GenerateApiKey(),
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                RateLimit = activeKeys.First().RateLimit,
                AllowedEndpoints = activeKeys.First().AllowedEndpoints,
                Purpose = activeKeys.First().Purpose,
                Name = activeKeys.First().Name,
                Description = "Regenerated after secret loss",
                MerchantId = merchant.Id
            };

            await _apiKeyRepository.CreateAsync(newApiKey);

            _logger.LogInformation("Successfully regenerated secret and created new API key for merchant {MerchantId}", merchantId);

            return new ApiKeyResponse
            {
                ApiKey = newApiKey.Key,
                Secret = newSecret,
                ExpiresAt = newApiKey.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                RateLimit = newApiKey.RateLimit,
                AllowedEndpoints = newApiKey.AllowedEndpoints,
                Purpose = newApiKey.Purpose
            };
        }

        /// <summary>
        /// Generates the initial API key and secret for a merchant
        /// </summary>
        public async Task<GenerateApiKeyResponse> GenerateInitialApiKeyAsync(GenerateApiKeyRequest request)
        {
            _logger.LogInformation("Generating initial API key for merchant {MerchantId}", request.MerchantId);

            // Get merchant
            var merchant = await _merchantRepository.GetByExternalIdAsync(request.MerchantId);
            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant with ID {request.MerchantId} not found");
            }

            // Check if merchant already has any API keys
            var existingKeys = await _apiKeyRepository.GetByMerchantIdAsync(merchant.Id);
            if (existingKeys.Any())
            {
                throw new InvalidOperationException($"Merchant {request.MerchantId} already has API keys. Use GenerateApiKeyAsync for additional keys.");
            }

            // Generate new API key and secret
            var (apiKey, secret) = _apiKeyGenerator.GenerateApiKeyAndSecret();

            // Create new API key
            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddYears(1),
                RateLimit = request.RateLimit ?? 1000,
                AllowedEndpoints = request.AllowedEndpoints ?? Array.Empty<string>(),
                Purpose = request.Purpose,
                Name = request.MerchantName ?? merchant.Name,
                Description = request.Description,
                MerchantId = merchant.Id
            };

            await _apiKeyRepository.CreateAsync(apiKeyEntity);

            // Store secret in AWS Secrets Manager
            var secretName = $"feenominal/merchants/{request.MerchantId}/apikeys/{apiKey}";
            var secretValue = new ApiKeySecret
            {
                ApiKey = apiKey,
                Secret = secret,
                MerchantId = request.MerchantId,
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            };
            await _secretsManager.StoreSecretAsync(secretName, JsonSerializer.Serialize(secretValue));

            _logger.LogInformation("Generated initial API key {ApiKeyId} for merchant {MerchantId}", apiKeyEntity.Id, merchant.Id);

            return new GenerateApiKeyResponse
            {
                ApiKey = apiKey,
                Secret = secret,
                ExpiresAt = apiKeyEntity.ExpiresAt ?? DateTime.UtcNow.AddYears(1),
                RateLimit = apiKeyEntity.RateLimit,
                AllowedEndpoints = apiKeyEntity.AllowedEndpoints,
                Purpose = apiKeyEntity.Purpose
            };
        }
    }
} 