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
    public class SurchargeProviderRepository : ISurchargeProviderRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurchargeProviderRepository> _logger;

        public SurchargeProviderRepository(
            ApplicationDbContext context,
            ILogger<SurchargeProviderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SurchargeProvider?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.SurchargeProviders
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID {ProviderId}", id);
                throw;
            }
        }

        public async Task<SurchargeProvider?> GetByCodeAsync(string code)
        {
            try
            {
                return await _context.SurchargeProviders
                    .FirstOrDefaultAsync(p => p.Code == code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by code {ProviderCode}", code);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetAllAsync()
        {
            try
            {
                return await _context.SurchargeProviders
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers");
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetActiveAsync()
        {
            try
            {
                return await _context.SurchargeProviders
                    .Where(p => p.Status == "active")
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active providers");
                throw;
            }
        }

        public async Task<SurchargeProvider> AddAsync(SurchargeProvider provider)
        {
            try
            {
                provider.CreatedAt = DateTime.UtcNow;
                provider.UpdatedAt = DateTime.UtcNow;

                _context.SurchargeProviders.Add(provider);
                await _context.SaveChangesAsync();

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding provider {ProviderName}", provider.Name);
                throw;
            }
        }

        public async Task<SurchargeProvider> UpdateAsync(SurchargeProvider provider)
        {
            try
            {
                provider.UpdatedAt = DateTime.UtcNow;

                _context.SurchargeProviders.Update(provider);
                await _context.SaveChangesAsync();

                return provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider {ProviderId}", provider.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var provider = await _context.SurchargeProviders.FindAsync(id);
                if (provider == null)
                    return false;

                _context.SurchargeProviders.Remove(provider);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting provider {ProviderId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _context.SurchargeProviders
                    .AnyAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence {ProviderId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
            try
            {
                return await _context.SurchargeProviders
                    .AnyAsync(p => p.Code == code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence by code {ProviderCode}", code);
                throw;
            }
        }
    }
} 