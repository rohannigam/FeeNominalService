using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FeeNominalService.Models;
using FeeNominalService.Examples.Models;

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

        public async Task<SurchargeResponse> CalculateSurchargeAsync(SurchargeRequest request)
        {
            var timestamp = DateTime.UtcNow.ToString("o");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = JsonSerializer.Serialize(request);
            var signature = GenerateSignature(timestamp, nonce, requestBody);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/surchargefee/calculate")
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Add("X-Merchant-ID", _merchantId);
            httpRequest.Headers.Add("X-Timestamp", timestamp);
            httpRequest.Headers.Add("X-Nonce", nonce);
            httpRequest.Headers.Add("X-Signature", signature);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<SurchargeResponse>()
                ?? throw new InvalidOperationException("Failed to deserialize response");
        }

        public async Task<BatchSurchargeResponse> CalculateBatchSurchargeAsync(BatchSurchargeRequest request)
        {
            var timestamp = DateTime.UtcNow.ToString("o");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = JsonSerializer.Serialize(request);
            var signature = GenerateSignature(timestamp, nonce, requestBody);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/surchargefee/calculate-batch")
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Add("X-Merchant-ID", _merchantId);
            httpRequest.Headers.Add("X-Timestamp", timestamp);
            httpRequest.Headers.Add("X-Nonce", nonce);
            httpRequest.Headers.Add("X-Signature", signature);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<BatchSurchargeResponse>()
                ?? throw new InvalidOperationException("Failed to deserialize response");
        }

        private string GenerateSignature(string timestamp, string nonce, string requestBody)
        {
            var message = $"{_merchantId}{timestamp}{nonce}{requestBody}";
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(_apiKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            return Convert.ToHexString(hash);
        }
    }
} 