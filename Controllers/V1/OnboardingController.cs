using Microsoft.AspNetCore.Mvc;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Services;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Common;

namespace FeeNominalService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/onboarding")]
    [ApiVersion("1.0")]
    public class OnboardingController : ControllerBase
    {
        private readonly ILogger<OnboardingController> _logger;
        private readonly IMerchantService _merchantService;
        private readonly IApiKeyService _apiKeyService;

        public OnboardingController(
            ILogger<OnboardingController> logger,
            IMerchantService merchantService,
            IApiKeyService apiKeyService)
        {
            _logger = logger;
            _merchantService = merchantService;
            _apiKeyService = apiKeyService;
        }

        /// <summary>
        /// Generates a new API key for a merchant
        /// </summary>
        /// <param name="request">The API key generation request</param>
        /// <returns>The generated API key</returns>
        [HttpPost("apikey/initial-generate")]
        [ProducesResponseType(typeof(ApiResponse<GenerateApiKeyResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<GenerateApiKeyResponse>>> GenerateInitialApiKey([FromBody] GenerateApiKeyRequest request)
        {
            try
            {
                var response = await _apiKeyService.GenerateInitialApiKeyAsync(request);
                return Ok(new ApiResponse<GenerateApiKeyResponse>
                {
                    Message = "Initial API key generated successfully",
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating initial API key");
                return StatusCode(500, new ApiResponse<GenerateApiKeyResponse>
                {
                    Message = "Failed to generate initial API key",
                    Success = false,
                    Data = new GenerateApiKeyResponse
                    {
                        ApiKey = string.Empty,
                        Secret = string.Empty,
                        ExpiresAt = DateTime.UtcNow
                    }
                });
            }
        }

        [HttpPost("apikey/generate")]
        [ProducesResponseType(typeof(ApiResponse<GenerateApiKeyResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 401)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<GenerateApiKeyResponse>>> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
        {
            try
            {
                var response = await _apiKeyService.GenerateApiKeyAsync(request);
                return Ok(new ApiResponse<GenerateApiKeyResponse>
                {
                    Message = "API key generated successfully",
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key");
                return StatusCode(500, new ApiResponse<GenerateApiKeyResponse>
                {
                    Message = "Failed to generate API key",
                    Success = false,
                    Data = new GenerateApiKeyResponse
                    {
                        ApiKey = string.Empty,
                        Secret = string.Empty,
                        ExpiresAt = DateTime.UtcNow
                    }
                });
            }
        }

        /// <summary>
        /// Updates an existing API key
        /// </summary>
        [HttpPost("apikey/update")]
        [ProducesResponseType(typeof(ApiKeyInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateApiKey([FromBody] UpdateApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Updating API key for merchant {MerchantId}", request.MerchantId);

                var apiKeyInfo = await _apiKeyService.UpdateApiKeyAsync(request);
                if (apiKeyInfo?.ApiKey == null)
                {
                    _logger.LogWarning("Failed to update API key for merchant {MerchantId}", request.MerchantId);
                    return BadRequest("Failed to update API key");
                }

                _logger.LogInformation("Successfully updated API key for merchant {MerchantId}", request.MerchantId);
                return Ok(apiKeyInfo);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", request.MerchantId);
                return NotFound($"Merchant not found: {request.MerchantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key for merchant {MerchantId}", request.MerchantId);
                return StatusCode(500, "An error occurred while updating the API key");
            }
        }

        /// <summary>
        /// Revokes an API key
        /// </summary>
        [HttpPost("apikey/revoke")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokeApiKey([FromBody] RevokeApiKeyRequest request, [FromHeader(Name = "X-API-Key")] string apiKey)
        {
            try
            {
                _logger.LogInformation("Revoking API key for merchant {MerchantId}", request.MerchantId);

                if (string.IsNullOrEmpty(apiKey))
                {
                    return BadRequest("X-API-Key header is required");
                }

                request.ApiKey = apiKey;
                await _apiKeyService.RevokeApiKeyAsync(request);
                _logger.LogInformation("Successfully revoked API key for merchant {MerchantId}", request.MerchantId);
                return Ok(new { message = "API key revoked successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", request.MerchantId);
                return NotFound($"Merchant not found: {request.MerchantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key for merchant {MerchantId}", request.MerchantId);
                return StatusCode(500, "An error occurred while revoking the API key");
            }
        }

        /// <summary>
        /// Gets API key information
        /// </summary>
        [HttpGet("apikey/list")]
        [ProducesResponseType(typeof(ApiKeyInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetApiKeys([FromQuery] string merchantId)
        {
            try
            {
                _logger.LogInformation("Retrieving API keys for merchant {MerchantId}", merchantId);

                var apiKeys = await _apiKeyService.GetMerchantApiKeysAsync(merchantId);
                return Ok(apiKeys);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", merchantId);
                return NotFound($"Merchant not found: {merchantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys for merchant {MerchantId}", merchantId);
                return StatusCode(500, "An error occurred while retrieving the API keys");
            }
        }

        [HttpPost("merchants")]
        public async Task<IActionResult> CreateMerchant([FromBody] Merchant merchant)
        {
            try
            {
                var createdMerchant = await _merchantService.CreateMerchantAsync(merchant);
                return CreatedAtAction(
                    nameof(GetMerchant),
                    new { id = createdMerchant.Id },
                    createdMerchant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating merchant");
                return StatusCode(500, "An error occurred while creating the merchant");
            }
        }

        [HttpGet("merchants/{id}")]
        public async Task<IActionResult> GetMerchant(Guid id)
        {
            try
            {
                var merchant = await _merchantService.GetMerchantAsync(id);
                if (merchant == null)
                {
                    return NotFound();
                }

                return Ok(merchant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant {MerchantId}", id);
                return StatusCode(500, "An error occurred while retrieving the merchant");
            }
        }

        [HttpPost("merchants/{id}/status")]
        public async Task<IActionResult> UpdateMerchantStatus(Guid id, [FromBody] UpdateMerchantStatusRequest request)
        {
            try
            {
                var merchant = await _merchantService.UpdateMerchantStatusAsync(id, request.StatusId);
                return Ok(merchant);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant or status not found for ID {MerchantId}", id);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant status for {MerchantId}", id);
                return StatusCode(500, "An error occurred while updating the merchant status");
            }
        }
    }

    public class UpdateMerchantStatusRequest
    {
        public Guid StatusId { get; set; }
    }
} 