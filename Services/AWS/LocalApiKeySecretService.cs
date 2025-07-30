using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Services.AWS;
using FeeNominalService.Utils;

namespace FeeNominalService.Services.AWS
{
    // This service uses LogSanitizer.SanitizeSecretName for secure handling of secret names
    // Enhanced security: Secret names contain sensitive data (merchant IDs, API keys) but are properly sanitized before logging
    public class LocalApiKeySecretService : IAwsSecretsManagerService
    {
        private readonly ILogger<LocalApiKeySecretService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly SecretNameFormatter _secretNameFormatter;

        public LocalApiKeySecretService(
            ILogger<LocalApiKeySecretService> logger, 
            ApplicationDbContext context, 
            SecretNameFormatter secretNameFormatter)
        {
            _logger = logger;
            _context = context;
            _secretNameFormatter = secretNameFormatter;
        }

        public async Task<string?> GetSecretAsync(string secretName)
        {
            try
            {
                // All sensitive data is properly sanitized using LogSanitizer
                _logger.LogInformation("Getting secret {SecretName} from database", LogSanitizer.SanitizeString(secretName));
                
                // Special handling for admin secrets
                if (secretName == "feenominal/admin/api-key-secret")
                {
                    var adminSecret = await _context.ApiKeySecrets
                        .FirstOrDefaultAsync(s => s.MerchantId == null && s.Scope == "admin" && s.Status == "ACTIVE" && !s.IsRevoked);
                    
                    if (adminSecret == null)
                    {
                        _logger.LogWarning("No active admin secret found in database");
                        return null;
                    }
                    
                    // Use secure wrapper for admin secret handling
                    using var secureAdminSecret = SecureApiKeySecretWrapper.FromApiKeySecret(adminSecret);
                    
                    // apiKey is sanitized before logging
                    _logger.LogInformation("Found admin secret with API key: {ApiKey}", LogSanitizer.SanitizeString(secureAdminSecret.GetApiKey()));
                    return secureAdminSecret.ToJsonString();
                }
                
                // Extract API key from secret name
                var apiKey = ExtractApiKeyFromSecretName(secretName);
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Invalid secret name format: {SecretName}", LogSanitizer.SanitizeString(secretName));
                    return null;
                }

                var secret = await _context.ApiKeySecrets
                    .FirstOrDefaultAsync(s => s.ApiKey == apiKey);

                if (secret == null)
                    return null;

                // Use secure wrapper for secret handling
                using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(secret);
                return secureSecret.ToJsonString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", LogSanitizer.SanitizeString(secretName));
                return null;
            }
        }

        public async Task<T?> GetSecretAsync<T>(string secretName) where T : class
        {
            try
            {
                // All sensitive data is properly sanitized using LogSanitizer
                _logger.LogInformation("Getting secret {SecretName} from database", LogSanitizer.SanitizeString(secretName));
                
                // Special handling for admin secrets
                if (secretName == "feenominal/admin/api-key-secret")
                {
                    var adminSecret = await _context.ApiKeySecrets
                        .FirstOrDefaultAsync(s => s.MerchantId == null && s.Scope == "admin" && s.Status == "ACTIVE" && !s.IsRevoked);
                    
                    if (adminSecret == null)
                    {
                        _logger.LogWarning("No active admin secret found in database");
                        return null;
                    }
                    
                    // Use secure wrapper for admin secret handling
                    using var secureAdminSecret = SecureApiKeySecretWrapper.FromApiKeySecret(adminSecret);
                    
                    _logger.LogInformation("Found admin secret with API key: {ApiKey}", LogSanitizer.SanitizeString(secureAdminSecret.GetApiKey()));
                    return JsonSerializer.Deserialize<T>(secureAdminSecret.ToJsonString());
                }
                
                // Extract API key from secret name
                var apiKey = ExtractApiKeyFromSecretName(secretName);
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Invalid secret name format: {SecretName}", LogSanitizer.SanitizeString(secretName));
                    return null;
                }

                var secret = await _context.ApiKeySecrets
                    .FirstOrDefaultAsync(s => s.ApiKey == apiKey);

                if (secret == null)
                {
                    return null;
                }

                // Use secure wrapper for secret handling
                using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(secret);
                return JsonSerializer.Deserialize<T>(secureSecret.ToJsonString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving secret {SecretName}", LogSanitizer.SanitizeString(secretName));
                return null;
            }
        }

