using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.ApiKey;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using Microsoft.EntityFrameworkCore;

namespace FeeNominalService.Services.AWS;

public class LocalApiKeySecretService : IAwsSecretsManagerService
{
    private readonly ILogger<LocalApiKeySecretService> _logger;
    private readonly ApplicationDbContext _context;

    public LocalApiKeySecretService(ILogger<LocalApiKeySecretService> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<string?> GetSecretAsync(string secretName)
    {
        try
        {
            _logger.LogInformation("Getting secret {SecretName} from database", secretName);
            
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

            var secret = new ApiKeySecret
            {
                ApiKey = apiKey,
                Secret = secretValue["Secret"],
                MerchantId = secretValue["MerchantId"],
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

    public async Task<string?> GetApiKeyAsync(string merchantId)
    {
        try
        {
            _logger.LogInformation("Getting API key for merchant {MerchantId} from database", merchantId);
            var secret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId && s.Status == "ACTIVE");
            return secret?.ApiKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API key for merchant {MerchantId}", merchantId);
            return null;
        }
    }

    public async Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(string merchantId)
    {
        try
        {
            _logger.LogInformation("Getting all API keys for merchant {MerchantId} from database", merchantId);
            var secrets = await _context.ApiKeySecrets
                .Where(s => s.MerchantId == merchantId)
                .ToListAsync();

            return secrets.Select(s => new ApiKeyInfo
            {
                ApiKey = s.ApiKey,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                LastRotatedAt = s.LastRotated,
                RevokedAt = s.RevokedAt,
                IsRevoked = s.IsRevoked,
                IsExpired = s.ExpiresAt.HasValue && s.ExpiresAt.Value < DateTime.UtcNow
            });
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
            _logger.LogInformation("Getting API key {ApiKeyId} for merchant {MerchantId} from database", apiKeyId, merchantId);
            var secret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId && s.ApiKey == apiKeyId);
            return secret?.ApiKey;
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
            _logger.LogInformation("Validating API key {ApiKey} for merchant {MerchantId} in database", apiKey, merchantId);
            var secret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId && s.ApiKey == apiKey && s.Status == "ACTIVE");
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
            var secret = await _context.ApiKeySecrets
                .FirstOrDefaultAsync(s => s.MerchantId == merchantId && s.ApiKey == apiKey);

            if (secret != null)
            {
                secret.IsRevoked = true;
                secret.RevokedAt = DateTime.UtcNow;
                secret.Status = "REVOKED";
                _context.ApiKeySecrets.Update(secret);
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
        // Expected format: feenominal/merchants/{merchantId}/apikeys/{apiKey}
        var parts = secretName.Split('/');
        return parts.Length >= 5 ? parts[4] : null;
    }
} 