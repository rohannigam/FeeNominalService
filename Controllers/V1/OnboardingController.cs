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
using Microsoft.AspNetCore.Authorization;
using FeeNominalService.Utils;

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
        [Authorize(Policy = "InitialKeyGeneration")]
        [ProducesResponseType(typeof(GenerateInitialApiKeyResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<GenerateInitialApiKeyResponse>> GenerateInitialApiKeyAsync(
            [FromBody] GenerateInitialApiKeyRequest request)
        {
            // Require X-Timestamp header
            if (!Request.Headers.TryGetValue("X-Timestamp", out var timestamp))
            {
                return BadRequest(new { error = "Missing X-Timestamp header." });
            }
            // Require X-Nonce header
            if (!Request.Headers.TryGetValue("X-Nonce", out var nonce))
            {
                return BadRequest(new { error = "Missing X-Nonce header." });
            }
            _logger.LogInformation("Initial API key generate requested with X-Timestamp: {Timestamp}, X-Nonce: {Nonce}", LogSanitizer.SanitizeString(timestamp.ToString()), LogSanitizer.SanitizeString(nonce.ToString()));

            try
            {
                _logger.LogInformation("Generating initial API key for merchant {MerchantName}", LogSanitizer.SanitizeString(request.MerchantName));

                // Create merchant
                var merchant = await _merchantService.CreateMerchantAsync(request, "SYSTEM");

                // Generate API key
                var apiKeyResponse = await _apiKeyService.GenerateInitialApiKeyAsync(merchant.MerchantId, request);

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
                    ExternalMerchantGuid = merchant.ExternalMerchantGuid.HasValue ? merchant.ExternalMerchantGuid : null,
                    MerchantName = merchant.Name,
                    Status = apiKeyResponse.Status,
                    ApiKey = apiKeyResponse.ApiKey,
                    ApiKeyId = apiKeyResponse.ApiKeyId,
                    Secret = apiKeyResponse.Secret,
                    ExpiresAt = apiKeyResponse.ExpiresAt,
                    CreatedAt = apiKeyResponse.CreatedAt,
                    RateLimit = apiKeyResponse.RateLimit,
                    AllowedEndpoints = apiKeyResponse.AllowedEndpoints,
                    Description = apiKeyResponse.Description,
                    Purpose = apiKeyResponse.Purpose,
                    OnboardingMetadata = apiKeyResponse.OnboardingMetadata
                };

                _logger.LogInformation("Successfully generated initial API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchant.MerchantId));
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
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MerchantAuditTrail>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<IEnumerable<MerchantAuditTrail>>>> GetMerchantAuditTrail(Guid merchantId)
        {
            try
            {
                _logger.LogInformation("Retrieving audit trail for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));

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
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                return NotFound(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID),
                    SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving audit trail for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                return StatusCode(500, new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.AUDIT_TRAIL_FAILED),
                    SurchargeErrorCodes.Onboarding.AUDIT_TRAIL_FAILED
                ));
            }
        }

        /// <summary>
        /// Gets merchant by external ID
        /// </summary>
        [HttpGet("merchants/external/{externalMerchantId}")]
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiResponse<Merchant>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<Merchant>>> GetMerchantByExternalId(string externalMerchantId)
        {
            try
            {
                _logger.LogInformation("Retrieving merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(externalMerchantId));

                var merchant = await _merchantService.GetByExternalMerchantIdAsync(externalMerchantId);
                if (merchant == null)
                {
                    return NotFound(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID),
                        SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID
                    ));
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
                _logger.LogError(ex, "Error retrieving merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(externalMerchantId));
                return StatusCode(500, new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID),
                    SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID
                ));
            }
        }

        /// <summary>
        /// Gets merchant by external GUID
        /// </summary>
        [HttpGet("merchants/external-guid/{externalMerchantGuid}")]
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiResponse<Merchant>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<Merchant>>> GetMerchantByExternalGuid(Guid externalMerchantGuid)
        {
            try
            {
                _logger.LogInformation("Retrieving merchant with external GUID {ExternalMerchantGuid}", LogSanitizer.SanitizeGuid(externalMerchantGuid));

                var merchant = await _merchantService.GetByExternalMerchantGuidAsync(externalMerchantGuid);
                if (merchant == null)
                {
                    return NotFound(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID),
                        SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID
                    ));
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
                _logger.LogError(ex, "Error retrieving merchant with external GUID {ExternalMerchantGuid}", LogSanitizer.SanitizeGuid(externalMerchantGuid));
                return StatusCode(500, new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID),
                    SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID
                ));
            }
        }

        [Authorize(Policy = "ApiKeyAccess")]
        [HttpPost("apikey/generate")]
        [ProducesResponseType(typeof(ApiResponse<GenerateApiKeyResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateApiKey([FromBody] GenerateApiKeyRequest request)
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.AUTHENTICATION_FAILED),
                    SurchargeErrorCodes.Onboarding.AUTHENTICATION_FAILED
                ));
            }
            // Enforce scope boundaries
            var scopeClaim = User.FindFirst("Scope")?.Value;
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
            if (request.IsAdmin)
            {
                if (scopeClaim != "admin")
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only admin-scope keys can create admin keys.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            else
            {
                if (scopeClaim != "merchant" || merchantIdClaim != request.MerchantId?.ToString())
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only merchant-scope keys can create merchant keys for their own merchant.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            try
            {
                // Validate merchant ID header matches request
                var (isValidMerchantId, headerMerchantId, merchantIdError) = HeaderValidationHelper.ValidateRequiredGuidHeader(Request.Headers, "X-Merchant-ID");
                if (!isValidMerchantId || headerMerchantId != request.MerchantId)
                {
                    return Unauthorized(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.INVALID_MERCHANT_ID_HEADER),
                        SurchargeErrorCodes.Onboarding.INVALID_MERCHANT_ID_HEADER
                    ));
                }

                // Get the API key from the header
                var (apiKeyHeaderValid, apiKeyHeader, apiKeyHeaderError) = HeaderValidationHelper.ValidateRequiredHeader(Request.Headers, "X-API-Key");
                if (!apiKeyHeaderValid || string.IsNullOrEmpty(apiKeyHeader))
                {
                    return Unauthorized(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MISSING_API_KEY_HEADER),
                        SurchargeErrorCodes.Onboarding.MISSING_API_KEY_HEADER
                    ));
                }

                // Fetch the API key entity from the database
                var apiKeyEntity = await _apiKeyService.GetApiKeyInfoAsync(apiKeyHeader);
                if (apiKeyEntity == null || apiKeyEntity.Status != "ACTIVE")
                {
                    return Unauthorized(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.INVALID_OR_INACTIVE_API_KEY),
                        SurchargeErrorCodes.Onboarding.INVALID_OR_INACTIVE_API_KEY
                    ));
                }

                // Ensure the API key's merchant matches the request's merchant
                if (apiKeyEntity.MerchantId != request.MerchantId)
                {
                    return Unauthorized(new ApiErrorResponse(
                        "API key merchant does not match request merchant.",
                        "MERCHANT_MISMATCH"
                    ));
                }

                // For admin API keys, skip merchant validation
                if (request.IsAdmin)
                {
                    // Generate API key
                    var adminApiKeyResponse = await _apiKeyService.GenerateApiKeyAsync(request);

                    // Log the response details for debugging
                    _logger.LogInformation("Generated admin API key response: HasApiKey={HasApiKey}, HasSecret={HasSecret}, ExpiresAt={ExpiresAt}", 
                        !string.IsNullOrEmpty(adminApiKeyResponse.ApiKey), 
                        !string.IsNullOrEmpty(adminApiKeyResponse.Secret),
                        adminApiKeyResponse.ExpiresAt);

                    var adminResponse = new ApiResponse<GenerateApiKeyResponse>
                    {
                        Success = true,
                        Message = "Admin API key generated successfully",
                        Data = adminApiKeyResponse
                    };

                    return Ok(adminResponse);
                }

                // For merchant API keys, validate merchant exists
                if (!request.MerchantId.HasValue)
                {
                    return BadRequest(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND),
                        SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND
                    ));
                }

                // Get merchant
                _logger.LogInformation("Retrieving merchant with ID {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                var merchant = await _merchantService.GetMerchantAsync(request.MerchantId.Value);
                if (merchant == null)
                {
                    _logger.LogWarning("Merchant not found with ID {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                    return NotFound(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND),
                        SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND
                    ));
                }

                // Validate onboarding metadata from request body
                if (request.OnboardingMetadata == null || 
                    string.IsNullOrEmpty(request.OnboardingMetadata.AdminUserId) || 
                    string.IsNullOrEmpty(request.OnboardingMetadata.OnboardingReference))
                {
                    return BadRequest(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.METADATA_PARSE_FAILED),
                        SurchargeErrorCodes.Onboarding.METADATA_PARSE_FAILED
                    ));
                }

                // Log claims for debugging
                var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}");
                _logger.LogInformation("Claims present: {Claims}", LogSanitizer.SanitizeString(string.Join(", ", claims)));

                // Generate API key
                var apiKeyResponse = await _apiKeyService.GenerateApiKeyAsync(request);

                // Log the response details for debugging
                _logger.LogInformation("Generated API key response for merchant {MerchantId}: HasApiKey={HasApiKey}, HasSecret={HasSecret}, ExpiresAt={ExpiresAt}", 
                    LogSanitizer.SanitizeGuid(request.MerchantId), 
                    !string.IsNullOrEmpty(apiKeyResponse.ApiKey), 
                    !string.IsNullOrEmpty(apiKeyResponse.Secret),
                    apiKeyResponse.ExpiresAt);

                // Add audit trail entry for API key generation (skip for admin keys)
                if (!request.IsAdmin && request.MerchantId.HasValue)
                {
                    var onboardingMetadata = request.OnboardingMetadata;
                    var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";
                    await _merchantService.CreateAuditTrailAsync(
                        request.MerchantId.Value,
                        "API_KEY_GENERATED",
                        "api_key",
                        null,
                        apiKeyResponse.ApiKey,
                        performedBy
                    );
                }

                var response = new ApiResponse<GenerateApiKeyResponse>
                {
                    Success = true,
                    Message = "API key generated successfully",
                    Data = apiKeyResponse
                };

                // Log the final response structure
                _logger.LogDebug("Final response structure: Success={Success}, Message={Message}, DataType={DataType}", 
                    response.Success, 
                    response.Message, 
                    response.Data?.GetType().Name ?? "null");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return StatusCode(500, new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.API_KEY_GENERATE_FAILED),
                    SurchargeErrorCodes.Onboarding.API_KEY_GENERATE_FAILED
                ));
            }
        }

        /// <summary>
        /// Updates an existing API key
        /// </summary>
        [HttpPost("apikey/update")]
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiResponse<ApiKeyInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateApiKey([FromBody] UpdateApiKeyRequest request)
        {
            // Enforce scope boundaries
            var scopeClaim = User.FindFirst("Scope")?.Value;
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
            var apiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(request.ApiKey);
            if (apiKeyInfo == null)
            {
                return NotFound("API key not found");
            }
            if (apiKeyInfo.Status == "admin")
            {
                if (scopeClaim != "admin")
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only admin-scope keys can update admin keys.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            else
            {
                if (scopeClaim != "merchant" || merchantIdClaim != apiKeyInfo.MerchantId?.ToString())
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only merchant-scope keys can update merchant keys for their own merchant.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            try
            {
                _logger.LogInformation("Updating API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));

                // Fetch old API key info for audit trail (before the update)
                var oldApiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(request.ApiKey);
                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());
                var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                var updatedApiKeyResponse = await _apiKeyService.UpdateApiKeyAsync(request, request.OnboardingMetadata);
                if (updatedApiKeyResponse?.ApiKey == null)
                {
                    _logger.LogWarning("Failed to update API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                    return BadRequest(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.API_KEY_UPDATE_FAILED),
                        SurchargeErrorCodes.Onboarding.API_KEY_UPDATE_FAILED
                    ));
                }

                // Create audit trail entry for API key update (skip for admin keys)
                if (!string.IsNullOrEmpty(request.MerchantId) && Guid.TryParse(request.MerchantId, out Guid merchantGuid))
                {
                    await _merchantService.CreateAuditTrailAsync(
                        merchantGuid,
                        "API_KEY_UPDATED",
                        "api_key",
                        JsonSerializer.Serialize(oldApiKeyInfo),
                        JsonSerializer.Serialize(updatedApiKeyResponse),
                        performedBy
                    );
                }

                _logger.LogInformation("Successfully updated API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return Ok(new ApiResponse<ApiKeyInfo>
                {
                    Success = true,
                    Message = "API key updated successfully",
                    Data = updatedApiKeyResponse
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", LogSanitizer.SanitizeString(request.MerchantId));
                return NotFound($"Merchant not found: {request.MerchantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return StatusCode(500, "An error occurred while updating the API key");
            }
        }

        [HttpPost("apikey/revoke")]
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiResponse<ApiKeyRevokeResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokeApiKey([FromBody] RevokeApiKeyRequest request)
        {
            // Enforce scope boundaries
            var scopeClaim = User.FindFirst("Scope")?.Value;
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
            var apiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(request.ApiKey);
            if (apiKeyInfo == null)
            {
                return NotFound("API key not found");
            }
            if (apiKeyInfo.Status == "admin")
            {
                if (scopeClaim != "admin")
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only admin-scope keys can revoke admin keys.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            else
            {
                if (scopeClaim != "merchant" || merchantIdClaim != apiKeyInfo.MerchantId?.ToString())
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only merchant-scope keys can revoke merchant keys for their own merchant.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            try
            {
                _logger.LogInformation("Revoking API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                if (!Guid.TryParse(request.MerchantId, out Guid merchantGuid))
                {
                    return BadRequest("Invalid merchant ID format");
                }

                var apiKey = request.ApiKey;
                var performedBy = "SYSTEM"; // TODO: Get from authenticated user

                await _apiKeyService.RevokeApiKeyAsync(request);

                // Fetch the revoked API key info for response
                var revokedApiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(apiKey);

                // Create audit trail entry for API key revocation (skip for admin keys)
                if (!string.IsNullOrEmpty(request.MerchantId))
                {
                    await _merchantService.CreateAuditTrailAsync(
                        merchantGuid,
                        "API_KEY_REVOKED",
                        "api_key",
                        null,
                        apiKey + "_Revoked",
                        performedBy
                    );
                }

                _logger.LogInformation("Successfully revoked API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return Ok(new ApiResponse<ApiKeyRevokeResponse>
                {
                    Success = true,
                    Message = "API key revoked successfully",
                    Data = new ApiKeyRevokeResponse
                    {
                        ApiKey = apiKey,
                        RevokedAt = revokedApiKeyInfo?.RevokedAt ?? DateTime.UtcNow,
                        Status = revokedApiKeyInfo?.Status ?? "REVOKED"
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", LogSanitizer.SanitizeString(request.MerchantId));
                return NotFound($"Merchant not found: {request.MerchantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return StatusCode(500, "An error occurred while revoking the API key");
            }
        }

        [HttpGet("apikey/list")]
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiKeyInfo), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetApiKeys([FromQuery] string merchantId)
        {
            try
            {
                _logger.LogInformation("Retrieving API keys for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                if (!Guid.TryParse(merchantId, out Guid merchantGuid))
                {
                    return BadRequest("Invalid merchant ID format");
                }

                var apiKeys = await _apiKeyService.GetMerchantApiKeysAsync(merchantId);
                return Ok(apiKeys);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                return NotFound($"Merchant not found: {merchantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                return StatusCode(500, "An error occurred while retrieving API keys");
            }
        }

        [HttpPost("merchants")]
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiResponse<MerchantResponse>), 201)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        [ProducesResponseType(typeof(ApiResponse), 500)]
        public async Task<ActionResult<ApiResponse<MerchantResponse>>> CreateMerchant([FromBody] GenerateInitialApiKeyRequest request)
        {
            try
            {
                _logger.LogInformation("Creating merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(request.ExternalMerchantId));

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
                _logger.LogWarning(ex, "Invalid operation while creating merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(request.ExternalMerchantId));
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_CREATE_FAILED),
                    SurchargeErrorCodes.Onboarding.MERCHANT_CREATE_FAILED
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating merchant with external ID {ExternalMerchantId}", LogSanitizer.SanitizeString(request.ExternalMerchantId));
                return StatusCode(500, new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_CREATE_FAILED),
                    SurchargeErrorCodes.Onboarding.MERCHANT_CREATE_FAILED
                ));
            }
        }

        [HttpGet("merchants/{id}")]
        [Authorize(Policy = "ApiKeyAccess")]
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
                _logger.LogError(ex, "Error retrieving merchant {MerchantId}", LogSanitizer.SanitizeGuid(id));
                return StatusCode(500, "An error occurred while retrieving the merchant");
            }
        }

        [HttpPost("merchants/{id}/status")]
        [Authorize(Policy = "ApiKeyAccess")]
        public async Task<IActionResult> UpdateMerchantStatus(Guid id, [FromBody] UpdateMerchantStatusRequest request)
        {
            try
            {
                var merchant = await _merchantService.UpdateMerchantStatusAsync(id, request.StatusId);
                return Ok(merchant);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant or status not found for ID {MerchantId}", LogSanitizer.SanitizeGuid(id));
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant status for {MerchantId}", LogSanitizer.SanitizeGuid(id));
                return StatusCode(500, "An error occurred while updating the merchant status");
            }
        }

        [HttpPost("merchants/{merchantId}/api-keys")]
        [Authorize(Policy = "ApiKeyAccess")]
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
                _logger.LogError(ex, "Error generating API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                return StatusCode(500, new { message = "Failed to generate API key", success = false });
            }
        }

        [HttpPut("{merchantId}")]
        [Authorize(Policy = "ApiKeyAccess")]
        public async Task<ActionResult<MerchantResponse>> UpdateMerchant(
            Guid merchantId,
            [FromBody] UpdateMerchantRequest request)
        {
            try
            {
                _logger.LogInformation("Updating merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));

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

                _logger.LogInformation("Successfully updated merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));

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
                _logger.LogWarning(ex, "Merchant {MerchantId} not found", LogSanitizer.SanitizeGuid(merchantId));
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating merchant {MerchantId}", LogSanitizer.SanitizeGuid(merchantId));
                return StatusCode(500, new { error = "An error occurred while updating the merchant" });
            }
        }

        /// <summary>
        /// Rotates an API key
        /// </summary>
        [HttpPost("apikey/rotate")]
        [Authorize(Policy = "ApiKeyAccess")]
        [ProducesResponseType(typeof(ApiResponse<GenerateApiKeyResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RotateApiKey([FromBody] RotateApiKeyRequest request)
        {
            // Enforce scope boundaries
            var scopeClaim = User.FindFirst("Scope")?.Value;
            var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
            var apiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(request.ApiKey);
            if (apiKeyInfo == null)
            {
                return NotFound("API key not found");
            }
            if (apiKeyInfo.Status == "admin")
            {
                if (scopeClaim != "admin")
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only admin-scope keys can rotate admin keys.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            else
            {
                if (scopeClaim != "merchant" || merchantIdClaim != apiKeyInfo.MerchantId?.ToString())
                {
                    return Unauthorized(new ApiErrorResponse(
                        "Only merchant-scope keys can rotate merchant keys for their own merchant.",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }
            }
            try
            {
                _logger.LogInformation("Rotating API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));

                // Validate the API key exists and belongs to the merchant
                if (apiKeyInfo == null)
                {
                    _logger.LogWarning("API key {ApiKey} not found", LogSanitizer.SanitizeString(request.ApiKey));
                    return NotFound(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.API_KEY_NOT_FOUND),
                        SurchargeErrorCodes.Onboarding.API_KEY_NOT_FOUND
                    ));
                }

                // Prevent rotation of revoked key (controller-level check)
                if (apiKeyInfo.Status == "REVOKED" || apiKeyInfo.IsRevoked)
                {
                    _logger.LogWarning("Attempted to rotate a revoked API key: {ApiKey}", LogSanitizer.SanitizeString(request.ApiKey));
                    return BadRequest(new ApiErrorResponse(
                        "Cannot rotate a revoked API key.",
                        SurchargeErrorCodes.Onboarding.API_KEY_ROTATE_FAILED
                    ));
                }

                // Rotate the API key and get the new secret
                var rotatedApiKeyResponse = await _apiKeyService.RotateApiKeyAsync(request.MerchantId, request.OnboardingMetadata, request.ApiKey);
                if (rotatedApiKeyResponse == null)
                {
                    _logger.LogWarning("Failed to rotate API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                    return BadRequest(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.API_KEY_ROTATE_FAILED),
                        SurchargeErrorCodes.Onboarding.API_KEY_ROTATE_FAILED
                    ));
                }

                var onboardingMetadata = ParseOnboardingMetadata(Request.Headers["X-Onboarding-Metadata"].ToString());
                var performedBy = onboardingMetadata?.AdminUserId ?? "SYSTEM";

                // Create audit trail entry for API key rotation (skip for admin keys)
                if (!string.IsNullOrEmpty(request.MerchantId) && Guid.TryParse(request.MerchantId, out Guid merchantGuid))
                {
                    await _merchantService.CreateAuditTrailAsync(
                        merchantGuid,
                        "API_KEY_ROTATED",
                        "api_key",
                        JsonSerializer.Serialize(apiKeyInfo),
                        JsonSerializer.Serialize(rotatedApiKeyResponse),
                        performedBy
                    );
                }

                _logger.LogInformation("Successfully rotated API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return Ok(new ApiResponse<GenerateApiKeyResponse>
                {
                    Message = "API key rotated successfully",
                    Success = true,
                    Data = rotatedApiKeyResponse
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Merchant not found: {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return NotFound(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND),
                    SurchargeErrorCodes.Onboarding.MERCHANT_NOT_FOUND
                ));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during API key rotation for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return BadRequest(new ApiErrorResponse(
                    ex.Message,
                    SurchargeErrorCodes.Onboarding.API_KEY_ROTATE_FAILED
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rotating API key for merchant {MerchantId}", LogSanitizer.SanitizeGuid(request.MerchantId));
                return StatusCode(500, new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Onboarding.API_KEY_ROTATE_FAILED),
                    SurchargeErrorCodes.Onboarding.API_KEY_ROTATE_FAILED
                ));
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