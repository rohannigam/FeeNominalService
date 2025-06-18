using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Services.AWS;
using System.Text.Json;

namespace FeeNominalService.Services
{
    public class MockAwsSecretsManagerService : IAwsSecretsManagerService
    {
        private readonly ILogger<MockAwsSecretsManagerService> _logger;
        private readonly Dictionary<string, string> _secrets;

        public MockAwsSecretsManagerService(ILogger<MockAwsSecretsManagerService> logger)
        {
            _logger = logger;
            _secrets = new Dictionary<string, string>();
        }

        public Task<string?> GetSecretAsync(string secretName)
        {
            try
            {
                if (_secrets.TryGetValue(secretName, out var secret))
                {
                    return Task.FromResult<string?>(secret);
                }
                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", secretName);
                return Task.FromResult<string?>(null);
            }
        }

        public Task<T?> GetSecretAsync<T>(string secretName) where T : class
        {
            try
            {
                if (_secrets.TryGetValue(secretName, out var secret))
                {
                    return Task.FromResult(JsonSerializer.Deserialize<T>(secret));
                }
                return Task.FromResult<T?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", secretName);
                return Task.FromResult<T?>(null);
            }
        }

        public Task StoreSecretAsync(string secretName, string secretValue)
        {
            try
            {
                _secrets[secretName] = secretValue;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secret {SecretName}", secretName);
                throw;
            }
        }

        public Task CreateSecretAsync(string secretName, Dictionary<string, string> secretValue)
        {
            try
            {
                _secrets[secretName] = JsonSerializer.Serialize(secretValue);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret {SecretName}", secretName);
                throw;
            }
        }

        public Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class
        {
            try
            {
                _secrets[secretName] = JsonSerializer.Serialize(secretValue);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret {SecretName}", secretName);
                throw;
            }
        }

        public Task<IEnumerable<T>> GetAllSecretsAsync<T>() where T : class
        {
            try
            {
                var secrets = new List<T>();
                foreach (var secret in _secrets.Values)
                {
                    var deserialized = JsonSerializer.Deserialize<T>(secret);
                    if (deserialized != null)
                    {
                        secrets.Add(deserialized);
                    }
                }
                return Task.FromResult<IEnumerable<T>>(secrets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all secrets");
                throw;
            }
        }

        public Task<string?> GetApiKeyAsync(string merchantId)
        {
            try
            {
                var secretName = $"apikey/{merchantId}/active";
                if (_secrets.TryGetValue(secretName, out var secret))
                {
                    var apiKeySecret = JsonSerializer.Deserialize<ApiKeySecret>(secret);
                    return Task.FromResult<string?>(apiKeySecret?.Secret);
                }
                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key for merchant {MerchantId}", merchantId);
                return Task.FromResult<string?>(null);
            }
        }

        public Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(string merchantId)
        {
            try
            {
                var apiKeys = new List<ApiKeyInfo>();
                foreach (var secret in _secrets)
                {
                    if (secret.Key.StartsWith($"apikey/{merchantId}/"))
                    {
                        var apiKeySecret = JsonSerializer.Deserialize<ApiKeySecret>(secret.Value);
                        if (apiKeySecret != null)
                        {
                            apiKeys.Add(new ApiKeyInfo
                            {
                                ApiKey = apiKeySecret.ApiKey,
                                MerchantId = Guid.Parse(merchantId),
                                Status = apiKeySecret.Status,
                                CreatedAt = apiKeySecret.CreatedAt,
                                LastRotatedAt = apiKeySecret.LastRotated,
                                RevokedAt = apiKeySecret.RevokedAt,
                                IsRevoked = apiKeySecret.IsRevoked,
                                IsExpired = apiKeySecret.ExpiresAt.HasValue && apiKeySecret.ExpiresAt.Value < DateTime.UtcNow
                            });
                        }
                    }
                }
                return Task.FromResult<IEnumerable<ApiKeyInfo>>(apiKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys for merchant {MerchantId}", merchantId);
                return Task.FromResult<IEnumerable<ApiKeyInfo>>(new List<ApiKeyInfo>());
            }
        }

        public Task<string?> GetApiKeyByIdAsync(string merchantId, string apiKeyId)
        {
            try
            {
                var secretName = $"apikey/{merchantId}/{apiKeyId}";
                if (_secrets.TryGetValue(secretName, out var secret))
                {
                    var apiKeySecret = JsonSerializer.Deserialize<ApiKeySecret>(secret);
                    return Task.FromResult<string?>(apiKeySecret?.Secret);
                }
                return Task.FromResult<string?>(null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key {ApiKeyId} for merchant {MerchantId}", apiKeyId, merchantId);
                return Task.FromResult<string?>(null);
            }
        }

        public Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var secretName = $"apikey/{merchantId}/active";
                if (_secrets.TryGetValue(secretName, out var secret))
                {
                    var apiKeySecret = JsonSerializer.Deserialize<ApiKeySecret>(secret);
                    return Task.FromResult(apiKeySecret?.Secret == apiKey);
                }
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key for merchant {MerchantId}", merchantId);
                return Task.FromResult(false);
            }
        }

        public Task RevokeApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var secretName = $"apikey/{merchantId}/{apiKey}";
                if (_secrets.TryGetValue(secretName, out var secret))
                {
                    var apiKeySecret = JsonSerializer.Deserialize<ApiKeySecret>(secret);
                    if (apiKeySecret != null)
                    {
                        apiKeySecret.IsRevoked = true;
                        apiKeySecret.RevokedAt = DateTime.UtcNow;
                        apiKeySecret.Status = "REVOKED";
                        _secrets[secretName] = JsonSerializer.Serialize(apiKeySecret);
                    }
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key for merchant {MerchantId}", merchantId);
                throw;
            }
        }
    }
} 