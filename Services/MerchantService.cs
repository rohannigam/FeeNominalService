using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using FeeNominalService.Models.Merchant;

namespace FeeNominalService.Services
{
    public class MerchantService : IMerchantService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MerchantService> _logger;

        public MerchantService(ApplicationDbContext context, ILogger<MerchantService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Merchant> CreateMerchantAsync(Merchant merchant)
        {
            try
            {
                _logger.LogInformation("Creating merchant with external ID {ExternalId}", merchant.ExternalId);

                // Set creation timestamp
                merchant.CreatedAt = DateTime.UtcNow;
                merchant.UpdatedAt = DateTime.UtcNow;

                _context.Merchants.Add(merchant);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created merchant with ID {MerchantId}", merchant.Id);
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating merchant with external ID {ExternalId}", merchant.ExternalId);
                throw;
            }
        }

        public async Task<Merchant?> GetMerchantAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Retrieving merchant with ID {MerchantId}", id);

                var merchant = await _context.Merchants
                    .Include(m => m.Status)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (merchant == null)
                {
                    _logger.LogWarning("Merchant not found with ID {MerchantId}", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved merchant with ID {MerchantId}", id);
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant with ID {MerchantId}", id);
                throw;
            }
        }

        public async Task<Merchant> UpdateMerchantStatusAsync(Guid id, Guid statusId)
        {
            try
            {
                _logger.LogInformation("Updating status for merchant {MerchantId} to status {StatusId}", id, statusId);

                var merchant = await _context.Merchants
                    .Include(m => m.Status)
                    .FirstOrDefaultAsync(m => m.Id == id);

                if (merchant == null)
                {
                    _logger.LogWarning("Merchant not found with ID {MerchantId}", id);
                    throw new KeyNotFoundException($"Merchant not found with ID {id}");
                }

                var status = await _context.MerchantStatuses.FindAsync(statusId);
                if (status == null)
                {
                    _logger.LogWarning("Status not found with ID {StatusId}", statusId);
                    throw new KeyNotFoundException($"Status not found with ID {statusId}");
                }

                merchant.StatusId = statusId;
                merchant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated status for merchant {MerchantId}", id);
                return merchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for merchant {MerchantId}", id);
                throw;
            }
        }

        public async Task<Merchant?> GetMerchantByExternalIdAsync(string externalId)
        {
            return await _context.Merchants
                .Include(m => m.Status)
                .Include(m => m.ApiKeys)
                .FirstOrDefaultAsync(m => m.ExternalId == externalId);
        }

        public async Task<IEnumerable<Merchant>> GetAllMerchantsAsync()
        {
            return await _context.Merchants
                .Include(m => m.Status)
                .Include(m => m.ApiKeys)
                .ToListAsync();
        }

        public async Task<bool> IsMerchantActiveAsync(Guid id)
        {
            var merchant = await _context.Merchants
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.Id == id);

            return merchant?.Status?.IsActive ?? false;
        }
    }
} 