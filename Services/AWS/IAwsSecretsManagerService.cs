using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Services.AWS
{
    public interface IAwsSecretsManagerService
    {
        Task<string?> GetSecretAsync(string secretName);
        Task<T?> GetSecretAsync<T>(string secretName) where T : class;
        Task StoreSecretAsync(string secretName, string secretValue);
        Task CreateSecretAsync(string secretName, Dictionary<string, string> secretValue);
        Task UpdateSecretAsync<T>(string secretName, T secretValue) where T : class;
        Task<IEnumerable<T>> GetAllSecretsAsync<T>() where T : class;
        Task<string?> GetApiKeyAsync(string merchantId);
        Task<IEnumerable<ApiKeyInfo>> GetApiKeysAsync(string merchantId);
        Task<string?> GetApiKeyByIdAsync(string merchantId, string apiKeyId);
        Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey);
        Task RevokeApiKeyAsync(string merchantId, string apiKey);
    }
} 