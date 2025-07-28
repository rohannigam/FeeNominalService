using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Utils;

namespace FeeNominalService.Repositories
{
    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApiKeyRepository> _logger;

        public ApiKeyRepository(ApplicationDbContext context, ILogger<ApiKeyRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ApiKey>> GetByMerchantIdAsync(Guid merchantId)
        {
            try
            {
                return await _context.ApiKeys
                    .Where(k => k.MerchantId == merchantId && k.Scope == "merchant")
                    .OrderBy(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                throw;
            }
        }

        public async Task<IEnumerable<ApiKey>> GetByScopeAsync(string scope)
        {
            try
            {
                return await _context.ApiKeys
                    .Where(k => k.Scope == scope)
                    .OrderBy(k => k.CreatedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for scope {Scope}", LogSanitizer.SanitizeString(scope));
                throw;
            }
        }

        public async Task<ApiKey?> GetByKeyAsync(string key)
        {
            try
            {
                return await _context.ApiKeys
                    .FirstOrDefaultAsync(k => k.Key == key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key by key {Key}", LogSanitizer.SanitizeString(key));
                throw;
            }
        }

        public async Task<ApiKey?> GetAdminKeyAsync()
        {
            try
            {
                return await _context.ApiKeys
                    .Where(k => k.Scope == "admin" && k.Status == "ACTIVE")
                    .OrderByDescending(k => k.CreatedAt)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active admin API key");
                throw;
            }
        }

        public async Task<ApiKey> CreateAsync(ApiKey apiKey)
        {
            try
            {
                _context.ApiKeys.Add(apiKey);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created API key {ApiKeyId} with scope {Scope}", LogSanitizer.SanitizeGuid(apiKey.Id), LogSanitizer.SanitizeString(apiKey.Scope));
                return apiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key with scope {Scope}", LogSanitizer.SanitizeString(apiKey.Scope));
                throw;
            }
        }

        public async Task<ApiKey> UpdateAsync(ApiKey apiKey)
        {
            try
            {
                _context.ApiKeys.Update(apiKey);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated API key {ApiKeyId} with scope {Scope}", LogSanitizer.SanitizeGuid(apiKey.Id), LogSanitizer.SanitizeString(apiKey.Scope));
                return apiKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key {ApiKeyId} with scope {Scope}", LogSanitizer.SanitizeGuid(apiKey.Id), LogSanitizer.SanitizeString(apiKey.Scope));
                throw;
            }
        }
    }
} 