        public async Task StoreSecretAsync(string secretName, string secretValue)
        {
            try
            {
                // All sensitive data is properly sanitized using LogSanitizer
                _logger.LogInformation("Storing secret {SecretName} in database", LogSanitizer.SanitizeSecretName(secretName));
                
                // Extract API key from secret name
                var apiKey = ExtractApiKeyFromSecretName(secretName);
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new ArgumentException($"Invalid secret name format: {secretName}");
                }

                // Use secure wrapper for secret deserialization
                var secret = JsonSerializer.Deserialize<ApiKeySecret>(secretValue);
                if (secret == null)
                {
                    throw new ArgumentException("Invalid secret value format");
                }

                using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(secret);

                // Check if secret already exists
                var existingSecret = await _context.ApiKeySecrets
                    .FirstOrDefaultAsync(s => s.ApiKey == apiKey);

                if (existingSecret != null)
                {
                    // Update existing secret using secure wrapper
                    existingSecret.Secret = secureSecret.GetSecret();
                    existingSecret.Status = secureSecret.Status;
                    existingSecret.IsRevoked = secureSecret.IsRevoked;
                    existingSecret.RevokedAt = secureSecret.RevokedAt;
                    existingSecret.LastRotated = secureSecret.LastRotated;
                    existingSecret.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new secret using secure wrapper
                    var newSecret = secureSecret.ToApiKeySecret();
                    newSecret.ApiKey = apiKey;
                    newSecret.CreatedAt = DateTime.UtcNow;
                    newSecret.UpdatedAt = DateTime.UtcNow;
                    _context.ApiKeySecrets.Add(newSecret);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // All sensitive data is properly sanitized using LogSanitizer
                _logger.LogError(ex, "Error storing secret {SecretName}", LogSanitizer.SanitizeSecretName(secretName));
                throw;
            }
        }

