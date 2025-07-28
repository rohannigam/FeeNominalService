using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Utils;

namespace FeeNominalService.Repositories
{
    public class MerchantRepository : IMerchantRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MerchantRepository> _logger;

        public MerchantRepository(ApplicationDbContext context, ILogger<MerchantRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Merchant?> GetByExternalIdAsync(string externalMerchantId)
        {
            return await _context.Merchants
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.ExternalMerchantId == externalMerchantId);
        }

        public async Task<Merchant> GetByIdAsync(Guid merchantId)
        {
            var merchant = await _context.Merchants
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.MerchantId == merchantId);

            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant with ID {merchantId} not found");
            }

            return merchant;
        }

        public async Task<Merchant> CreateAsync(Merchant merchant)
        {
            try
            {
                // Validate status exists
                var status = await _context.MerchantStatuses.FindAsync(merchant.StatusId);
                if (status == null)
                {
                    throw new InvalidOperationException($"Invalid merchant status ID: {merchant.StatusId}");
                }

                _context.Merchants.Add(merchant);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created merchant with ID {MerchantId}", LogSanitizer.SanitizeGuid(merchant.MerchantId));
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(merchant.ExternalMerchantId));
                throw;
            }
        }

        public async Task<Merchant?> GetByExternalMerchantIdAsync(string externalMerchantId)
        {
            try
            {
                return await _context.Merchants
                    .Include(m => m.Status)
                    .FirstOrDefaultAsync(m => m.ExternalMerchantId == externalMerchantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(externalMerchantId));
                throw;
            }
        }

        public async Task<Merchant?> GetByExternalMerchantGuidAsync(Guid externalMerchantGuid)
        {
            try
            {
                return await _context.Merchants
                    .Include(m => m.Status)
                    .FirstOrDefaultAsync(m => m.ExternalMerchantGuid == externalMerchantGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant with external GUID {ExternalMerchantGuid}", LogSanitizer.SanitizeGuid(externalMerchantGuid));
                throw;
            }
        }

        public async Task<Merchant> UpdateAsync(Merchant merchant)
        {
            try
            {
                // Validate status exists
                var status = await _context.MerchantStatuses.FindAsync(merchant.StatusId);
                if (status == null)
                {
                    throw new InvalidOperationException($"Invalid merchant status ID: {merchant.StatusId}");
                }

                _context.Merchants.Update(merchant);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchant.MerchantId));
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchant.MerchantId));
                throw;
            }
        }

        public async Task<bool> ExistsByExternalMerchantIdAsync(string externalMerchantId)
        {
            try
            {
                return await _context.Merchants
                    .AnyAsync(m => m.ExternalMerchantId == externalMerchantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(externalMerchantId));
                throw;
            }
        }

        public async Task<bool> ExistsByExternalMerchantGuidAsync(Guid externalMerchantGuid)
        {
            try
            {
                return await _context.Merchants
                    .AnyAsync(m => m.ExternalMerchantGuid == externalMerchantGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence of merchant with external GUID {ExternalMerchantGuid}", LogSanitizer.SanitizeGuid(externalMerchantGuid));
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid merchantId)
        {
            try
            {
                var merchant = await _context.Merchants.FindAsync(merchantId);
                if (merchant == null)
                {
                    return false;
                }

                // Deactivate all provider configurations for this merchant
                await DeactivateAllProviderConfigsForMerchantAsync(merchantId);

                // Remove the merchant from the database
                _context.Merchants.Remove(merchant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted merchant {MerchantId} and deactivated all associated provider configurations", LogSanitizer.SanitizeGuid(merchantId));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                throw;
            }
        }

        private async Task DeactivateAllProviderConfigsForMerchantAsync(Guid merchantId)
        {
            try
            {
                _logger.LogInformation("Deactivating all provider configurations for merchant {MerchantId} due to merchant deletion", LogSanitizer.SanitizeGuid(merchantId));

                // Get all active provider configurations for this merchant
                var configs = await _context.SurchargeProviderConfigs
                    .Where(c => c.MerchantId == merchantId && c.IsActive)
                    .ToListAsync();

                var configCount = 0;

                foreach (var config in configs)
                {
                    try
                    {
                        // Deactivate the configuration
                        config.IsActive = false;
                        config.IsPrimary = false;
                        config.UpdatedAt = DateTime.UtcNow;
                        config.UpdatedBy = "SYSTEM";

                        configCount++;
                        _logger.LogDebug("Deactivated provider config {ConfigId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(config.Id), LogSanitizer.SanitizeGuid(merchantId));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deactivating provider config {ConfigId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(config.Id), LogSanitizer.SanitizeGuid(merchantId));
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deactivated {ConfigCount} provider configurations for merchant {MerchantId}",
                    configCount, LogSanitizer.SanitizeGuid(merchantId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating provider configurations for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                throw;
            }
        }

        public async Task<MerchantStatus?> GetMerchantStatusAsync(int statusId)
        {
            try
            {
                return await _context.MerchantStatuses.FindAsync(statusId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant status with ID {StatusId}", statusId);
                throw;
            }
        }

        public async Task<bool> IsValidStatusIdAsync(int statusId)
        {
            try
            {
                return await _context.MerchantStatuses.AnyAsync(s => s.MerchantStatusId == statusId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking validity of merchant status ID {StatusId}", statusId);
                throw;
            }
        }
    }
} 