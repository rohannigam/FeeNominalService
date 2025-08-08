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
        Task<bool> ValidateApiKeyAsync(string merchantId, string apiKey);
        Task RevokeApiKeyAsync(string merchantId, string apiKey);
        Task<SecureApiKeySecret?> GetSecureApiKeySecretAsync(string secretName);
        Task StoreSecureApiKeySecretAsync(string secretName, SecureApiKeySecret secureSecret);
        Task UpdateSecureApiKeySecretAsync(string secretName, SecureApiKeySecret secureSecret);
        
        // Secure methods that avoid passing sensitive data as parameters
        Task<ApiKeySecret?> GetMerchantSecretSecurelyAsync(string merchantId, string apiKey);
        Task StoreMerchantSecretSecurelyAsync(string merchantId, string apiKey, string secretValue);
        Task UpdateMerchantSecretSecurelyAsync<T>(string merchantId, string apiKey, T secretValue) where T : class;
        Task<ApiKeySecret?> GetAdminSecretSecurelyAsync(string serviceName);
        Task StoreAdminSecretSecurelyAsync(string serviceName, string secretValue);
        Task UpdateAdminSecretSecurelyAsync<T>(string serviceName, T secretValue) where T : class;
    }
} 