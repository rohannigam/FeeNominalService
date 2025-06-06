using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.Merchant.Responses;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Repositories;
using FeeNominalService.Models.ApiKey;
using System.Text.Json;

namespace FeeNominalService.Services
{
    public static class MerchantStatusIds
    {
        public const int Suspended = -2;
        public const int Inactive = -1;
        public const int Unknown = 0;
        public const int Active = 1;
        public const int Pending = 2;
        public const int Verified = 3;
    }

    public class MerchantService : IMerchantService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MerchantService> _logger;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IMerchantAuditTrailRepository _auditTrailRepository;

        public MerchantService(
            ApplicationDbContext context,
            ILogger<MerchantService> logger,
            IMerchantRepository merchantRepository,
            IMerchantAuditTrailRepository auditTrailRepository)
        {
            _context = context;
            _logger = logger;
            _merchantRepository = merchantRepository;
            _auditTrailRepository = auditTrailRepository;
        }

        public async Task<MerchantResponse> CreateMerchantAsync(GenerateInitialApiKeyRequest request, string createdBy)
        {
            try
            {
                // Check if merchant already exists
                if (await _merchantRepository.ExistsByExternalMerchantIdAsync(request.ExternalMerchantId))
                {
                    _logger.LogWarning("Merchant with external ID {ExternalMerchantId} already exists", request.ExternalMerchantId);
                    throw new InvalidOperationException($"Merchant with external ID {request.ExternalMerchantId} already exists");
                }

                if (request.ExternalMerchantGuid.HasValue && 
                    await _merchantRepository.ExistsByExternalMerchantGuidAsync(request.ExternalMerchantGuid.Value))
                {
                    _logger.LogWarning("Merchant with external GUID {ExternalMerchantGuid} already exists", request.ExternalMerchantGuid);
                    throw new InvalidOperationException($"Merchant with external GUID {request.ExternalMerchantGuid} already exists");
                }

                // Create merchant
                var merchant = new Merchant
                {
                    ExternalMerchantId = request.ExternalMerchantId,
                    ExternalMerchantGuid = request.ExternalMerchantGuid,
                    Name = request.MerchantName,
                    CreatedBy = createdBy,
                    StatusId = MerchantStatusIds.Active
                };

                var createdMerchant = await _merchantRepository.CreateAsync(merchant);

                // Convert to response DTO
                return new MerchantResponse
                {
                    MerchantId = createdMerchant.MerchantId,
                    ExternalMerchantId = createdMerchant.ExternalMerchantId,
                    ExternalMerchantGuid = createdMerchant.ExternalMerchantGuid,
                    Name = createdMerchant.Name,
                    StatusId = createdMerchant.StatusId,
                    StatusCode = createdMerchant.Status.Code,
                    StatusName = createdMerchant.Status.Name,
                    CreatedAt = createdMerchant.CreatedAt,
                    UpdatedAt = createdMerchant.UpdatedAt,
                    CreatedBy = createdMerchant.CreatedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating merchant");
                throw;
            }
        }

        public async Task<Merchant> UpdateMerchantAsync(Guid merchantId, string name, string updatedBy)
        {
            try
            {
                var merchant = await _merchantRepository.GetByIdAsync(merchantId);
                if (merchant == null)
                {
                    throw new KeyNotFoundException($"Merchant with ID {merchantId} not found");
                }

                var oldName = merchant.Name;
                merchant.Name = name;

                var updatedMerchant = await _merchantRepository.UpdateAsync(merchant);

                // Create audit trail
                await CreateAuditTrailAsync(merchantId, "UPDATE", "Name", oldName, name, updatedBy);

                _logger.LogInformation("Updated merchant {MerchantId} name from {OldName} to {NewName}", 
                    merchantId, oldName, name);

                return updatedMerchant;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task<Merchant?> GetByExternalMerchantIdAsync(string externalMerchantId)
        {
            try
            {
                return await _merchantRepository.GetByExternalMerchantIdAsync(externalMerchantId);
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
                return await _merchantRepository.GetByExternalMerchantGuidAsync(externalMerchantGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant with external GUID {ExternalMerchantGuid}", externalMerchantGuid);
                throw;
            }
        }

        public async Task<IEnumerable<MerchantAuditTrail>> GetMerchantAuditTrailAsync(Guid merchantId)
        {
            try
            {
                return await _auditTrailRepository.GetByMerchantIdAsync(merchantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit trail for merchant {MerchantId}", merchantId);
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
                    .FirstOrDefaultAsync(m => m.MerchantId == id);

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

        public async Task<Merchant> UpdateMerchantStatusAsync(Guid id, int statusId)
        {
            try
            {
                _logger.LogInformation("Updating status for merchant {MerchantId} to status {StatusId}", id, statusId);

                var merchant = await _context.Merchants
                    .Include(m => m.Status)
                    .FirstOrDefaultAsync(m => m.MerchantId == id);

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

        public async Task<IEnumerable<Merchant>> GetAllMerchantsAsync()
        {
            return await _context.Merchants
                .Include(m => m.Status)
                .Include(m => m.ApiKeys)
                .ToListAsync();
        }

        public async Task<bool> IsMerchantActiveAsync(Guid merchantId)
        {
            var merchant = await _context.Merchants
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.MerchantId == merchantId);

            return merchant?.Status?.IsActive ?? false;
        }

        public async Task<ApiKeyInfo> GenerateApiKeyAsync(Guid merchantId, GenerateApiKeyRequest request, OnboardingMetadata? onboardingMetadata)
        {
            try
            {
                _logger.LogInformation("Generating API key for merchant {MerchantId}", merchantId);

                // Verify merchant exists
                var merchant = await _merchantRepository.GetByIdAsync(merchantId);
                if (merchant == null)
                {
                    throw new KeyNotFoundException($"Merchant with ID {merchantId} not found");
                }

                // Create API key
                var apiKey = new ApiKey
                {
                    MerchantId = merchantId,
                    Key = Guid.NewGuid().ToString("N"),
                    Name = request.Description ?? "Default API Key",
                    Description = request.Description,
                    RateLimit = request.RateLimit ?? 1000,
                    AllowedEndpoints = request.AllowedEndpoints ?? Array.Empty<string>(),
                    Status = "ACTIVE",
                    ExpirationDays = 30,
                    CreatedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM",
                    OnboardingReference = onboardingMetadata?.OnboardingReference ?? string.Empty,
                    Purpose = request.Purpose ?? "PRODUCTION"
                };

                // Save API key
                var createdApiKey = await _context.ApiKeys.AddAsync(apiKey);
                await _context.SaveChangesAsync();

                // Create audit trail
                await CreateAuditTrailAsync(
                    merchantId,
                    "API_KEY_GENERATED",
                    "api_key",
                    null,
                    apiKey.Key,
                    onboardingMetadata?.AdminUserId ?? "SYSTEM"
                );

                _logger.LogInformation("Successfully generated API key for merchant {MerchantId}", merchantId);

                return new ApiKeyInfo
                {
                    ApiKey = createdApiKey.Entity.Key,
                    Description = createdApiKey.Entity.Description ?? string.Empty,
                    RateLimit = createdApiKey.Entity.RateLimit,
                    AllowedEndpoints = createdApiKey.Entity.AllowedEndpoints,
                    Status = createdApiKey.Entity.Status,
                    ExpiresAt = createdApiKey.Entity.ExpiresAt,
                    CreatedAt = createdApiKey.Entity.CreatedAt,
                    LastRotatedAt = createdApiKey.Entity.LastRotatedAt,
                    RevokedAt = createdApiKey.Entity.RevokedAt,
                    IsRevoked = createdApiKey.Entity.Status == "REVOKED",
                    IsExpired = createdApiKey.Entity.ExpiresAt.HasValue && createdApiKey.Entity.ExpiresAt.Value < DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task CreateAuditTrailAsync(
            Guid merchantId,
            string action,
            string entityType,
            string? oldValue,
            string? newValue,
            string performedBy)
        {
            var auditTrail = new MerchantAuditTrail
            {
                MerchantAuditTrailId = Guid.NewGuid(),
                MerchantId = merchantId,
                Action = action,
                EntityType = entityType,
                PropertyName = entityType, // Using entityType as property name for now
                OldValue = oldValue,
                NewValue = newValue,
                UpdatedBy = performedBy,
                CreatedAt = DateTime.UtcNow
            };

            await _auditTrailRepository.CreateAsync(auditTrail);
        }
    }
} 