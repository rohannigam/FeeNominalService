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
        Task<SurchargeProvider?> GetByCodeAsync(string code);
        Task<IEnumerable<SurchargeProvider>> GetAllAsync();
        Task<IEnumerable<SurchargeProvider>> GetActiveAsync();
        Task<SurchargeProvider> CreateAsync(SurchargeProvider provider);
        Task<SurchargeProvider> UpdateAsync(SurchargeProvider provider);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByCodeAsync(string code);
        Task<bool> ValidateCredentialsSchemaAsync(Guid providerId, JsonDocument credentials);
    }
} 