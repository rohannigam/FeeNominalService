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
                    
                    _logger.LogInformation("Found admin secret with API key: {ApiKey}", LogSanitizer.SanitizeString(adminSecret.ApiKey));
                    return JsonSerializer.Serialize(adminSecret);
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

                return secret != null ? JsonSerializer.Serialize(secret) : null;
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
                    
                    _logger.LogInformation("Found admin secret with API key: {ApiKey}", LogSanitizer.SanitizeString(adminSecret.ApiKey));
                    return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(adminSecret));
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

                return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(secret));
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
                _logger.LogInformation("Storing secret {SecretName} in database", LogSanitizer.SanitizeString(secretName));
                
                // Extract API key from secret name
                var apiKey = ExtractApiKeyFromSecretName(secretName);
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new ArgumentException($"Invalid secret name format: {secretName}");
                }

                var secret = JsonSerializer.Deserialize<ApiKeySecret>(secretValue);
                if (secret == null)
                {
                    throw new ArgumentException("Invalid secret value format");
                }

                // Check if secret already exists
                var existingSecret = await _context.ApiKeySecrets
                    .FirstOrDefaultAsync(s => s.ApiKey == apiKey);

                if (existingSecret != null)
                {
                    // Update existing secret
                    existingSecret.Secret = secret.Secret;
                    existingSecret.Status = secret.Status;
                    existingSecret.IsRevoked = secret.IsRevoked;
                    existingSecret.RevokedAt = secret.RevokedAt;
                    existingSecret.LastRotated = secret.LastRotated;
                    existingSecret.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new secret
                    secret.ApiKey = apiKey;
                    secret.CreatedAt = DateTime.UtcNow;
                    secret.UpdatedAt = DateTime.UtcNow;
                    _context.ApiKeySecrets.Add(secret);
                }

                await _context.SaveChangesAsync();
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
                _logger.LogInformation("Creating secret {SecretName} in database", LogSanitizer.SanitizeString(secretName));
                
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
                _logger.LogError(ex, "Error creating secret {SecretName}", LogSanitizer.SanitizeString(secretName));
                throw;
            }
        }

        public async Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class
        {
            try
            {
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
            return secrets.Select(s => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(s))).Where(s => s != null)!;
        }

        public async Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
                _logger.LogInformation("Validating API key {ApiKey} for merchant {MerchantId} in database", LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(merchantId));
                
                if (!Guid.TryParse(merchantId, out _))
                {
                    _logger.LogError("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                    return false;
                }

                                 var secret = await _context.ApiKeySecrets
                     .FirstOrDefaultAsync(s => s.ApiKey == apiKey && s.MerchantId.ToString() == merchantId && !s.IsRevoked && s.Status == "ACTIVE");

                return secret != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key {ApiKey} for merchant {MerchantId}", LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(merchantId));
                return false;
            }
        }

        public async Task RevokeApiKeyAsync(string merchantId, string apiKey)
        {
            try
            {
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
                _logger.LogError(ex, "Error revoking API key {ApiKey} for merchant {MerchantId}", LogSanitizer.SanitizeString(apiKey), LogSanitizer.SanitizeString(merchantId));
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