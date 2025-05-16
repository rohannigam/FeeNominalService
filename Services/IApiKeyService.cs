using System.Threading.Tasks;
using System.Collections.Generic;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Services
{
    public interface IApiKeyService
    {
        Task<GenerateApiKeyResponse> GenerateInitialApiKeyAsync(GenerateApiKeyRequest request);
        Task<GenerateApiKeyResponse> GenerateApiKeyAsync(GenerateApiKeyRequest request);
        Task<bool> RevokeApiKeyAsync(RevokeApiKeyRequest request);
        Task<IEnumerable<ApiKeyInfo>> GetMerchantApiKeysAsync(string merchantId);
        Task<ApiKeyInfo> GetApiKeyInfoAsync(string apiKey);
        Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature);
        Task<ApiKeyInfo> GetApiKeyAsync(string merchantId);
        Task<ApiKeyInfo> UpdateApiKeyAsync(UpdateApiKeyRequest request);
        Task<ApiKeyInfo> RotateApiKeyAsync(string merchantId);
    }
} 