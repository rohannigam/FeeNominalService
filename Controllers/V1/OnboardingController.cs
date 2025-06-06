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
using FeeNominalService.Services.AWS;
using FeeNominalService.Models.Merchant.Responses;
using FeeNominalService.Models.Merchant.Requests;
using System.Text.Json;

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
        private readonly FeeNominalService.Services.AWS.IAwsSecretsManagerService _secretsManager;

        private const string ActiveStatus = "ACTIVE";

        public OnboardingController(
            ILogger<OnboardingController> logger,
            IMerchantService merchantService,
            IApiKeyService apiKeyService,
            FeeNominalService.Services.AWS.IAwsSecretsManagerService secretsManager)
        {
            _logger = logger;
            _merchantService = merchantService;
            _apiKeyService = apiKeyService;
            _secretsManager = secretsManager;
        }

        /// <summary>
        /// Generates an initial API key for a new merchant
        /// </summary>
        [HttpPost("apikey/initial-generate")]
        [ProducesResponseType(typeof(GenerateInitialApiKeyResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GenerateInitialApiKeyResponse>> GenerateInitialApiKeyAsync(
            [FromBody] GenerateInitialApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Generating initial API key for merchant {MerchantName}", request.MerchantName);

                // Create merchant
                var merchant = await _merchantService.CreateMerchantAsync(request, "SYSTEM");

                // Generate API key
                var apiKeyResponse = await _apiKeyService.GenerateInitialApiKeyAsync(merchant.MerchantId);

                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());
                var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                // Create audit trail entry for initial API key generation
                await _merchantService.CreateAuditTrailAsync(
                    merchant.MerchantId,
                    "INITIAL_API_KEY_GENERATED",
                    "api_key",
                    null,
                    JsonSerializer.Serialize(apiKeyResponse),
                    performedBy
                );

                var response = new GenerateInitialApiKeyResponse
                {
                    MerchantId = merchant.MerchantId,
                    ExternalMerchantId = merchant.ExternalMerchantId,
                    ExternalMerchantGuid = merchant.ExternalMerchantGuid,
                    MerchantName = merchant.Name,
                    StatusId = merchant.StatusId,
                    StatusCode = merchant.StatusCode,
                    StatusName = merchant.StatusName,
                    ApiKey = apiKeyResponse.ApiKey,
                    ApiKeyId = apiKeyResponse.ApiKeyId,
                    Secret = apiKeyResponse.Secret,
                    ExpiresAt = apiKeyResponse.ExpiresAt,
                    CreatedAt = apiKeyResponse.CreatedAt,
                    RateLimit = apiKeyResponse.RateLimit,
                    AllowedEndpoints = apiKeyResponse.AllowedEndpoints,
                    OnboardingMetadata = apiKeyResponse.OnboardingMetadata
                };

                _logger.LogInformation("Successfully generated initial API key for merchant {MerchantId}", merchant.MerchantId);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while generating initial API key");
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating initial API key");
                return StatusCode(500, new { error = "An error occurred while generating the API key" });
            }
        }

        /// <summary>
        /// Gets merchant audit trail
        /// </summary>
        [HttpGet("merchants/{merchantId}/audit-trail")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MerchantAuditTrail>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<MerchantAuditTrail>>>> GetMerchantAuditTrail(Guid merchantId)
        {
            try
            {
                _logger.LogInformation("Retrieving audit trail for merchant {MerchantId}", merchantId);

                var auditTrail = await _merchantService.GetMerchantAuditTrailAsync(merchantId);
                return Ok(new ApiResponse<IEnumerable<MerchantAuditTrail>>
                {
                    Message = "Audit trail retrieved successfully",
                    Success = true,
                    Data = auditTrail
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", merchantId);
                return NotFound(new ApiResponse
                {
                    Message = ex.Message,
                    Success = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit trail for merchant {MerchantId}", merchantId);
                return StatusCode(500, new ApiResponse
                {
                    Message = "Failed to retrieve audit trail",
                    Success = false
                });
            }
        }

        /// <summary>
        /// Gets merchant by external ID
        /// </summary>
        [HttpGet("merchants/external/{externalMerchantId}")]
        [ProducesResponseType(typeof(ApiResponse<Merchant>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<Merchant>>> GetMerchantByExternalId(string externalMerchantId)
        {
            try
            {
                _logger.LogInformation("Retrieving merchant with external ID {ExternalMerchantId}", externalMerchantId);

                var merchant = await _merchantService.GetByExternalMerchantIdAsync(externalMerchantId);
                if (merchant == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Message = $"Merchant not found with external ID {externalMerchantId}",
                        Success = false
                    });
                }

                return Ok(new ApiResponse<Merchant>
                {
                    Message = "Merchant retrieved successfully",
                    Success = true,
                    Data = merchant
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant with external ID {ExternalMerchantId}", externalMerchantId);
                return StatusCode(500, new ApiResponse
                {
                    Message = "Failed to retrieve merchant",
                    Success = false
                });
            }
        }

        /// <summary>
        /// Gets merchant by external GUID
        /// </summary>
        [HttpGet("merchants/external-guid/{externalMerchantGuid}")]
        [ProducesResponseType(typeof(ApiResponse<Merchant>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<Merchant>>> GetMerchantByExternalGuid(Guid externalMerchantGuid)
        {
            try
            {
                _logger.LogInformation("Retrieving merchant with external GUID {ExternalMerchantGuid}", externalMerchantGuid);

                var merchant = await _merchantService.GetByExternalMerchantGuidAsync(externalMerchantGuid);
                if (merchant == null)
                {
                    return NotFound(new ApiResponse
                    {
                        Message = $"Merchant not found with external GUID {externalMerchantGuid}",
                        Success = false
                    });
                }

                return Ok(new ApiResponse<Merchant>
                {
                    Message = "Merchant retrieved successfully",
                    Success = true,
                    Data = merchant
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving merchant with external GUID {ExternalMerchantGuid}", externalMerchantGuid);
                return StatusCode(500, new ApiResponse
                {
                    Message = "Failed to retrieve merchant",
                    Success = false
                });
            }
        }

        [HttpPost("apikey/generate")]
        [ProducesResponseType(typeof(ApiResponse<GenerateApiKeyResponse>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<GenerateApiKeyResponse>>> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Generating API key for merchant {MerchantId}", request.MerchantId);

                // Check if merchant exists
                var merchant = await _merchantService.GetMerchantAsync(request.MerchantId);
                if (merchant == null)
                {
                    _logger.LogWarning("Attempt to generate API key for non-existent merchant {MerchantId}", request.MerchantId);
                    return NotFound(new ApiResponse
                    {
                        Message = $"Merchant not found: {request.MerchantId}",
                        Success = false
                    });
                }

                // Generate API key
                var response = await _apiKeyService.GenerateApiKeyAsync(request);

                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());
                var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                // Create audit trail entry for API key generation
                await _merchantService.CreateAuditTrailAsync(
                    request.MerchantId,
                    "API_KEY_GENERATED",
                    "api_key",
                    null,
                    JsonSerializer.Serialize(new
                    {
                        ApiKey = response.ApiKey,
                        MerchantId = response.MerchantId,
                        ExternalMerchantId = response.ExternalMerchantId,
                        MerchantName = response.MerchantName,
                        RateLimit = response.RateLimit,
                        AllowedEndpoints = response.AllowedEndpoints,
                        Purpose = response.Purpose,
                        ExpiresAt = response.ExpiresAt
                    }),
                    performedBy
                );

                return Ok(new ApiResponse<GenerateApiKeyResponse>
                {
                    Message = "API key generated successfully",
                    Success = true,
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for merchant {MerchantId}", request.MerchantId);
                return StatusCode(500, new ApiResponse
                {
                    Message = "Failed to generate API key",
                    Success = false
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

                // Fetch old API key info for audit trail
                var oldApiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(apiKeyInfo.ApiKey);
                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());
                var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                // Create audit trail entry for API key update
                await _merchantService.CreateAuditTrailAsync(
                    Guid.Parse(request.MerchantId),
                    "API_KEY_UPDATED",
                    "api_key",
                    JsonSerializer.Serialize(oldApiKeyInfo),
                    JsonSerializer.Serialize(apiKeyInfo),
                    performedBy
                );

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
                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());
                var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                // Create audit trail entry for API key revocation
                await _merchantService.CreateAuditTrailAsync(
                    Guid.Parse(request.MerchantId),
                    "API_KEY_REVOKED",
                    "api_key",
                    null,
                    apiKey + "_Revoked",
                    performedBy
                );

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
        [ProducesResponseType(typeof(ApiResponse<MerchantResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<MerchantResponse>>> CreateMerchant([FromBody] GenerateInitialApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Creating merchant with external ID {ExternalMerchantId}", request.ExternalMerchantId);

                var createdMerchant = await _merchantService.CreateMerchantAsync(request, "SYSTEM");
                return CreatedAtAction(
                    nameof(GetMerchant),
                    new { id = createdMerchant.MerchantId },
                    new ApiResponse<MerchantResponse>
                    {
                        Message = "Merchant created successfully",
                        Success = true,
                        Data = createdMerchant
                    });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating merchant with external ID {ExternalMerchantId}", request.ExternalMerchantId);
                return BadRequest(new ApiResponse
                {
                    Message = ex.Message,
                    Success = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating merchant with external ID {ExternalMerchantId}", request.ExternalMerchantId);
                return StatusCode(500, new ApiResponse
                {
                    Message = "Failed to create merchant",
                    Success = false
                });
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

        [HttpPost("merchants/{merchantId}/api-keys")]
        public async Task<ActionResult<ApiKeyInfo>> GenerateApiKey(
            Guid merchantId,
            [FromBody] GenerateApiKeyRequest request,
            [FromHeader(Name = "X-Onboarding-Metadata")] string? onboardingMetadataHeader)
        {
            try
            {
                var onboardingMetadata = ParseOnboardingMetadata(onboardingMetadataHeader);
                var updatedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                var apiKeyInfo = await _merchantService.GenerateApiKeyAsync(merchantId, request, onboardingMetadata);
                
                // Create audit trail for API key generation
                await _merchantService.CreateAuditTrailAsync(
                    merchantId,
                    "API_KEY_GENERATED",
                    "api_key",
                    null,
                    apiKeyInfo.ApiKey,
                    updatedBy
                );

                return Ok(apiKeyInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for merchant {MerchantId}", merchantId);
                return StatusCode(500, new { message = "Failed to generate API key", success = false });
            }
        }

        [HttpPut("{merchantId}")]
        public async Task<ActionResult<MerchantResponse>> UpdateMerchant(
            Guid merchantId,
            [FromBody] UpdateMerchantRequest request)
        {
            try
            {
                _logger.LogInformation("Updating merchant {MerchantId}", merchantId);

                // Parse onboarding metadata from header
                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());

                // Update merchant
                var updatedMerchant = await _merchantService.UpdateMerchantAsync(
                    merchantId, 
                    request.Name,
                    onboardingMetadata?.AdminUserId ?? "SYSTEM"
                );

                // Create audit trail
                await _merchantService.CreateAuditTrailAsync(
                    merchantId,
                    "MERCHANT_UPDATED",
                    "merchant",
                    null,
                    System.Text.Json.JsonSerializer.Serialize(request),
                    onboardingMetadata?.AdminUserId ?? "SYSTEM"
                );

                _logger.LogInformation("Successfully updated merchant {MerchantId}", merchantId);

                return Ok(new MerchantResponse
                {
                    MerchantId = updatedMerchant.MerchantId,
                    Name = updatedMerchant.Name,
                    StatusId = updatedMerchant.StatusId,
                    StatusCode = updatedMerchant.Status.Code,
                    StatusName = updatedMerchant.Status.Name,
                    CreatedAt = updatedMerchant.CreatedAt,
                    UpdatedAt = updatedMerchant.UpdatedAt,
                    CreatedBy = updatedMerchant.CreatedBy
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant {MerchantId} not found", merchantId);
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant {MerchantId}", merchantId);
                return StatusCode(500, new { error = "An error occurred while updating the merchant" });
            }
        }

        /// <summary>
        /// Rotates an API key
        /// </summary>
        [HttpPost("apikey/rotate")]
        [ProducesResponseType(typeof(ApiResponse<ApiKeyInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RotateApiKey([FromBody] RotateApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Rotating API key for merchant {MerchantId}", request.MerchantId);

                // Validate the API key exists and belongs to the merchant
                var apiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(request.ApiKey);
                if (apiKeyInfo == null)
                {
                    _logger.LogWarning("API key {ApiKey} not found", request.ApiKey);
                    return NotFound(new ApiResponse
                    {
                        Message = $"API key {request.ApiKey} not found",
                        Success = false
                    });
                }

                // Rotate the API key
                var rotatedApiKeyInfo = await _apiKeyService.RotateApiKeyAsync(request.MerchantId);
                if (rotatedApiKeyInfo == null)
                {
                    _logger.LogWarning("Failed to rotate API key for merchant {MerchantId}", request.MerchantId);
                    return BadRequest(new ApiResponse
                    {
                        Message = "Failed to rotate API key",
                        Success = false
                    });
                }

                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());
                var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                // Create audit trail entry for API key rotation
                await _merchantService.CreateAuditTrailAsync(
                    Guid.Parse(request.MerchantId),
                    "API_KEY_ROTATED",
                    "api_key",
                    JsonSerializer.Serialize(apiKeyInfo),
                    JsonSerializer.Serialize(rotatedApiKeyInfo),
                    performedBy
                );

                _logger.LogInformation("Successfully rotated API key for merchant {MerchantId}", request.MerchantId);
                return Ok(new ApiResponse<ApiKeyInfo>
                {
                    Message = "API key rotated successfully",
                    Success = true,
                    Data = rotatedApiKeyInfo
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", request.MerchantId);
                return NotFound(new ApiResponse
                {
                    Message = $"Merchant not found: {request.MerchantId}",
                    Success = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating API key for merchant {MerchantId}", request.MerchantId);
                return StatusCode(500, new ApiResponse
                {
                    Message = "An error occurred while rotating the API key",
                    Success = false
                });
            }
        }

        private OnboardingMetadata? ParseOnboardingMetadata(string? metadataHeader)
        {
            if (string.IsNullOrEmpty(metadataHeader))
            {
                return null;
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<OnboardingMetadata>(metadataHeader);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse onboarding metadata header");
                return null;
            }
        }
    }

    public class UpdateMerchantStatusRequest
    {
        public int StatusId { get; set; }
    }
} 