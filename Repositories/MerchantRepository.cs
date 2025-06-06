using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Data;
using Microsoft.Extensions.Logging;
using FeeNominalService.Services;

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
            var merchant = await _context.Merchants.FindAsync(merchantId);
            if (merchant == null)
            {
                return false;
            }

            _context.Merchants.Remove(merchant);
            await _context.SaveChangesAsync();
            return true;
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
                return await _context.MerchantStatuses.AnyAsync(s => s.Id == statusId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking validity of merchant status ID {StatusId}", statusId);
                throw;
            }
        }
    }
} 