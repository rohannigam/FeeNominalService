using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using System.Text.Json;

namespace FeeNominalService.Services
{
    public interface ISurchargeProviderService
    {
        Task<SurchargeProvider?> GetByIdAsync(Guid id);
        Task<SurchargeProvider?> GetByIdAsync(Guid id, bool includeDeleted);
        Task<SurchargeProvider?> GetByCodeAsync(string code);
        Task<IEnumerable<SurchargeProvider>> GetAllAsync();
        Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId);
        Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId, bool includeDeleted);
        Task<IEnumerable<SurchargeProvider>> GetConfiguredProvidersByMerchantIdAsync(string merchantId);
        Task<IEnumerable<SurchargeProvider>> GetActiveAsync();
        Task<SurchargeProvider> CreateAsync(SurchargeProvider provider);
        Task<SurchargeProvider> CreateWithConfigurationAsync(SurchargeProvider provider, ProviderConfigurationRequest configuration, string merchantId);
        Task<SurchargeProvider> UpdateAsync(SurchargeProvider provider);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SoftDeleteAsync(Guid id, string deletedBy);
        Task<bool> RestoreAsync(Guid id, string restoredBy);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByCodeAsync(string code);
        Task<bool> HasConfigurationAsync(string merchantId, Guid providerId);
        Task<bool> ValidateCredentialsSchemaAsync(Guid providerId, JsonDocument credentials);
        Task<SurchargeProviderStatus?> GetStatusByCodeAsync(string code);
    }
} 