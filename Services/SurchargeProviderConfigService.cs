using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Repositories;
using Microsoft.Extensions.Logging;
using FeeNominalService.Utils;

namespace FeeNominalService.Services
{
    public class SurchargeProviderConfigService : ISurchargeProviderConfigService
    {
        private readonly ISurchargeProviderConfigRepository _repository;
        private readonly ILogger<SurchargeProviderConfigService> _logger;

        public SurchargeProviderConfigService(
            ISurchargeProviderConfigRepository repository,
            ILogger<SurchargeProviderConfigService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SurchargeProviderConfig?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting config by ID {ConfigId}", LogSanitizer.SanitizeGuid(id));
                return await _repository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting config by ID {ConfigId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<SurchargeProviderConfig?> GetPrimaryConfigAsync(string merchantId, Guid providerId)
        {
            try
            {
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                    return null;
                }

                _logger.LogInformation("Getting primary config for merchant {MerchantId} and provider {ProviderId}", 
                    LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(providerId));
                return await _repository.GetPrimaryConfigAsync(merchantGuid, providerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting primary config for merchant {MerchantId} and provider {ProviderId}", 
                    LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfig>> GetByMerchantIdAsync(string merchantId)
        {
            try
            {
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                    return Enumerable.Empty<SurchargeProviderConfig>();
                }

                _logger.LogInformation("Getting configs for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                return await _repository.GetByMerchantIdAsync(merchantGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configs for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfig>> GetByProviderIdAsync(Guid providerId)
        {
            try
            {
                _logger.LogInformation("Getting configs for provider {ProviderId}", LogSanitizer.SanitizeGuid(providerId));
                return await _repository.GetByProviderIdAsync(providerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configs for provider {ProviderId}", LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProviderConfig>> GetActiveConfigsAsync(string merchantId)
        {
            try
            {
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                    return Enumerable.Empty<SurchargeProviderConfig>();
                }

                _logger.LogInformation("Getting active configs for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                return await _repository.GetActiveConfigsAsync(merchantGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active configs for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                throw;
            }
        }

        public async Task<SurchargeProviderConfig> CreateAsync(SurchargeProviderConfig config, string requestor)
        {
            try
            {
                _logger.LogInformation("Creating config for merchant {MerchantId} and provider {ProviderId}", 
                    LogSanitizer.SanitizeMerchantId(config.MerchantId?.ToString()), LogSanitizer.SanitizeGuid(config.ProviderId));

                // Handle primary config
                if (config.IsPrimary)
                {
                    if (config.MerchantId.HasValue)
                    {
                        var existingPrimary = await GetPrimaryConfigAsync(config.MerchantId.Value.ToString(), config.ProviderId);
                        if (existingPrimary != null)
                        {
                            existingPrimary.IsPrimary = false;
                            await _repository.UpdateAsync(existingPrimary);
                            _logger.LogWarning("[PRIMARY SWITCH] User '{Requestor}' set config {NewConfigId} ('{NewConfigName}') as primary. Previous primary config {OldConfigId} ('{OldConfigName}') demoted to non-primary.",
                                LogSanitizer.SanitizeString(requestor), LogSanitizer.SanitizeGuid(config.Id), LogSanitizer.SanitizeString(config.ConfigName), LogSanitizer.SanitizeGuid(existingPrimary.Id), LogSanitizer.SanitizeString(existingPrimary.ConfigName));
                        }
                    }
                }
                else
                {
                    // If not explicitly primary, check if this is the first config for this merchant/provider
                    if (config.MerchantId.HasValue)
                    {
                        var hasPrimary = await HasPrimaryConfigAsync(config.MerchantId.Value.ToString(), config.ProviderId);
                        if (!hasPrimary)
                        {
                            config.IsPrimary = true;
                            _logger.LogInformation("No existing primary config found for merchant {MerchantId}, provider {ProviderId}. Defaulting new config {ConfigId} ('{ConfigName}') to primary.",
                                LogSanitizer.SanitizeMerchantId(config.MerchantId.ToString()), LogSanitizer.SanitizeGuid(config.ProviderId), LogSanitizer.SanitizeGuid(config.Id), LogSanitizer.SanitizeString(config.ConfigName));
                        }
                    }
                }

                // Archive all previous configs for this provider/merchant
                if (config.MerchantId.HasValue)
                {
                    var existingConfigs = await _repository.GetByProviderIdAsync(config.ProviderId);
                    foreach (var oldConfig in existingConfigs.Where(c => c.MerchantId == config.MerchantId && c.IsActive))
                    {
                        oldConfig.IsActive = false;
                        await _repository.UpdateAsync(oldConfig);
                    }
                }

                // Set timestamps
                config.CreatedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;

                // Initialize counters
                config.SuccessCount = 0;
                config.ErrorCount = 0;

                return await _repository.CreateAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating config for merchant {MerchantId} and provider {ProviderId}", 
                    LogSanitizer.SanitizeMerchantId(config.MerchantId?.ToString()), LogSanitizer.SanitizeGuid(config.ProviderId));
                throw;
            }
        }

        public async Task<SurchargeProviderConfig> UpdateAsync(SurchargeProviderConfig config, string requestor)
        {
            try
            {
                _logger.LogInformation("Updating config {ConfigId}", LogSanitizer.SanitizeGuid(config.Id));

                // Check if config exists
                var existingConfig = await _repository.GetByIdAsync(config.Id);
                if (existingConfig == null)
                {
                    throw new KeyNotFoundException($"Config with ID {config.Id} not found");
                }

                // Handle primary config
                if (config.IsPrimary && !existingConfig.IsPrimary)
                {
                    if (config.MerchantId.HasValue)
                    {
                        var existingPrimary = await GetPrimaryConfigAsync(config.MerchantId.Value.ToString(), config.ProviderId);
                        if (existingPrimary != null && existingPrimary.Id != config.Id)
                        {
                            existingPrimary.IsPrimary = false;
                            await _repository.UpdateAsync(existingPrimary);
                            _logger.LogWarning("[PRIMARY SWITCH] User '{Requestor}' set config {NewConfigId} ('{NewConfigName}') as primary. Previous primary config {OldConfigId} ('{OldConfigName}') demoted to non-primary.",
                                LogSanitizer.SanitizeString(requestor), LogSanitizer.SanitizeGuid(config.Id), LogSanitizer.SanitizeString(config.ConfigName), LogSanitizer.SanitizeGuid(existingPrimary.Id), LogSanitizer.SanitizeString(existingPrimary.ConfigName));
                        }
                    }
                }

                // Update timestamp
                config.UpdatedAt = DateTime.UtcNow;

                return await _repository.UpdateAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating config {ConfigId}", LogSanitizer.SanitizeGuid(config.Id));
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting config {ConfigId}", LogSanitizer.SanitizeGuid(id));

                // Check if config exists and get its details
                var config = await _repository.GetByIdAsync(id);
                if (config == null)
                {
                    return false;
                }

                // If deleting a primary config, promote another active config
                if (config.IsPrimary && config.MerchantId.HasValue)
                {
                    await PromoteNextPrimaryConfigAsync(config.MerchantId.Value, config.ProviderId, config.Id);
                }

                return await _repository.DeleteAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting config {ConfigId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        /// <summary>
        /// Promotes the next available active config to primary when the current primary is deleted
        /// </summary>
        private async Task PromoteNextPrimaryConfigAsync(Guid merchantId, Guid providerId, Guid deletedConfigId)
        {
            try
            {
                _logger.LogInformation("Promoting next primary config for merchant {MerchantId} and provider {ProviderId} after deleting {DeletedConfigId}", 
                    LogSanitizer.SanitizeMerchantId(merchantId.ToString()), LogSanitizer.SanitizeGuid(providerId), LogSanitizer.SanitizeGuid(deletedConfigId));

                // Get all active configs for this merchant-provider combination
                var configs = await _repository.GetByMerchantIdAsync(merchantId);
                var activeConfigs = configs.Where(c => 
                    c.ProviderId == providerId && 
                    c.IsActive && 
                    c.Id != deletedConfigId).ToList();

                // Promote the first active config to primary
                var nextPrimary = activeConfigs.FirstOrDefault();
                if (nextPrimary != null)
                {
                    _logger.LogInformation("Promoting config {ConfigId} to primary for merchant {MerchantId} and provider {ProviderId}", 
                        LogSanitizer.SanitizeGuid(nextPrimary.Id), LogSanitizer.SanitizeMerchantId(merchantId.ToString()), LogSanitizer.SanitizeGuid(providerId));
                    
                    nextPrimary.IsPrimary = true;
                    nextPrimary.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(nextPrimary);
                }
                else
                {
                    _logger.LogWarning("No active configs available to promote to primary for merchant {MerchantId} and provider {ProviderId}", 
                        LogSanitizer.SanitizeMerchantId(merchantId.ToString()), LogSanitizer.SanitizeGuid(providerId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error promoting next primary config for merchant {MerchantId} and provider {ProviderId}", 
                    LogSanitizer.SanitizeMerchantId(merchantId.ToString()), LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _repository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking config existence {ConfigId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<bool> HasActiveConfigAsync(string merchantId, Guid providerId)
        {
            try
            {
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                    return false;
                }

                return await _repository.HasActiveConfigAsync(merchantGuid, providerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking active config existence for merchant {MerchantId} and provider {ProviderId}", 
                    LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        public async Task<bool> HasPrimaryConfigAsync(string merchantId, Guid providerId)
        {
            try
            {
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                    return false;
                }

                return await _repository.HasPrimaryConfigAsync(merchantGuid, providerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking primary config existence for merchant {MerchantId} and provider {ProviderId}", 
                    LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        public async Task UpdateLastUsedAsync(Guid id, bool success, string? errorMessage = null, double? responseTime = null)
        {
            try
            {
                await _repository.UpdateLastUsedAsync(id, success, errorMessage, responseTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last used for config {ConfigId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(Guid configId, JsonDocument credentials)
        {
            try
            {
                _logger.LogInformation("Validating credentials for config {ConfigId}", LogSanitizer.SanitizeGuid(configId));

                var config = await _repository.GetByIdAsync(configId);
                if (config == null)
                {
                    throw new KeyNotFoundException($"Config with ID {configId} not found");
                }

                // TODO: Implement additional validation logic
                // This would typically involve:
                // 1. Comparing the provided credentials with the stored credentials
                // 2. Checking for required fields, data types, etc.
                // 3. Validating any provider-specific requirements

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials for config {ConfigId}", LogSanitizer.SanitizeGuid(configId));
                throw;
            }
        }

        public async Task<bool> ValidateRateLimitAsync(Guid configId)
        {
            try
            {
                _logger.LogInformation("Validating rate limit for config {ConfigId}", LogSanitizer.SanitizeGuid(configId));

                var config = await _repository.GetByIdAsync(configId);
                if (config == null)
                {
                    throw new KeyNotFoundException($"Config with ID {configId} not found");
                }

                if (!config.RateLimit.HasValue || !config.RateLimitPeriod.HasValue)
                {
                    return true; // No rate limit configured
                }

                // TODO: Implement rate limit validation logic
                // This would typically involve:
                // 1. Checking the number of requests within the rate limit period
                // 2. Comparing against the configured rate limit
                // 3. Handling any provider-specific rate limiting requirements

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating rate limit for config {ConfigId}", LogSanitizer.SanitizeGuid(configId));
                throw;
            }
        }

        public async Task<bool> ValidateTimeoutAsync(Guid configId)
        {
            try
            {
                _logger.LogInformation("Validating timeout for config {ConfigId}", LogSanitizer.SanitizeGuid(configId));

                var config = await _repository.GetByIdAsync(configId);
                if (config == null)
                {
                    throw new KeyNotFoundException($"Config with ID {configId} not found");
                }

                if (!config.Timeout.HasValue)
                {
                    return true; // No timeout configured
                }

                // TODO: Implement timeout validation logic
                // This would typically involve:
                // 1. Checking the last request time
                // 2. Comparing against the configured timeout
                // 3. Handling any provider-specific timeout requirements

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating timeout for config {ConfigId}", LogSanitizer.SanitizeGuid(configId));
                throw;
            }
        }
    }
} 