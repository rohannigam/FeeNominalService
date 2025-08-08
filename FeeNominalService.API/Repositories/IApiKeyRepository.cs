using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Repositories;

public interface IApiKeyRepository
{
    Task<IEnumerable<ApiKey>> GetByMerchantIdAsync(Guid merchantId);
    Task<IEnumerable<ApiKey>> GetByScopeAsync(string scope);
    Task<ApiKey?> GetByKeyAsync(string key);
    Task<ApiKey?> GetAdminKeyAsync();
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    Task<ApiKey> UpdateAsync(ApiKey apiKey);
} 