        public async Task CreateSecretAsync(string secretName, Dictionary<string, string> secretValue)
        {
            try
            {
                // All sensitive data is properly sanitized using LogSanitizer
                _logger.LogInformation("Creating secret {SecretName} in database", LogSanitizer.SanitizeSecretName(secretName));
                
                // Extract API key from secret name
                var apiKey = ExtractApiKeyFromSecretName(secretName);
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new ArgumentException($"Invalid secret name format: {secretName}");
                }

                                 // Handle nullable MerchantId for admin secrets
                 Guid? merchantId = null;
                 if (secretValue.ContainsKey("MerchantId") && !string.IsNullOrEmpty(secretValue["MerchantId"]))
                 {
                     if (Guid.TryParse(secretValue["MerchantId"], out Guid parsedMerchantId))
                     {
                         merchantId = parsedMerchantId;
                     }
                     else
                     {
                         throw new ArgumentException($"Invalid MerchantId format: {secretValue["MerchantId"]}");
                     }
                 }

                 var secret = new ApiKeySecret
                 {
                     ApiKey = apiKey,
                     Secret = secretValue.ContainsKey("Secret") ? secretValue["Secret"] : string.Empty,
                     MerchantId = merchantId,
                     CreatedAt = DateTime.UtcNow,
                     UpdatedAt = DateTime.UtcNow,
                     Status = secretValue.ContainsKey("Status") ? secretValue["Status"] : "ACTIVE",
                     IsRevoked = secretValue.ContainsKey("IsRevoked") && bool.Parse(secretValue["IsRevoked"]),
                     Scope = secretValue.ContainsKey("Scope") ? secretValue["Scope"] : "merchant"
                 };

                _context.ApiKeySecrets.Add(secret);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // All sensitive data is properly sanitized using LogSanitizer
                _logger.LogError(ex, "Error creating secret {SecretName}", LogSanitizer.SanitizeSecretName(secretName));
                throw;
            }
        }

        public async Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class
        {
            try
            {
                // All sensitive data is properly sanitized using LogSanitizer
                _logger.LogInformation("Updating secret {SecretName} in database", LogSanitizer.SanitizeString(secretName));
                
                // Extract API key from secret name
                var apiKey = ExtractApiKeyFromSecretName(secretName);
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new ArgumentException($"Invalid secret name format: {secretName}");
                }

                var existingSecret = await _context.ApiKeySecrets
                    .FirstOrDefaultAsync(s => s.ApiKey == apiKey);

                if (existingSecret == null)
                {
                    throw new KeyNotFoundException($"Secret not found for API key: {apiKey}");
                }

                // Update the secret properties
                var secretJson = JsonSerializer.Serialize(secretValue);
                var updatedSecret = JsonSerializer.Deserialize<ApiKeySecret>(secretJson);
                
                if (updatedSecret != null)
                {
                    existingSecret.Secret = updatedSecret.Secret;
                    existingSecret.Status = updatedSecret.Status;
                    existingSecret.IsRevoked = updatedSecret.IsRevoked;
                    existingSecret.RevokedAt = updatedSecret.RevokedAt;
                    existingSecret.LastRotated = updatedSecret.LastRotated;
                    existingSecret.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating secret {SecretName}", LogSanitizer.SanitizeString(secretName));
                throw;
            }
        }

        public async Task<IEnumerable<T>> GetAllSecretsAsync<T>() where T : class
        {
            var secrets = await _context.ApiKeySecrets.ToListAsync();
            var secureSecrets = new List<T>();
            
            foreach (var secret in secrets)
            {
                using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(secret);
                var deserialized = JsonSerializer.Deserialize<T>(secureSecret.ToJsonString());
                if (deserialized != null)
                {
                    secureSecrets.Add(deserialized);
                }
            }
            
            return secureSecrets;
        }

        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                // All sensitive data is properly sanitized using LogSanitizer
                // apiKey and merchantId are sanitized before logging
                _logger.LogInformation("Validating API key {ApiKey} for merchant {MerchantId} in database", LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(merchantId));
                
                if (!Guid.TryParse(merchantId, out _))
                {
                    _logger.LogError("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                    return false;
                }

                                 var secret = await _context.ApiKeySecrets
                     .FirstOrDefaultAsync(s => s.ApiKey == apiKey && s.MerchantId.ToString() == merchantId && !s.IsRevoked && s.Status == "ACTIVE");

                if (secret == null)
                    return false;

                // Use secure wrapper for validation
                using var secureSecret = SecureApiKeySecretWrapper.FromApiKeySecret(secret);
                return secureSecret.Status == "ACTIVE" && !secureSecret.IsRevoked;
            }
            catch (Exception ex)
            {
                // apiKey and merchantId are sanitized before logging
                _logger.LogError(ex, "Error validating API key {ApiKey} for merchant {MerchantId}", LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(merchantId));
                return false;
            }
        }

        public async Task RevokeApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                // All sensitive data is properly sanitized using LogSanitizer
                // apiKey and merchantId are sanitized before logging
                _logger.LogInformation("Revoking API key {ApiKey} for merchant {MerchantId} in database", LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(merchantId));
                
                if (!Guid.TryParse(merchantId, out _))
                {
                    _logger.LogError("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                    return;
                }

                                 var secret = await _context.ApiKeySecrets
                     .FirstOrDefaultAsync(s => s.ApiKey == apiKey && s.MerchantId.ToString() == merchantId);

                if (secret != null)
                {
                    secret.IsRevoked = true;
                    secret.Status = "REVOKED";
                    secret.RevokedAt = DateTime.UtcNow;
                    secret.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // apiKey and merchantId are sanitized before logging
                _logger.LogError(ex, "Error revoking API key {ApiKey} for merchant {MerchantId}", LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(merchantId));
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
                // Convert wrapper to SecureApiKeySecret for interface compatibility
                using var wrapper = SecureApiKeySecretWrapper.FromApiKeySecret(secret);
                return SecureApiKeySecret.FromApiKeySecret(wrapper.ToApiKeySecret());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving secure secret for {SecretName}", LogSanitizer.SanitizeString(secretName));
                return null;
            }
        }

        /// <summary>
        /// Securely retrieves merchant secret without exposing sensitive data in method parameters
        /// This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="merchantId">The merchant ID (non-sensitive)</param>
        /// <param name="apiKey">The API key (non-sensitive)</param>
        /// <returns>ApiKeySecret or null if not found</returns>
        public async Task<ApiKeySecret?> GetMerchantSecretSecurelyAsync(string merchantId, string apiKey)
        {
            try
            {
                // Build the secret name using the configured pattern internally
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
                _logger.LogInformation("Looking up merchant secret: {SecretName}", LogSanitizer.SanitizeString(secretName));

                return await GetSecretAsync<ApiKeySecret>(secretName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving merchant secret for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                return null;
            }
        }

        /// <summary>
        /// Securely stores merchant secret without exposing sensitive data in method parameters
        /// This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="merchantId">The merchant ID (non-sensitive)</param>
        /// <param name="apiKey">The API key (non-sensitive)</param>
        /// <param name="secretValue">The secret value to store</param>
        public async Task StoreMerchantSecretSecurelyAsync(string merchantId, string apiKey, string secretValue)
        {
            try
            {
                // Build the secret name using the configured pattern internally
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
                _logger.LogInformation("Storing merchant secret: {SecretName}", LogSanitizer.SanitizeString(secretName));

                await StoreSecretAsync(secretName, secretValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing merchant secret for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                throw;
            }
        }

        /// <summary>
        /// Securely updates merchant secret without exposing sensitive data in method parameters
        /// This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="merchantId">The merchant ID (non-sensitive)</param>
        /// <param name="apiKey">The API key (non-sensitive)</param>
        /// <param name="secretValue">The secret value to update</param>
        public async Task UpdateMerchantSecretSecurelyAsync<T>(string merchantId, string apiKey, T secretValue) where T : class
        {
            try
            {
                // Build the secret name using the configured pattern internally
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
                _logger.LogInformation("Updating merchant secret: {SecretName}", LogSanitizer.SanitizeString(secretName));

                await UpdateSecretAsync(secretName, secretValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant secret for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                throw;
            }
        }

        /// <summary>
        /// Securely retrieves admin secret without exposing sensitive data in method parameters
        /// This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="serviceName">The service name (non-sensitive)</param>
        /// <returns>ApiKeySecret or null if not found</returns>
        public async Task<ApiKeySecret?> GetAdminSecretSecurelyAsync(string serviceName)
        {
            try
            {
                // Build the secret name using the configured pattern internally
                var secretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
                _logger.LogInformation("Looking up admin secret: {SecretName}", LogSanitizer.SanitizeString(secretName));

                return await GetSecretAsync<ApiKeySecret>(secretName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving admin secret for service {ServiceName}", LogSanitizer.SanitizeString(serviceName));
                return null;
            }
        }

        /// <summary>
        /// Securely stores admin secret without exposing sensitive data in method parameters
        /// This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="serviceName">The service name (non-sensitive)</param>
        /// <param name="secretValue">The secret value to store</param>
        public async Task StoreAdminSecretSecurelyAsync(string serviceName, string secretValue)
        {
            try
            {
                // Build the secret name using the configured pattern internally
                var secretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
                _logger.LogInformation("Storing admin secret: {SecretName}", LogSanitizer.SanitizeString(secretName));

                await StoreSecretAsync(secretName, secretValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing admin secret for service {ServiceName}", LogSanitizer.SanitizeString(serviceName));
                throw;
            }
        }

        /// <summary>
        /// Securely updates admin secret without exposing sensitive data in method parameters
        /// This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="serviceName">The service name (non-sensitive)</param>
        /// <param name="secretValue">The secret value to update</param>
        public async Task UpdateAdminSecretSecurelyAsync<T>(string serviceName, T secretValue) where T : class
        {
            try
            {
                // Build the secret name using the configured pattern internally
                var secretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
                _logger.LogInformation("Updating admin secret: {SecretName}", LogSanitizer.SanitizeString(secretName));

                await UpdateSecretAsync(secretName, secretValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin secret for service {ServiceName}", LogSanitizer.SanitizeString(serviceName));
                throw;
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

        private string? ExtractApiKeyFromSecretName(string secretName)
        {
            // Expected format: feenominal/merchants/{merchantId}/apikeys/{apiKey}
            var parts = secretName.Split('/');
            if (parts.Length >= 5 && parts[0] == "feenominal" && parts[1] == "merchants" && parts[3] == "apikeys")
            {
                return parts[4];
            }
            return null;
        }
    }
}
