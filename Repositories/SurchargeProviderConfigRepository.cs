using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Repositories
{
    public class SurchargeProviderConfigRepository : ISurchargeProviderConfigRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurchargeProviderConfigRepository> _logger;

        public SurchargeProviderConfigRepository(
            ApplicationDbContext context,
            ILogger<SurchargeProviderConfigRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SurchargeProviderConfig?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .Include(c => c.Provider)
                    .FirstOrDefaultAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting config by ID {ConfigId}", id);
                throw;
            }
        }

        public async Task<SurchargeProviderConfig?> GetPrimaryConfigAsync(string merchantId, Guid providerId)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .Include(c => c.Provider)
                    .FirstOrDefaultAsync(c => 
                        c.MerchantId == merchantId && 
                        c.ProviderId == providerId && 
                        c.IsPrimary && 
                        c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary config for merchant {MerchantId} and provider {ProviderId}", 
                    merchantId, providerId);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfig>> GetByMerchantIdAsync(string merchantId)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .Include(c => c.Provider)
                    .Where(c => c.MerchantId == merchantId)
                    .OrderBy(c => c.ConfigName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configs for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfig>> GetByProviderIdAsync(Guid providerId)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .Include(c => c.Provider)
                    .Where(c => c.ProviderId == providerId)
                    .OrderBy(c => c.MerchantId)
                    .ThenBy(c => c.ConfigName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configs for provider {ProviderId}", providerId);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfig>> GetActiveConfigsAsync(string merchantId)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .Include(c => c.Provider)
                    .Where(c => c.MerchantId == merchantId && c.IsActive)
                    .OrderBy(c => c.ConfigName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active configs for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task<SurchargeProviderConfig> AddAsync(SurchargeProviderConfig config)
        {
            try
            {
                config.CreatedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;

                _context.SurchargeProviderConfigs.Add(config);
                await _context.SaveChangesAsync();

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding config for merchant {MerchantId}", config.MerchantId);
                throw;
            }
        }

        public async Task<SurchargeProviderConfig> UpdateAsync(SurchargeProviderConfig config)
        {
            try
            {
                config.UpdatedAt = DateTime.UtcNow;

                _context.SurchargeProviderConfigs.Update(config);
                await _context.SaveChangesAsync();

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating config {ConfigId}", config.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var config = await _context.SurchargeProviderConfigs.FindAsync(id);
                if (config == null)
                    return false;

                _context.SurchargeProviderConfigs.Remove(config);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting config {ConfigId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .AnyAsync(c => c.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking config existence {ConfigId}", id);
                throw;
            }
        }

        public async Task<bool> HasActiveConfigAsync(string merchantId, Guid providerId)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .AnyAsync(c => 
                        c.MerchantId == merchantId && 
                        c.ProviderId == providerId && 
                        c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active config existence for merchant {MerchantId} and provider {ProviderId}", 
                    merchantId, providerId);
                throw;
            }
        }

        public async Task<bool> HasPrimaryConfigAsync(string merchantId, Guid providerId)
        {
            try
            {
                return await _context.SurchargeProviderConfigs
                    .AnyAsync(c => 
                        c.MerchantId == merchantId && 
                        c.ProviderId == providerId && 
                        c.IsPrimary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking primary config existence for merchant {MerchantId} and provider {ProviderId}", 
                    merchantId, providerId);
                throw;
            }
        }

        public async Task UpdateLastUsedAsync(Guid id, bool success, string? errorMessage = null, double? responseTime = null)
        {
            try
            {
                var config = await _context.SurchargeProviderConfigs.FindAsync(id);
                if (config == null)
                    return;

                config.LastUsedAt = DateTime.UtcNow;

                if (success)
                {
                    config.LastSuccessAt = DateTime.UtcNow;
                    config.SuccessCount++;
                    if (responseTime.HasValue)
                    {
                        config.AverageResponseTime = config.AverageResponseTime.HasValue
                            ? (config.AverageResponseTime.Value + responseTime.Value) / 2
                            : responseTime.Value;
                    }
                }
                else
                {
                    config.LastErrorAt = DateTime.UtcNow;
                    config.LastErrorMessage = errorMessage;
                    config.ErrorCount++;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last used for config {ConfigId}", id);
                throw;
            }
        }
    }
} 