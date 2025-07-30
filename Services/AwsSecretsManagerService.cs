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
using FeeNominalService.Utils;
using System.Collections.Generic;

namespace FeeNominalService.Services
{
    public class AwsSecretsManagerService : IAwsSecretsManagerService
    {
        private readonly IAmazonSecretsManager _secretsManager;
        private readonly ILogger<AwsSecretsManagerService> _logger;
        private readonly ApiKeyConfiguration _settings;
        private readonly SecretNameFormatter _secretNameFormatter;

        public AwsSecretsManagerService(
            IAmazonSecretsManager secretsManager,
            ILogger<AwsSecretsManagerService> logger,
            IOptions<ApiKeyConfiguration> settings,
            SecretNameFormatter secretNameFormatter)
        {
            _secretsManager = secretsManager;
            _logger = logger;
            _settings = settings.Value;
            _secretNameFormatter = secretNameFormatter;
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
                _logger.LogError(ex, "Error retrieving secret {SecretName}", LogSanitizer.SanitizeString(secretName));
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
                var deserialized = JsonSerializer.Deserialize<T>(response.SecretString) 
                    ?? throw new InvalidOperationException($"Failed to deserialize secret {secretName}");

                // Use secure wrapper for ApiKeySecret objects
                if (typeof(T) == typeof(ApiKeySecret) && deserialized is ApiKeySecret apiKeySecret)
                {
                    using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(apiKeySecret);
                    return JsonSerializer.Deserialize<T>(secureSecret.ToJsonString());
                }

                return deserialized;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", LogSanitizer.SanitizeString(secretName));
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
                _logger.LogError(ex, "Error storing secret {SecretName}", LogSanitizer.SanitizeString(secretName));
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
                _logger.LogError(ex, "Error creating secret {SecretName}", LogSanitizer.SanitizeString(secretName));
                throw;
            }
        }

        public async Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class
        {
            try
            {
                string secretString;
                
                // Use secure wrapper for ApiKeySecret objects
                if (typeof(T) == typeof(ApiKeySecret) && secretValue is ApiKeySecret apiKeySecret)
                {
                    using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(apiKeySecret);
                    secretString = secureSecret.ToJsonString();
                }
                else
                {
                    secretString = JsonSerializer.Serialize(secretValue);
                }

                var request = new UpdateSecretRequest
                {
                    SecretId = secretName,
                    SecretString = secretString
                };

                await _secretsManager.UpdateSecretAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret {SecretName}", LogSanitizer.SanitizeString(secretName));
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

        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
                var secret = await GetSecretAsync<ApiKeySecret>(secretName);
                
                if (secret == null)
                    return false;

                // Use secure wrapper for validation
                using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(secret);
                return secureSecret.ProcessApiKeySecurely(secureApiKey => 
                    SimpleSecureDataHandler.FromSecureString(secureApiKey) == apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                return false;
            }
        }

        public async Task RevokeApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
                var secret = await GetSecretAsync<ApiKeySecret>(secretName);
                
                if (secret == null)
                {
                    throw new KeyNotFoundException($"API key {apiKey} not found for merchant {merchantId}");
                }

                // Use secure wrapper for revocation
                using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(secret);
                
                // Validate API key securely
                var isValidApiKey = secureSecret.ProcessApiKeySecurely(secureApiKey => 
                    SimpleSecureDataHandler.FromSecureString(secureApiKey) == apiKey);
                
                if (!isValidApiKey)
                {
                    throw new KeyNotFoundException($"API key {apiKey} not found for merchant {merchantId}");
                }

                // Update revocation status securely
                secureSecret.IsRevoked = true;
                secureSecret.RevokedAt = DateTime.UtcNow;
                secureSecret.Status = "REVOKED";
                
                await UpdateSecretAsync(secretName, secureSecret.ToApiKeySecret());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                throw;
            }
        }

        public async Task<SecureApiKeySecret?> GetSecureApiKeySecretAsync(string secretName)
        {
            try
            {
                var secret = await GetSecretAsync<ApiKeySecret>(secretName);
                if (secret == null)
                    return null;
                return SecureApiKeySecret.FromApiKeySecret(secret);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving secure secret for {SecretName}", LogSanitizer.SanitizeString(secretName));
                return null;
            }
        }

        public async Task StoreSecureApiKeySecretAsync(string secretName, SecureApiKeySecret secureSecret)
        {
            try
            {
                var apiKeySecret = secureSecret.ToApiKeySecret();
                var jsonString = JsonSerializer.Serialize(apiKeySecret);
                await StoreSecretAsync(secretName, jsonString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing secure secret for {SecretName}", LogSanitizer.SanitizeString(secretName));
                throw;
            }
        }

        public async Task UpdateSecureApiKeySecretAsync(string secretName, SecureApiKeySecret secureSecret)
        {
            try
            {
                var apiKeySecret = secureSecret.ToApiKeySecret();
                await UpdateSecretAsync(secretName, apiKeySecret);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secure secret for {SecretName}", LogSanitizer.SanitizeString(secretName));
                throw;
            }
        }
    }
} 