using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FeeNominalService.Models.Client;

namespace FeeNominalService.Services.Client
{
    public interface IRequestSigningClient
    {
        Task<SignedRequestModel> SignRequestAsync<T>(string merchantId, string secretKey, T requestBody);
        Task<HttpRequestMessage> CreateSignedRequestAsync<T>(string merchantId, string secretKey, HttpMethod method, string url, T requestBody);
    }

    public class RequestSigningClient : IRequestSigningClient
    {
        private readonly ILogger<RequestSigningClient> _logger;

        public RequestSigningClient(ILogger<RequestSigningClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<SignedRequestModel> SignRequestAsync<T>(string merchantId, string secretKey, T requestBody)
        {
            try
            {
                // Generate timestamp in ISO 8601 format
                var timestamp = DateTime.UtcNow.ToString("o");

                // Generate a unique nonce
                var nonce = GenerateNonce();

                // Serialize request body
                var requestBodyJson = JsonSerializer.Serialize(requestBody);

                // Create the string to sign
                var stringToSign = $"{timestamp}:{nonce}:{requestBodyJson}";

                // Generate signature
                var signature = GenerateSignature(secretKey, stringToSign);

                return await Task.FromResult(new SignedRequestModel
                {
                    MerchantId = merchantId,
                    Timestamp = timestamp,
                    Nonce = nonce,
                    Signature = signature,
                    RequestBody = requestBodyJson
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing request for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        public async Task<HttpRequestMessage> CreateSignedRequestAsync<T>(string merchantId, string secretKey, HttpMethod method, string url, T requestBody)
        {
            try
            {
                var signedRequest = await SignRequestAsync(merchantId, secretKey, requestBody);

                var request = new HttpRequestMessage(method, url);

                // Add required headers
                request.Headers.Add("X-Merchant-ID", signedRequest.MerchantId);
                request.Headers.Add("X-Timestamp", signedRequest.Timestamp);
                request.Headers.Add("X-Nonce", signedRequest.Nonce);
                request.Headers.Add("X-Signature", signedRequest.Signature);

                // Add content if it's a POST/PUT request
                if (method == HttpMethod.Post || method == HttpMethod.Put)
                {
                    request.Content = new StringContent(
                        signedRequest.RequestBody,
                        Encoding.UTF8,
                        "application/json"
                    );
                }

                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating signed request for merchant {MerchantId}", merchantId);
                throw;
            }
        }

        private string GenerateNonce()
        {
            using var rng = RandomNumberGenerator.Create();
            var nonceBytes = new byte[16];
            rng.GetBytes(nonceBytes);
            return Convert.ToBase64String(nonceBytes);
        }

        private string GenerateSignature(string secretKey, string stringToSign)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            return Convert.ToBase64String(hash);
        }
    }
} 