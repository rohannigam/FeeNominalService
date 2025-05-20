using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;

namespace FeeNominalService.Repositories
{
    public interface ISurchargeProviderRepository
    {
        Task<SurchargeProvider?> GetByIdAsync(Guid id);
        Task<SurchargeProvider?> GetByCodeAsync(string code);
        Task<IEnumerable<SurchargeProvider>> GetAllAsync();
        Task<IEnumerable<SurchargeProvider>> GetActiveAsync();
        Task<SurchargeProvider> AddAsync(SurchargeProvider provider);
        Task<SurchargeProvider> UpdateAsync(SurchargeProvider provider);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByCodeAsync(string code);
    }
} 