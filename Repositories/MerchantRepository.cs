using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Data;
using Microsoft.Extensions.Logging;
using FeeNominalService.Services;
using System.Linq;

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
                _logger.LogInformation("Created merchant with ID {MerchantId}", merchant.MerchantId);
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating merchant with external ID {ExternalMerchantId}", merchant.ExternalMerchantId);
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
                _logger.LogError(ex, "Error retrieving merchant with external ID {ExternalMerchantId}", externalMerchantId);
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
                _logger.LogError(ex, "Error retrieving merchant with external GUID {ExternalMerchantGuid}", externalMerchantGuid);
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
                _logger.LogInformation("Updated merchant {MerchantId}", merchant.MerchantId);
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant {MerchantId}", merchant.MerchantId);
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
                _logger.LogError(ex, "Error checking existence of merchant with external ID {ExternalMerchantId}", externalMerchantId);
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
                _logger.LogError(ex, "Error checking existence of merchant with external GUID {ExternalMerchantGuid}", externalMerchantGuid);
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

                // Deactivate all providers and their configurations before deleting the merchant
                await DeactivateAllProvidersForMerchantAsync(merchantId);

                _context.Merchants.Remove(merchant);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Successfully deleted merchant {MerchantId} and deactivated all associated providers", merchantId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting merchant {MerchantId}", merchantId);
                throw;
            }
        }

        private async Task DeactivateAllProvidersForMerchantAsync(Guid merchantId)
        {
            try
            {
                _logger.LogInformation("Deactivating all providers and configurations for merchant {MerchantId} due to merchant deletion", merchantId);

                // Get all providers for this merchant
                var providers = await _context.SurchargeProviders
                    .Include(p => p.Configurations)
                    .Where(p => p.CreatedBy == merchantId.ToString() && p.Status.Code != "DELETED")
                    .ToListAsync();

                var providerCount = 0;

                foreach (var provider in providers)
                {
                    // Get the DELETED status
                    var deletedStatus = await _context.SurchargeProviderStatuses
                        .FirstOrDefaultAsync(s => s.Code == "DELETED");

                    if (deletedStatus == null)
                    {
                        _logger.LogWarning("DELETED status not found, skipping provider {ProviderId}", provider.Id);
                        continue;
                    }

                    // Update the provider to DELETED status
                    provider.StatusId = deletedStatus.StatusId;
                    provider.UpdatedAt = DateTime.UtcNow;
                    provider.UpdatedBy = "SYSTEM";

                    // Deactivate all provider configurations and unset primary
                    if (provider.Configurations != null)
                    {
                        foreach (var config in provider.Configurations)
                        {
                            config.IsActive = false;
                            config.IsPrimary = false;
                            config.UpdatedAt = DateTime.UtcNow;
                            config.UpdatedBy = "SYSTEM";
                        }
                    }

                    providerCount++;
                    _logger.LogDebug("Soft deleted provider {ProviderId} for merchant {MerchantId}", provider.Id, merchantId);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deactivated {ProviderCount} providers and their configurations for merchant {MerchantId}", 
                    providerCount, merchantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating providers for merchant {MerchantId}", merchantId);
                // Don't throw - we don't want merchant deletion to fail if provider deactivation fails
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
                return await _context.MerchantStatuses.AnyAsync(s => s. MerchantStatusId == statusId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking validity of merchant status ID {StatusId}", statusId);
                throw;
            }
        }
    }
} 