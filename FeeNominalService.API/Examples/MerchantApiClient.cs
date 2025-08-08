using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FeeNominalService.Models;

namespace FeeNominalService.Examples
{
    public class MerchantApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _merchantId;
        private readonly string _apiKey;

        public MerchantApiClient(string baseUrl, string merchantId, string apiKey)
        {
            _baseUrl = baseUrl;
            _merchantId = merchantId;
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        // Note: Surcharge calculation methods have been removed as part of the cleanup.
        // New surcharge endpoints will be implemented separately.

        private string GenerateSignature(string timestamp, string nonce, string requestBody)
        {
            var message = $"{_merchantId}{timestamp}{nonce}{requestBody}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(_apiKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash);
        }
    }
} 