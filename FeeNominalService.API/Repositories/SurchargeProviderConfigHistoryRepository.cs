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
    public class SurchargeProviderConfigHistoryRepository : ISurchargeProviderConfigHistoryRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurchargeProviderConfigHistoryRepository> _logger = null!;

        public SurchargeProviderConfigHistoryRepository(
            ApplicationDbContext context,
            ILogger<SurchargeProviderConfigHistoryRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SurchargeProviderConfigHistory?> GetByIdAsync(Guid id)
        {
            try
            {
                var history = await _context.SurchargeProviderConfigHistory
                    .Include(h => h.Config)
                    .ThenInclude(c => c!.Provider)
                    .FirstOrDefaultAsync(h => h.Id == id);

                if (history == null)
                {
                    _logger.LogWarning("History record {HistoryId} not found", id);
                    return null;
                }

                var config = history.Config;
                if (config == null)
                {
                    _logger.LogWarning("History record {HistoryId} has missing configuration", id);
                    return history;
                }

                var provider = config.Provider;
                if (provider == null)
                {
                    _logger.LogWarning("History record {HistoryId} has missing provider", id);
                }

                return history;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history by ID {HistoryId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfigHistory>> GetByConfigIdAsync(Guid configId)
        {
            try
            {
                var histories = await _context.SurchargeProviderConfigHistory
                    .Include(h => h.Config)
                    .ThenInclude(c => c!.Provider)
                    .Where(h => h.ConfigId == configId)
                    .OrderByDescending(h => h.ChangedAt)
                    .ToListAsync();

                foreach (var history in histories)
                {
                    if (history == null)
                    {
                        _logger.LogWarning("Found null history record in collection");
                        continue;
                    }

                    var config = history.Config;
                    if (config == null)
                    {
                        _logger.LogWarning("History record {HistoryId} has missing configuration", history.Id);
                        continue;
                    }

                    var provider = config.Provider;
                    if (provider == null)
                    {
                        _logger.LogWarning("History record {HistoryId} has missing provider", history.Id);
                    }
                }

                return histories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for config {ConfigId}", configId);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfigHistory>> GetByMerchantIdAsync(string merchantId)
        {
            try
            {
                // Convert string merchantId to Guid for database comparison
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", merchantId);
                    return Enumerable.Empty<SurchargeProviderConfigHistory>();
                }

                var histories = await _context.SurchargeProviderConfigHistory
                    .Include(h => h.Config)
                    .ThenInclude(c => c!.Provider)
                    .Where(h => h.Config != null && h.Config.MerchantId == merchantGuid)
                    .OrderByDescending(h => h.ChangedAt)
                    .ToListAsync();

                foreach (var history in histories)
                {
                    if (history == null)
                    {
                        _logger.LogWarning("Found null history record in collection");
                        continue;
                    }

                    var config = history.Config;
                    if (config == null)
                    {
                        _logger.LogWarning("History record {HistoryId} has missing configuration", history.Id);
                        continue;
                    }

                    var provider = config.Provider;
                    if (provider == null)
                    {
                        _logger.LogWarning("History record {HistoryId} has missing provider", history.Id);
                    }
                }

                return histories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfigHistory>> GetByProviderIdAsync(Guid providerId)
        {
            try
            {
                var histories = await _context.SurchargeProviderConfigHistory
                    .Include(h => h.Config)
                    .ThenInclude(c => c!.Provider)
                    .Where(h => h.Config != null && h.Config.ProviderId == providerId)
                    .OrderByDescending(h => h.ChangedAt)
                    .ToListAsync();

                foreach (var history in histories)
                {
                    if (history == null)
                    {
                        _logger.LogWarning("Found null history record in collection");
                        continue;
                    }

                    var config = history.Config;
                    if (config == null)
                    {
                        _logger.LogWarning("History record {HistoryId} has missing configuration", history.Id);
                        continue;
                    }

                    var provider = config.Provider;
                    if (provider == null)
                    {
                        _logger.LogWarning("History record {HistoryId} has missing provider", history.Id);
                    }
                }

                return histories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting history for provider {ProviderId}", providerId);
                throw;
            }
        }

        public async Task<SurchargeProviderConfigHistory> AddAsync(SurchargeProviderConfigHistory history)
        {
            try
            {
                if (history == null)
                {
                    throw new ArgumentNullException(nameof(history));
                }

                if (history.Config == null)
                {
                    throw new InvalidOperationException("History record must have an associated configuration");
                }

                history.ChangedAt = DateTime.UtcNow;

                await _context.SurchargeProviderConfigHistory.AddAsync(history);
                await _context.SaveChangesAsync();

                return history;
            }
            catch (Exception ex)
            {
                if (history == null)
                {
                    _logger.LogError(ex, "Error adding history: history object is null");
                }
                else
                {
                    _logger.LogError(ex, "Error adding history for config {ConfigId}", history.ConfigId);
                }
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var history = await _context.SurchargeProviderConfigHistory.FindAsync(id);
                if (history == null)
                {
                    return false;
                }

                _context.SurchargeProviderConfigHistory.Remove(history);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting history {HistoryId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _context.SurchargeProviderConfigHistory
                    .AnyAsync(h => h.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking history existence {HistoryId}", id);
                throw;
            }
        }
    }
} 