using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;

namespace FeeNominalService.Repositories
{
    public interface ISurchargeProviderConfigHistoryRepository
    {
        Task<SurchargeProviderConfigHistory?> GetByIdAsync(Guid id);
        Task<IEnumerable<SurchargeProviderConfigHistory>> GetByConfigIdAsync(Guid configId);
        Task<IEnumerable<SurchargeProviderConfigHistory>> GetByMerchantIdAsync(string merchantId);
        Task<IEnumerable<SurchargeProviderConfigHistory>> GetByProviderIdAsync(Guid providerId);
        Task<SurchargeProviderConfigHistory> AddAsync(SurchargeProviderConfigHistory history);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
} 