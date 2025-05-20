using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;

namespace FeeNominalService.Repositories
{
    public interface ISurchargeProviderConfigRepository
    {
        Task<SurchargeProviderConfig?> GetByIdAsync(Guid id);
        Task<SurchargeProviderConfig?> GetPrimaryConfigAsync(string merchantId, Guid providerId);
        Task<IEnumerable<SurchargeProviderConfig>> GetByMerchantIdAsync(string merchantId);
        Task<IEnumerable<SurchargeProviderConfig>> GetByProviderIdAsync(Guid providerId);
        Task<IEnumerable<SurchargeProviderConfig>> GetActiveConfigsAsync(string merchantId);
        Task<SurchargeProviderConfig> AddAsync(SurchargeProviderConfig config);
        Task<SurchargeProviderConfig> UpdateAsync(SurchargeProviderConfig config);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> HasActiveConfigAsync(string merchantId, Guid providerId);
        Task<bool> HasPrimaryConfigAsync(string merchantId, Guid providerId);
        Task UpdateLastUsedAsync(Guid id, bool success, string? errorMessage = null, double? responseTime = null);
    }
} 