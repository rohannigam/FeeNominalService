using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;

namespace FeeNominalService.Repositories
{
    public interface ISurchargeProviderRepository
    {
        Task<SurchargeProvider?> GetByIdAsync(Guid id);
        Task<SurchargeProvider?> GetByIdAsync(Guid id, bool includeDeleted);
        Task<SurchargeProvider?> GetByCodeAsync(string code);
        Task<IEnumerable<SurchargeProvider>> GetAllAsync();
        Task<IEnumerable<SurchargeProvider>> GetAllAsync(bool includeDeleted);
        Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId);
        Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId, bool includeDeleted);
        Task<IEnumerable<SurchargeProvider>> GetConfiguredProvidersByMerchantIdAsync(string merchantId);
        Task<IEnumerable<SurchargeProvider>> GetActiveAsync();
        Task<SurchargeProvider> AddAsync(SurchargeProvider provider);
        Task<SurchargeProvider> UpdateAsync(SurchargeProvider provider);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SoftDeleteAsync(Guid id, string deletedBy);
        Task<bool> RestoreAsync(Guid id, string restoredBy);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsAsync(Guid id, bool includeDeleted);
        Task<bool> ExistsByCodeAsync(string code);
        Task<bool> ExistsByCodeAndMerchantAsync(string code, string merchantId);
        Task<bool> HasConfigurationAsync(string merchantId, Guid providerId);
        Task<SurchargeProviderStatus?> GetStatusByCodeAsync(string code);
    }
} 