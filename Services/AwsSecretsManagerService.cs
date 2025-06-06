using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FeeNominalService.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using FeeNominalService.Models;
using FeeNominalService.Models.ApiKey;
using System;
using System.Threading.Tasks;
using FeeNominalService.Services.AWS;

namespace FeeNominalService.Services
{
    public class AwsSecretsManagerService : IAwsSecretsManagerService
    {
        private readonly IAmazonSecretsManager _secretsManager;
        private readonly ILogger<AwsSecretsManagerService> _logger;
        private readonly ApiKeyConfiguration _settings;

        public AwsSecretsManagerService(
            IAmazonSecretsManager secretsManager,
            ILogger<AwsSecretsManagerService> logger,
            IOptions<ApiKeyConfiguration> settings)
        {
            _secretsManager = secretsManager;
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<string?> GetSecretAsync(string secretName)
        {
            try
            {
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = await _secretsManager.GetSecretValueAsync(request);
                return response.SecretString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", secretName);
                throw;
            }
        }

        public async Task<T?> GetSecretAsync<T>(string secretName) where T : class
        {
            try
            {
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = await _secretsManager.GetSecretValueAsync(request);
                return JsonSerializer.Deserialize<T>(response.SecretString) 
                    ?? throw new InvalidOperationException($"Failed to deserialize secret {secretName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", secretName);
                throw;
            }
        }

        public async Task StoreSecretAsync(string secretName, string secretValue)
        {
            try
            {
                var request = new CreateSecretRequest
                {
                    Name = secretName,
                    SecretString = secretValue
                };

                await _secretsManager.CreateSecretAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secret {SecretName}", secretName);
                throw;
            }
        }

        public async Task CreateSecretAsync(string secretName, Dictionary<string, string> secretValue)
        {
            try
            {
                var request = new CreateSecretRequest
                {
                    Name = secretName,
                    SecretString = JsonSerializer.Serialize(secretValue)
                };

                await _secretsManager.CreateSecretAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating secret {SecretName}", secretName);
                throw;
            }
        }

        public async Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class
        {
            try
            {
                var secretString = JsonSerializer.Serialize(secretValue);
                var request = new UpdateSecretRequest
                {
                    SecretId = secretName,
                    SecretString = secretString
                };

                await _secretsManager.UpdateSecretAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret {SecretName}", secretName);
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllSecretsAsync<T>() where T : class
        {
            try
            {
                var request = new ListSecretsRequest();
                var response = await _secretsManager.ListSecretsAsync(request);
                var secrets = new List<T>();

                foreach (var secret in response.SecretList)
                {
                    if (secret.Name == null)
                    {
                        _logger.LogWarning("Found secret with null name, skipping");
                        continue;
                    }

                    var secretValue = await GetSecretAsync<T>(secret.Name);
                    if (secretValue != null)
                    {
                        secrets.Add(secretValue);
                    }
                }

                return secrets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all secrets");
                throw;
            }
        }

        public async Task<string?> GetApiKeyAsync(string merchantId)
        {
            try
            {
                var secretName = $"{_settings.SecretName}/{merchantId}";
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = await _secretsManager.GetSecretValueAsync(request);
                var secret = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
                
                return secret?["ApiKey"] ?? throw new InvalidOperationException("API key not found in secret");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(string merchantId)
        {
            try
            {
                var secretName = $"{_settings.SecretName}/{merchantId}";
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = await _secretsManager.GetSecretValueAsync(request);
                var secret = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
                
                if (secret == null)
                {
                    return Enumerable.Empty<ApiKeyInfo>();
                }

                return new List<ApiKeyInfo>
                {
                    new ApiKeyInfo
                    {
                        ApiKey = secret["ApiKey"],
                        Status = secret.GetValueOrDefault("Status", "ACTIVE"),
                        CreatedAt = DateTime.Parse(secret.GetValueOrDefault("CreatedAt", DateTime.UtcNow.ToString())),
                        LastRotatedAt = secret.ContainsKey("LastRotatedAt") ? DateTime.Parse(secret["LastRotatedAt"]) : null,
                        RevokedAt = secret.ContainsKey("RevokedAt") ? DateTime.Parse(secret["RevokedAt"]) : null,
                        IsRevoked = secret.GetValueOrDefault("IsRevoked", "false").ToLower() == "true",
                        IsExpired = secret.ContainsKey("ExpiresAt") && DateTime.Parse(secret["ExpiresAt"]) < DateTime.UtcNow
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys for merchant {MerchantId}", merchantId);
                return Enumerable.Empty<ApiKeyInfo>();
            }
        }

        public async Task<string?> GetApiKeyByIdAsync(string merchantId, string apiKeyId)
        {
            try
            {
                var secretName = $"{_settings.SecretName}/{merchantId}/{apiKeyId}";
                var request = new GetSecretValueRequest
                {
                    SecretId = secretName
                };

                var response = await _secretsManager.GetSecretValueAsync(request);
                var secret = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
                
                return secret?["ApiKey"];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key {ApiKeyId} for merchant {MerchantId}", apiKeyId, merchantId);
                return null;
            }
        }

        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var storedApiKey = await GetApiKeyAsync(merchantId);
                return storedApiKey == apiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key for merchant {MerchantId}", merchantId);
                return false;
            }
        }

        public async Task RevokeApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var secretName = $"feenominal/merchants/{merchantId}/apikeys/{apiKey}";
                var secret = await GetSecretAsync<ApiKeySecret>(secretName);
                
                if (secret == null || secret.ApiKey != apiKey)
                {
                    throw new KeyNotFoundException($"API key {apiKey} not found for merchant {merchantId}");
                }

                secret.IsRevoked = true;
                secret.RevokedAt = DateTime.UtcNow;
                secret.Status = "REVOKED";
                
                await UpdateSecretAsync(secretName, secret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key for merchant {MerchantId}", merchantId);
                throw;
            }
        }
    }
} 