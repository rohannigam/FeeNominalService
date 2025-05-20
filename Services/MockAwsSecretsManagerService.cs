using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FeeNominalService.Models.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Merchant;

namespace FeeNominalService.Services
{
    public class MockAwsSecretsManagerService : IAwsSecretsManagerService
    {
        private readonly ILogger<MockAwsSecretsManagerService> _logger;
        private readonly ApiKeyConfiguration _settings;
        private readonly Dictionary<string, string> _mockSecrets;
        private readonly Dictionary<string, string> _secrets;

        public MockAwsSecretsManagerService(
            ILogger<MockAwsSecretsManagerService> logger,
            IOptions<ApiKeyConfiguration> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _mockSecrets = new Dictionary<string, string>();
            _secrets = new Dictionary<string, string>();

            // Initialize with test data
            InitializeTestData();
        }

        private void InitializeTestData()
        {
            // Development test merchants
            var dev001Key = "dev_test_key_001";
            var dev002Key = "dev_test_key_002";
            var dev003Key = "dev_test_key_003";

            // Store secrets with the same path structure as production
            _mockSecrets[$"feenominal/merchants/DEV001/apikeys/{dev001Key}"] = JsonSerializer.Serialize(new ApiKeySecret 
            { 
                ApiKey = dev001Key,
                MerchantId = "DEV001",
                Secret = "dev_test_secret_001",
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            });

            _mockSecrets[$"feenominal/merchants/DEV002/apikeys/{dev002Key}"] = JsonSerializer.Serialize(new ApiKeySecret 
            { 
                ApiKey = dev002Key,
                MerchantId = "DEV002",
                Secret = "dev_test_secret_002",
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            });

            _mockSecrets[$"feenominal/merchants/DEV003/apikeys/{dev003Key}"] = JsonSerializer.Serialize(new ApiKeySecret 
            { 
                ApiKey = dev003Key,
                MerchantId = "DEV003",
                Secret = "dev_test_secret_003",
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            });

            // Production test merchants
            var merch123Key = "test_secret_key_123";
            var merch456Key = "test_secret_key_456";

            _mockSecrets[$"feenominal/merchants/MERCH123/apikeys/{merch123Key}"] = JsonSerializer.Serialize(new ApiKeySecret 
            { 
                ApiKey = merch123Key,
                MerchantId = "MERCH123",
                Secret = "test_secret_value_123",
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            });

            _mockSecrets[$"feenominal/merchants/MERCH456/apikeys/{merch456Key}"] = JsonSerializer.Serialize(new ApiKeySecret 
            { 
                ApiKey = merch456Key,
                MerchantId = "MERCH456",
                Secret = "test_secret_value_456",
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            });
        }

        public async Task<T> GetSecretAsync<T>(string secretName) where T : class
        {
            _logger.LogInformation("Mock: Getting secret {SecretName}", secretName);

            if (_mockSecrets.TryGetValue(secretName, out var secretValue))
            {
                try
                {
                    var result = await Task.Run(() => JsonSerializer.Deserialize<T>(secretValue));
                    if (result == null)
                    {
                        _logger.LogWarning("Mock: Failed to deserialize secret {SecretName}", secretName);
                        throw new InvalidOperationException($"Failed to deserialize secret {secretName}");
                    }
                    _logger.LogInformation("Mock: Successfully retrieved secret {SecretName}", secretName);
                    return result;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Mock: Error deserializing secret {SecretName}", secretName);
                    throw new InvalidOperationException($"Invalid secret format: {ex.Message}");
                }
            }

            _logger.LogWarning("Mock: Secret {SecretName} not found", secretName);
            throw new KeyNotFoundException($"Secret {secretName} not found");
        }

        public Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class
        {
            _logger.LogInformation("Mock: Updating secret {SecretName}", secretName);
            
            if (!_mockSecrets.ContainsKey(secretName))
            {
                throw new KeyNotFoundException($"Secret {secretName} not found");
            }

            _mockSecrets[secretName] = JsonSerializer.Serialize(secretValue);
            return Task.CompletedTask;
        }

        public async Task StoreSecretAsync(string secretName, string secretValue)
        {
            _logger.LogInformation("Mock: Storing secret {SecretName}", secretName);
            try
            {
                // Validate that the secret value is a valid ApiKeySecret
                var secret = await Task.Run(() => JsonSerializer.Deserialize<ApiKeySecret>(secretValue));
                if (secret == null)
                {
                    throw new InvalidOperationException("Invalid secret format");
                }

                // Store the validated secret
                _mockSecrets[secretName] = secretValue;
                _logger.LogInformation("Mock: Successfully stored secret {SecretName} with ApiKey {ApiKey}", secretName, secret.ApiKey);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Mock: Failed to store secret {SecretName} - Invalid JSON format", secretName);
                throw new InvalidOperationException($"Invalid secret format: {ex.Message}");
            }
        }

        public Task<IEnumerable<T>> GetAllSecretsAsync<T>() where T : class
        {
            _logger.LogInformation("Mock: Getting all secrets");
            var secrets = _mockSecrets.Values
                .Select(value => JsonSerializer.Deserialize<T>(value))
                .Where(secret => secret != null)
                .Cast<T>()
                .ToList();

            return Task.FromResult<IEnumerable<T>>(secrets);
        }

        public Task<string> GetApiKeyAsync(string merchantId)
        {
            // Find the first non-revoked API key for the merchant
            var merchantSecrets = _mockSecrets
                .Where(kvp => kvp.Key.StartsWith($"feenominal/merchants/{merchantId}/apikeys/"))
                .Select(kvp => JsonSerializer.Deserialize<ApiKeySecret>(kvp.Value))
                .Where(secret => secret != null && !secret.IsRevoked)
                .FirstOrDefault();

            if (merchantSecrets == null)
            {
                throw new KeyNotFoundException($"No active API key found for merchant {merchantId}");
            }

            return Task.FromResult(merchantSecrets.ApiKey);
        }

        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                var secretName = $"feenominal/merchants/{merchantId}/apikeys/{apiKey}";
                var secret = await GetSecretAsync<ApiKeySecret>(secretName);
                var isValid = !string.IsNullOrEmpty(secret.ApiKey) && 
                             secret.ApiKey == apiKey && 
                             !secret.IsRevoked;
                
                _logger.LogInformation("Mock: API key validation result for {ApiKey}: {IsValid}", apiKey, isValid);
                return isValid;
            }
            catch (KeyNotFoundException)
            {
                _logger.LogWarning("Mock: API key {ApiKey} not found for merchant {MerchantId}", apiKey, merchantId);
                return false;
            }
        }

        public async Task RevokeApiKeyAsync(string merchantId, string apiKey)
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
    }
} 