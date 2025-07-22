using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.ApiKey;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FeeNominalService.Data;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Services.AWS;

namespace FeeNominalService.Services.AWS;

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
            _logger.LogInformation("Getting secret {SecretName} from database", secretName);
            
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
                
                _logger.LogInformation("Found admin secret with API key: {ApiKey}", adminSecret.ApiKey);
                return JsonSerializer.Serialize(adminSecret);
            }
            
            // Extract API key from secret name
            var apiKey = ExtractApiKeyFromSecretName(secretName);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Invalid secret name format: {SecretName}", secretName);
                return null;
            }

            var secret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.ApiKey == apiKey);

            return secret != null ? JsonSerializer.Serialize(secret) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret {SecretName}", secretName);
            return null;
        }
    }

    public async Task<T?> GetSecretAsync<T>(string secretName) where T : class
    {
        try
        {
            _logger.LogInformation("Getting secret {SecretName} from database", secretName);
            
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
                
                _logger.LogInformation("Found admin secret with API key: {ApiKey}", adminSecret.ApiKey);
                return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(adminSecret));
            }
            
            // Extract API key from secret name
            var apiKey = ExtractApiKeyFromSecretName(secretName);
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("Invalid secret name format: {SecretName}", secretName);
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
            _logger.LogError(ex, "Error retrieving secret {SecretName}", secretName);
            return null;
        }
    }

    public async Task StoreSecretAsync(string secretName, string secretValue)
    {
        try
        {
            _logger.LogInformation("Storing secret {SecretName} in database", secretName);
            
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
                existingSecret.LastRotated = secret.LastRotated;
                existingSecret.IsRevoked = secret.IsRevoked;
                existingSecret.RevokedAt = secret.RevokedAt;
                _context.ApiKeySecrets.Update(existingSecret);
            }
            else
            {
                // Create new secret
                _context.ApiKeySecrets.Add(secret);
            }

            await _context.SaveChangesAsync();
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
            _logger.LogInformation("Creating secret {SecretName} in database", secretName);
            
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
                Secret = secretValue["Secret"],
                MerchantId = merchantId, // Now properly handles null for admin secrets
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE"
            };

            _context.ApiKeySecrets.Add(secret);
            await _context.SaveChangesAsync();
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
            _logger.LogInformation("Updating secret {SecretName} in database", secretName);
            
            // Extract API key from secret name
            var apiKey = ExtractApiKeyFromSecretName(secretName);
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentException($"Invalid secret name format: {secretName}");
            }

            var secret = JsonSerializer.Deserialize<ApiKeySecret>(JsonSerializer.Serialize(secretValue));
            if (secret == null)
            {
                throw new ArgumentException("Invalid secret value format");
            }

            var existingSecret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.ApiKey == apiKey);

            if (existingSecret == null)
            {
                throw new KeyNotFoundException($"Secret not found: {secretName}");
            }

            existingSecret.Secret = secret.Secret;
            existingSecret.Status = secret.Status;
            existingSecret.LastRotated = secret.LastRotated;
            existingSecret.IsRevoked = secret.IsRevoked;
            existingSecret.RevokedAt = secret.RevokedAt;

            _context.ApiKeySecrets.Update(existingSecret);
            await _context.SaveChangesAsync();
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
            _logger.LogInformation("Getting all secrets from database");
            var secrets = await _context.ApiKeySecrets.ToListAsync();
            return secrets.Select(s => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(s))).Where(s => s != null)!;
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
            _logger.LogInformation("Validating API key {ApiKey} for merchant {MerchantId} in database", apiKey, merchantId);
            if (!Guid.TryParse(merchantId, out Guid merchantGuid))
            {
                _logger.LogError("Invalid merchant ID format: {MerchantId}", merchantId);
                return false;
            }
            var secret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.MerchantId == merchantGuid && s.ApiKey == apiKey && s.Status == "ACTIVE");
            return secret != null && !secret.IsRevoked;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating API key {ApiKey} for merchant {MerchantId}", apiKey, merchantId);
            return false;
        }
    }

    public async Task RevokeApiKeyAsync(string merchantId, string apiKey)
    {
        try
        {
            _logger.LogInformation("Revoking API key {ApiKey} for merchant {MerchantId} in database", apiKey, merchantId);
            if (!Guid.TryParse(merchantId, out Guid merchantGuid))
            {
                _logger.LogError("Invalid merchant ID format: {MerchantId}", merchantId);
                throw new ArgumentException("Invalid merchant ID format", nameof(merchantId));
            }
            var secret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.MerchantId == merchantGuid && s.ApiKey == apiKey);
            if (secret != null)
            {
                secret.Status = "REVOKED";
                secret.IsRevoked = true;
                secret.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking API key {ApiKey} for merchant {MerchantId}", apiKey, merchantId);
            throw;
        }
    }

    private string? ExtractApiKeyFromSecretName(string secretName)
    {
        // Handle admin secret for local development
        if (secretName == "feenominal/admin/api-key-secret")
        {
            // For admin secrets, we need to find the active admin secret in the database
            // since the secret name doesn't contain the actual API key
            return null; // We'll handle this specially in GetSecretAsync
        }
        
        // Use the formatter to extract API key from admin secrets
        if (_secretNameFormatter.IsAdminSecretName(secretName))
        {
            var serviceName = _secretNameFormatter.ExtractServiceNameFromAdminSecretName(secretName);
            return serviceName != null ? $"{serviceName}-admin-api-key-secret" : null;
        }
        
        // Use the formatter to extract API key from merchant secrets
        if (_secretNameFormatter.IsMerchantSecretName(secretName))
        {
            return _secretNameFormatter.ExtractApiKeyFromMerchantSecretName(secretName);
        }
        
        // Fallback for unknown patterns
        var parts = secretName.Split('/');
        return parts.Length >= 5 ? parts[4] : null;
    }
} 