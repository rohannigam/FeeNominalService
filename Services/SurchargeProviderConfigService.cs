using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Repositories;
using Microsoft.Extensions.Logging;

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
                _logger.LogInformation("Getting config by ID {ConfigId}", id);
                return await _repository.GetByIdAsync(id);
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
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", merchantId);
                    return null;
                }

                _logger.LogInformation("Getting primary config for merchant {MerchantId} and provider {ProviderId}", 
                    merchantId, providerId);
                return await _repository.GetPrimaryConfigAsync(merchantGuid, providerId);
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
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", merchantId);
                    return Enumerable.Empty<SurchargeProviderConfig>();
                }

                _logger.LogInformation("Getting configs for merchant {MerchantId}", merchantId);
                return await _repository.GetByMerchantIdAsync(merchantGuid);
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
                _logger.LogInformation("Getting configs for provider {ProviderId}", providerId);
                return await _repository.GetByProviderIdAsync(providerId);
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
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", merchantId);
                    return Enumerable.Empty<SurchargeProviderConfig>();
                }

                _logger.LogInformation("Getting active configs for merchant {MerchantId}", merchantId);
                return await _repository.GetActiveConfigsAsync(merchantGuid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active configs for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task<SurchargeProviderConfig> CreateAsync(SurchargeProviderConfig config)
        {
            try
            {
                _logger.LogInformation("Creating config for merchant {MerchantId} and provider {ProviderId}", 
                    config.MerchantId, config.ProviderId);

                // Handle primary config
                if (config.IsPrimary)
                {
                    var existingPrimary = await GetPrimaryConfigAsync(config.MerchantId.ToString(), config.ProviderId);
                    if (existingPrimary != null)
                    {
                        existingPrimary.IsPrimary = false;
                        await _repository.UpdateAsync(existingPrimary);
                    }
                }

                // Set timestamps
                config.CreatedAt = DateTime.UtcNow;
                config.UpdatedAt = DateTime.UtcNow;

                // Initialize counters
                config.SuccessCount = 0;
                config.ErrorCount = 0;

                return await _repository.AddAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating config for merchant {MerchantId} and provider {ProviderId}", 
                    config.MerchantId, config.ProviderId);
                throw;
            }
        }

        public async Task<SurchargeProviderConfig> UpdateAsync(SurchargeProviderConfig config)
        {
            try
            {
                _logger.LogInformation("Updating config {ConfigId}", config.Id);

                // Check if config exists
                var existingConfig = await _repository.GetByIdAsync(config.Id);
                if (existingConfig == null)
                {
                    throw new KeyNotFoundException($"Config with ID {config.Id} not found");
                }

                // Handle primary config
                if (config.IsPrimary && !existingConfig.IsPrimary)
                {
                    var existingPrimary = await GetPrimaryConfigAsync(config.MerchantId.ToString(), config.ProviderId);
                    if (existingPrimary != null && existingPrimary.Id != config.Id)
                    {
                        existingPrimary.IsPrimary = false;
                        await _repository.UpdateAsync(existingPrimary);
                    }
                }

                // Update timestamp
                config.UpdatedAt = DateTime.UtcNow;

                return await _repository.UpdateAsync(config);
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
                _logger.LogInformation("Deleting config {ConfigId}", id);

                // Check if config exists
                if (!await _repository.ExistsAsync(id))
                {
                    return false;
                }

                return await _repository.DeleteAsync(id);
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
                return await _repository.ExistsAsync(id);
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
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", merchantId);
                    return false;
                }

                return await _repository.HasActiveConfigAsync(merchantGuid, providerId);
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
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    _logger.LogWarning("Invalid merchant ID format: {MerchantId}", merchantId);
                    return false;
                }

                return await _repository.HasPrimaryConfigAsync(merchantGuid, providerId);
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
                await _repository.UpdateLastUsedAsync(id, success, errorMessage, responseTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating last used for config {ConfigId}", id);
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(Guid configId, JsonDocument credentials)
        {
            try
            {
                _logger.LogInformation("Validating credentials for config {ConfigId}", configId);

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
                _logger.LogError(ex, "Error validating credentials for config {ConfigId}", configId);
                throw;
            }
        }

        public async Task<bool> ValidateRateLimitAsync(Guid configId)
        {
            try
            {
                _logger.LogInformation("Validating rate limit for config {ConfigId}", configId);

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
                _logger.LogError(ex, "Error validating rate limit for config {ConfigId}", configId);
                throw;
            }
        }

        public async Task<bool> ValidateTimeoutAsync(Guid configId)
        {
            try
            {
                _logger.LogInformation("Validating timeout for config {ConfigId}", configId);

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
                _logger.LogError(ex, "Error validating timeout for config {ConfigId}", configId);
                throw;
            }
        }
    }
} 