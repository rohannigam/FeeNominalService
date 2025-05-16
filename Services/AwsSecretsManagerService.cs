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

namespace FeeNominalService.Services
{
    public interface IAwsSecretsManagerService
    {
        Task<string> GetApiKeyAsync(string merchantId);
        Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey);
        Task<T> GetSecretAsync<T>(string secretName) where T : class;
        Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class;
        Task StoreSecretAsync(string secretName, string secretValue);
        Task<IEnumerable<T>> GetAllSecretsAsync<T>() where T : class;
        Task RevokeApiKeyAsync(string merchantId, string apiKey);
    }

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

        public async Task<string> GetApiKeyAsync(string merchantId)
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

        public async Task<T> GetSecretAsync<T>(string secretName) where T : class
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

        public async Task<IEnumerable<T>> GetAllSecretsAsync<T>() where T : class
        {
            try
            {
                var request = new ListSecretsRequest();
                var response = await _secretsManager.ListSecretsAsync(request);
                var secrets = new List<T>();

                foreach (var secret in response.SecretList)
                {
                    var secretValue = await GetSecretAsync<T>(secret.Name);
                    secrets.Add(secretValue);
                }

                return secrets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all secrets");
                throw;
            }
        }

        public async Task RevokeApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var secretName = $"feenominal/merchants/{merchantId}/apikeys/{apiKey}";
                var secret = await GetSecretAsync<ApiKeySecret>(secretName);
                
                if (secret.ApiKey != apiKey)
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