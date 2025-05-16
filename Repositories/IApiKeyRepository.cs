using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Repositories;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByKeyAsync(string key);
    Task<List<ApiKey>> GetByMerchantIdAsync(Guid merchantId);
    Task<ApiKey> CreateAsync(ApiKey apiKey);
    Task<ApiKey> UpdateAsync(ApiKey apiKey);
} 