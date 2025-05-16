using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Repositories;
using FeeNominalService.Exceptions;

namespace FeeNominalService.Services
{
    public interface IRequestSigningService
    {
        Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature);
        Task<string> GenerateSignatureAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody);
    }

    public class RequestSigningService : IRequestSigningService
    {
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IMerchantRepository _merchantRepository;
        private readonly IAwsSecretsManagerService _secretsManager;
        private readonly ILogger<RequestSigningService> _logger;
        private readonly ApiKeyConfiguration _apiKeyConfig;

        public RequestSigningService(
            IApiKeyRepository apiKeyRepository,
            IMerchantRepository merchantRepository,
            IAwsSecretsManagerService secretsManager,
            ILogger<RequestSigningService> logger,
            IOptions<ApiKeyConfiguration> apiKeyConfig)
        {
            _apiKeyRepository = apiKeyRepository;
            _merchantRepository = merchantRepository;
            _secretsManager = secretsManager;
            _logger = logger;
            _apiKeyConfig = apiKeyConfig.Value;
        }

        public async Task<bool> ValidateRequestAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody, string signature)
        {
            try
            {
                // 1. Validate merchant exists
                var merchant = await _merchantRepository.GetByExternalIdAsync(merchantId);
                if (merchant == null)
                {
                    _logger.LogWarning("Merchant not found: {MerchantId}", merchantId);
                    return false;
                }

                // 2. Get API key from database
                var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
                if (apiKeyEntity == null || apiKeyEntity.Status != "ACTIVE")
                {
                    _logger.LogWarning("Invalid or inactive API key: {ApiKey}", apiKey);
                    return false;
                }

                // 3. Get secret from AWS Secrets Manager
                var secretName = $"feenominal/merchants/{merchantId}/apikeys/{apiKey}";
                var secretData = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                if (secretData == null || secretData.IsRevoked)
                {
                    _logger.LogWarning("Secret not found or revoked for API key: {ApiKey}", apiKey);
                    return false;
                }

                // 4. Validate timestamp
                if (!DateTime.TryParse(timestamp, out var requestTime))
                {
                    _logger.LogWarning("Invalid timestamp format: {Timestamp}", timestamp);
                    return false;
                }

                var timeDiff = Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes);
                if (timeDiff > _apiKeyConfig.RequestTimeWindowMinutes)
                {
                    _logger.LogWarning("Request timestamp outside allowed window: {TimeDiff} minutes", timeDiff);
                    return false;
                }

                // 5. Validate signature
                var expectedSignature = await GenerateSignatureAsync(merchantId, apiKey, timestamp, nonce, requestBody);
                return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating request signature");
                return false;
            }
        }

        public async Task<string> GenerateSignatureAsync(string merchantId, string apiKey, string timestamp, string nonce, string requestBody)
        {
            try
            {
                // 1. Get secret from AWS Secrets Manager
                var secretName = $"feenominal/merchants/{merchantId}/apikeys/{apiKey}";
                var secretData = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                if (secretData == null)
                {
                    throw new NotFoundException($"Secret not found for API key: {apiKey}");
                }

                // 2. Generate signature
                var data = $"{timestamp}{nonce}{requestBody}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretData.Secret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating signature");
                throw;
            }
        }
    }
} 