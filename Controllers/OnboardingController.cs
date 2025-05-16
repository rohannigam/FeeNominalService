using Microsoft.AspNetCore.Mvc;
using FeeNominalService.Models;
using FeeNominalService.Services;
using Microsoft.Extensions.Options;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.ApiKey.Requests;

namespace FeeNominalService.Controllers
{
    [ApiController]
    [Route("api/v1/onboarding")]
    public class OnboardingController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<OnboardingController> _logger;
        private readonly ApiKeyConfiguration _config;

        public OnboardingController(
            IApiKeyService apiKeyService,
            IOptions<ApiKeyConfiguration> config,
            ILogger<OnboardingController> logger)
        {
            _apiKeyService = apiKeyService;
            _config = config.Value;
            _logger = logger;
        }

        [HttpPost("apikey/generate")]
        public async Task<IActionResult> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Generating API key for merchant {MerchantId}", request.MerchantId);

                var response = await _apiKeyService.GenerateApiKeyAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for merchant {MerchantId}", request.MerchantId);
                return StatusCode(500, new { success = false, message = "Error generating API key" });
            }
        }

        [HttpPost("apikey/update")]
        public async Task<IActionResult> UpdateApiKey([FromBody] UpdateApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Updating API key for merchant {MerchantId}", request.MerchantId);

                var response = await _apiKeyService.UpdateApiKeyAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key for merchant {MerchantId}", request.MerchantId);
                return StatusCode(500, new { success = false, message = "Error updating API key" });
            }
        }

        [HttpPost("apikey/revoke")]
        public async Task<IActionResult> RevokeApiKey([FromBody] RevokeApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Revoking API key for merchant {MerchantId}", request.MerchantId);

                await _apiKeyService.RevokeApiKeyAsync(request);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key for merchant {MerchantId}", request.MerchantId);
                return StatusCode(500, new { success = false, message = "Error revoking API key" });
            }
        }

        [HttpGet("apikey/list")]
        public async Task<IActionResult> GetApiKeys([FromQuery] string merchantId)
        {
            try
            {
                _logger.LogInformation("Retrieving API keys for merchant {MerchantId}", merchantId);

                var response = await _apiKeyService.GetMerchantApiKeysAsync(merchantId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys for merchant {MerchantId}", merchantId);
                return StatusCode(500, new { success = false, message = "Error retrieving API keys" });
            }
        }
    }
} 