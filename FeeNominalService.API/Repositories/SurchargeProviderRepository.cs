using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Models.Common;
using FeeNominalService.Utils;

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
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .Where(p => p.Status.Code != "DELETED")
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<SurchargeProvider?> GetByIdAsync(Guid id, bool includeDeleted)
        {
            try
            {
                if (includeDeleted)
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Include(p => p.Configurations)
                        .FirstOrDefaultAsync(p => p.Id == id);
                }
                else
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Include(p => p.Configurations)
                        .Where(p => p.Status.Code != "DELETED")
                        .FirstOrDefaultAsync(p => p.Id == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID {ProviderId} (includeDeleted: {IncludeDeleted})", LogSanitizer.SanitizeGuid(id), includeDeleted);
                throw;
            }
        }

        public async Task<SurchargeProvider?> GetByCodeAsync(string code)
        {
            try
            {
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .Where(p => p.Status.Code != "DELETED")
                    .FirstOrDefaultAsync(p => p.Code == code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by code {ProviderCode}", LogSanitizer.SanitizeString(code));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetAllAsync()
        {
            try
            {
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .Where(p => p.Status.Code != "DELETED")
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers");
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetAllAsync(bool includeDeleted)
        {
            try
            {
                if (includeDeleted)
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Include(p => p.Configurations)
                        .OrderBy(p => p.Name)
                        .ToListAsync();
                }
                else
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Include(p => p.Configurations)
                        .Where(p => p.Status.Code != "DELETED")
                        .OrderBy(p => p.Name)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers (includeDeleted: {IncludeDeleted})", includeDeleted);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId)
        {
            try
            {
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .Where(p => p.CreatedBy == merchantId && p.Status.Code != "DELETED")
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting providers for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId, bool includeDeleted)
        {
            try
            {
                if (includeDeleted)
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Include(p => p.Configurations)
                        .Where(p => p.CreatedBy == merchantId)
                        .OrderBy(p => p.Name)
                        .ToListAsync();
                }
                else
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Include(p => p.Configurations)
                        .Where(p => p.CreatedBy == merchantId && p.Status.Code != "DELETED")
                        .OrderBy(p => p.Name)
                        .ToListAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting providers for merchant {MerchantId} (includeDeleted: {IncludeDeleted})", LogSanitizer.SanitizeMerchantId(merchantId), includeDeleted);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetConfiguredProvidersByMerchantIdAsync(string merchantId)
        {
            try
            {
                // Convert string merchantId to Guid for database comparison
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                    return Enumerable.Empty<SurchargeProvider>();
                }

                // Get providers that the merchant has configured via surcharge_provider_configs table
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .Where(p => _context.SurchargeProviderConfigs
                        .Any(c => c.MerchantId == merchantGuid && c.ProviderId == p.Id))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configured providers for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                throw;
            }
        }

        public async Task<bool> HasConfigurationAsync(string merchantId, Guid providerId)
        {
            try
            {
                // Convert string merchantId to Guid for database comparison
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                    return false;
                }

                return await _context.SurchargeProviderConfigs
                    .AnyAsync(c => c.MerchantId == merchantGuid && c.ProviderId == providerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking configuration for merchant {MerchantId} and provider {ProviderId}", LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetActiveAsync()
        {
            try
            {
                // Get the ACTIVE status ID
                var activeStatus = await _context.SurchargeProviderStatuses
                    .FirstOrDefaultAsync(s => s.Code == "ACTIVE");
                
                if (activeStatus == null)
                {
                    throw new InvalidOperationException("ACTIVE status not found in the database");
                }

                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .Where(p => p.StatusId == activeStatus.StatusId)
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

                // Reload the entity with Status and Configurations included
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .FirstOrDefaultAsync(p => p.Id == provider.Id) ?? provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding provider {ProviderName}", LogSanitizer.SanitizeString(provider.Name));
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

                // Reload the entity with Status and Configurations included
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .FirstOrDefaultAsync(p => p.Id == provider.Id) ?? provider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider {ProviderId}", LogSanitizer.SanitizeGuid(provider.Id));
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
                _logger.LogError(ex, "Error deleting provider {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Where(p => p.Status.Code != "DELETED")
                    .AnyAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id, bool includeDeleted)
        {
            try
            {
                if (includeDeleted)
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .AnyAsync(p => p.Id == id);
                }
                else
                {
                    return await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Where(p => p.Status.Code != "DELETED")
                        .AnyAsync(p => p.Id == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence {ProviderId} (includeDeleted: {IncludeDeleted})", LogSanitizer.SanitizeGuid(id), includeDeleted);
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
                _logger.LogError(ex, "Error checking provider existence by code {ProviderCode}", LogSanitizer.SanitizeString(code));
                throw;
            }
        }

        public async Task<bool> ExistsByCodeAndMerchantAsync(string code, string merchantId)
        {
            try
            {
                return await _context.SurchargeProviders
                    .AnyAsync(p => p.Code == code && p.CreatedBy == merchantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence by code {ProviderCode} for merchant {MerchantId}", LogSanitizer.SanitizeString(code), LogSanitizer.SanitizeMerchantId(merchantId));
                throw;
            }
        }

        public async Task<SurchargeProviderStatus?> GetStatusByCodeAsync(string code)
        {
            return await _context.SurchargeProviderStatuses.FirstOrDefaultAsync(s => s.Code == code);
        }

        public async Task<bool> SoftDeleteAsync(Guid id, string deletedBy)
        {
            try
            {
                var provider = await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Include(p => p.Configurations)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (provider == null)
                {
                    _logger.LogWarning("Provider not found for soft delete: {ProviderId}", LogSanitizer.SanitizeGuid(id));
                    return false;
                }

                // Check if already deleted
                if (provider.Status.Code == "DELETED")
                {
                    _logger.LogWarning("Provider is already deleted: {ProviderId}", LogSanitizer.SanitizeGuid(id));
                    return false;
                }

                // Get the DELETED status
                var deletedStatus = await _context.SurchargeProviderStatuses
                    .FirstOrDefaultAsync(s => s.Code == "DELETED");

                if (deletedStatus == null)
                {
                    throw new InvalidOperationException("DELETED status not found in the database");
                }

                // Update the provider to DELETED status
                provider.StatusId = deletedStatus.StatusId;
                provider.UpdatedAt = DateTime.UtcNow;
                provider.UpdatedBy = deletedBy;

                // Debug: Log the status change
                _logger.LogDebug("Updating provider {ProviderId} status from {OldStatus} to DELETED", 
                    LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(provider.Status?.Code ?? "NULL"));

                // Deactivate all provider configurations and unset primary
                if (provider.Configurations != null)
                {
                    foreach (var config in provider.Configurations)
                    {
                        config.IsActive = false;
                        config.IsPrimary = false;
                        config.UpdatedAt = DateTime.UtcNow;
                        config.UpdatedBy = deletedBy;
                    }
                }

                await _context.SaveChangesAsync();

                // Debug: Verify the status was saved correctly
                var savedProvider = await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .FirstOrDefaultAsync(p => p.Id == id);
                
                _logger.LogDebug("After save, provider {ProviderId} status is: {Status}", 
                    LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(savedProvider?.Status?.Code ?? "NULL"));

                _logger.LogInformation("Provider {ProviderId} soft deleted by {DeletedBy} and all configs deactivated/unset primary", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(deletedBy));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting provider {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<bool> RestoreAsync(Guid id, string restoredBy)
        {
            try
            {
                var provider = await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (provider == null)
                {
                    _logger.LogWarning("Provider not found for restore: {ProviderId}", LogSanitizer.SanitizeGuid(id));
                    return false;
                }

                // Check if currently deleted
                if (provider.Status.Code != "DELETED")
                {
                    _logger.LogWarning("Provider is not deleted: {ProviderId}", LogSanitizer.SanitizeGuid(id));
                    return false;
                }

                // Get the ACTIVE status (default for restoration)
                var activeStatus = await _context.SurchargeProviderStatuses
                    .FirstOrDefaultAsync(s => s.Code == "ACTIVE");

                if (activeStatus == null)
                {
                    throw new InvalidOperationException("ACTIVE status not found in the database");
                }

                // Update the provider to ACTIVE status
                provider.StatusId = activeStatus.StatusId;
                provider.UpdatedAt = DateTime.UtcNow;
                provider.UpdatedBy = restoredBy;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Provider {ProviderId} restored by {RestoredBy}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(restoredBy));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring provider {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<int> GetCountByMerchantAsync(string merchantId)
        {
            try
            {
                return await _context.SurchargeProviders
                    .Include(p => p.Status)
                    .Where(p => p.CreatedBy == merchantId && p.Status.Code != "DELETED")
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider count for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                throw;
            }
        }

        public async Task<SurchargeProvider> AddWithLimitCheckAsync(SurchargeProvider provider, int maxProvidersPerMerchant)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Check merchant provider limit within transaction
                    var currentProviderCount = await _context.SurchargeProviders
                        .Include(p => p.Status)
                        .Where(p => p.CreatedBy == provider.CreatedBy && p.Status.Code != "DELETED")
                        .CountAsync();

                    if (currentProviderCount >= maxProvidersPerMerchant)
                    {
                        await transaction.RollbackAsync();
                        throw new InvalidOperationException($"Merchant has reached the maximum number of providers ({maxProvidersPerMerchant}). Current count: {currentProviderCount}. Error code: {SurchargeErrorCodes.Provider.PROVIDER_LIMIT_EXCEEDED}");
                    }

                    // Add the provider
                    _context.SurchargeProviders.Add(provider);
                    await _context.SaveChangesAsync();

                    // Commit the transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully created provider {ProviderId} for merchant {MerchantId} (count: {Count})", 
                        LogSanitizer.SanitizeGuid(provider.Id), LogSanitizer.SanitizeMerchantId(provider.CreatedBy), currentProviderCount + 1);

                    return provider;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
    }
} 