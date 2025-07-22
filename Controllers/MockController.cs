using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeeNominalService.Services;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Services.AWS;

namespace FeeNominalService.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class MockController : ControllerBase
    {
        private static Dictionary<string, string> _apiKeySecrets = new Dictionary<string, string>();
        private readonly ILogger<MockController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IAwsSecretsManagerService _secretsManager;
        private readonly SecretNameFormatter _secretNameFormatter;

        public MockController(
            ILogger<MockController> logger, 
            IWebHostEnvironment environment,
            IAwsSecretsManagerService secretsManager,
            SecretNameFormatter secretNameFormatter)
        {
            _logger = logger;
            _environment = environment;
            _secretsManager = secretsManager;
            _secretNameFormatter = secretNameFormatter;
        }

        [HttpPost("onboarding/apikey/initial-generate")]
        public async Task<IActionResult> GenerateInitialApiKey([FromBody] JsonElement request)
        {
            try
            {
                var mockData = LoadMockData("ApiKeyMockData.json");
                var initialApiKeyData = mockData.GetProperty("initialApiKey");

                // Store the API key and secret mapping
                var apiKey = initialApiKeyData.GetProperty("response").GetProperty("data").GetProperty("apiKey").GetString();
                var secret = initialApiKeyData.GetProperty("response").GetProperty("data").GetProperty("secret").GetString();
                var merchantId = request.GetProperty("merchantId").GetString();

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(merchantId))
                {
                    _logger.LogError("API key, secret, or merchant ID is null or empty in request or mock data");
                    return StatusCode(500, new { error = "Invalid request or mock data configuration" });
                }

                // Store in the mock secrets manager
                var merchantIdGuid = Guid.Parse(merchantId);
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantIdGuid, apiKey);
                var secretValue = new ApiKeySecret
                {
                    ApiKey = apiKey,
                    Secret = secret,
                    MerchantId = merchantIdGuid,
                    CreatedAt = DateTime.UtcNow,
                    LastRotated = null,
                    IsRevoked = false,
                    RevokedAt = null,
                    Status = "ACTIVE"
                };

                await _secretsManager.StoreSecretAsync(secretName, JsonSerializer.Serialize(secretValue));
                _logger.LogInformation("Stored API key secret for {MerchantId} with key {ApiKey}", merchantId, apiKey);

                return Ok(initialApiKeyData.GetProperty("response"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating initial API key");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        [HttpPost("onboarding/apikey/get-secret")]
        public async Task<IActionResult> GetApiKeySecret([FromBody] JsonElement request)
        {
            try
            {
                var apiKey = request.GetProperty("apiKey").GetString();
                var merchantId = request.GetProperty("merchantId").GetString();
                
                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(merchantId))
                {
                    return BadRequest(new { error = "API key and merchant ID are required" });
                }

                var merchantIdGuid = Guid.Parse(merchantId);
                var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantIdGuid, apiKey);
                var secret = await _secretsManager.GetSecretAsync<ApiKeySecret>(secretName);
                
                if (secret == null)
                {
                    return NotFound(new { error = "API key not found" });
                }

                return Ok(new { data = new { secret = secret.Secret } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key secret");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        private JsonElement LoadMockData(string fileName)
        {
            var mockDataPath = Path.Combine(_environment.ContentRootPath, "MockData", fileName);
            if (!System.IO.File.Exists(mockDataPath))
            {
                throw new FileNotFoundException($"Mock data file not found: {fileName}");
            }

            var jsonString = System.IO.File.ReadAllText(mockDataPath);
            return JsonSerializer.Deserialize<JsonElement>(jsonString);
        }
    }
} 