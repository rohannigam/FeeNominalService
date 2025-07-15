using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Models.Common;
using FeeNominalService.Services;
using FeeNominalService.Settings;
using FeeNominalService.Utils;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace FeeNominalService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/merchants/{merchantId}/surcharge-providers")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "ApiKeyAccess")]
    public class SurchargeProviderController : ControllerBase
    {
        private readonly ISurchargeProviderService _surchargeProviderService;
        private readonly ICredentialValidationService _credentialValidationService;
        private readonly SurchargeProviderValidationSettings _validationSettings;
        private readonly ILogger<SurchargeProviderController> _logger;

        public SurchargeProviderController(
            ISurchargeProviderService surchargeProviderService,
            ICredentialValidationService credentialValidationService,
            SurchargeProviderValidationSettings validationSettings,
            ILogger<SurchargeProviderController> logger)
        {
            _surchargeProviderService = surchargeProviderService;
            _credentialValidationService = credentialValidationService;
            _validationSettings = validationSettings;
            _logger = logger;
        }

        /// <summary>
        /// Create a new surcharge provider for the specified merchant
        /// </summary>
        /// <param name="merchantId">Merchant ID</param>
        /// <param name="request">Surcharge provider request</param>
        /// <returns>Created surcharge provider</returns>
        [HttpPost]
        public async Task<IActionResult> CreateProvider(string merchantId, [FromBody] SurchargeProviderRequest request)
        {
            try
            {
                // Log the request with masked sensitive data
                var maskedRequest = SensitiveDataMasker.MaskSensitiveData(request);
                
                _logger.LogInformation("Creating surcharge provider for merchant: {MerchantId}. Request:\n{MaskedRequest}", 
                    merchantId, maskedRequest);

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                }

                // Validate the credentials schema structure with enhanced validation
                if (!request.ValidateCredentialsSchema(out var schemaErrors, _validationSettings))
                {
                    return BadRequest(ApiErrorResponse.InvalidCredentialsSchema(schemaErrors));
                }

                // Validate configuration if provided with enhanced validation
                if (!request.ValidateConfiguration(out var configErrors, _validationSettings))
                {
                    return BadRequest(ApiErrorResponse.InvalidConfiguration(configErrors));
                }

                // Validate actual credentials if configuration is provided
                if (request.Configuration?.Credentials != null)
                {
                    var credentialsValidation = ValidateCredentials(request.Configuration.Credentials);
                    if (!credentialsValidation.IsValid)
                    {
                        return BadRequest(ApiErrorResponse.InvalidCredentials(credentialsValidation.Errors));
                    }
                }
                
                // Always set status to ACTIVE for new providers
                var status = await _surchargeProviderService.GetStatusByCodeAsync("ACTIVE");
                if (status == null)
                {
                    return BadRequest(ApiErrorResponse.SystemConfigurationError("ACTIVE status not found in the database"));
                }

                // Convert the credentials schema to JsonDocument
                var credentialsSchema = JsonSerializer.SerializeToDocument(request.CredentialsSchema);

                var provider = new SurchargeProvider
                {
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    BaseUrl = request.BaseUrl,
                    AuthenticationType = request.AuthenticationType,
                    CredentialsSchema = credentialsSchema,
                    StatusId = status.StatusId,
                    CreatedBy = merchantId,
                    UpdatedBy = merchantId
                };

                SurchargeProvider result;

                // If configuration is provided, create both provider and configuration
                if (request.Configuration != null)
                {
                    result = await _surchargeProviderService.CreateWithConfigurationAsync(provider, request.Configuration, merchantId);
                }
                else
                {
                    // Otherwise, just create the provider
                    result = await _surchargeProviderService.CreateAsync(provider);
                }

                // Log successful creation
                _logger.LogInformation("Successfully created surcharge provider: {ProviderId} ({ProviderName}) for merchant: {MerchantId}", 
                    result.Id, result.Name, merchantId);

                return Ok(result.ToResponse());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating provider for merchant {MerchantId}", merchantId);
                
                if (ex.Message.Contains("maximum number of providers"))
                {
                    return BadRequest(ApiErrorResponse.ProviderLimitExceeded(_validationSettings.MaxProvidersPerMerchant, 0));
                }
                else if (ex.Message.Contains("already exists"))
                {
                    return BadRequest(ApiErrorResponse.ProviderCodeExists(request.Code));
                }
                else if (ex.Message.Contains("Credentials schema is required"))
                {
                    return BadRequest(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_CREDENTIALS_INVALID),
                        SurchargeErrorCodes.Provider.PROVIDER_CREDENTIALS_INVALID,
                        ex.Message
                    ));
                }
                else if (ex.Message.Contains("Invalid credentials schema"))
                {
                    return BadRequest(ApiErrorResponse.InvalidCredentialsSchema(new List<string> { ex.Message }));
                }
                else if (ex.Message.Contains("ACTIVE status not found"))
                {
                    return BadRequest(ApiErrorResponse.SystemConfigurationError(ex.Message));
                }
                
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Validation.INVALID_DATA),
                    SurchargeErrorCodes.Validation.INVALID_DATA,
                    ex.Message
                ));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Key not found while creating provider for merchant {MerchantId}", merchantId);
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND),
                    SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND,
                    ex.Message
                ));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating provider for merchant {MerchantId}", merchantId);
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Validation.INVALID_DATA),
                    SurchargeErrorCodes.Validation.INVALID_DATA,
                    ex.Message
                ));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON parsing error while creating provider for merchant {MerchantId}", merchantId);
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Validation.INVALID_DATA),
                    SurchargeErrorCodes.Validation.INVALID_DATA,
                    ex.Message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating surcharge provider for merchant {MerchantId}", merchantId);
                return StatusCode(500, ApiErrorResponse.InternalServerError());
            }
        }

        /// <summary>
        /// Validates the actual credential values against the schema
        /// </summary>
        private (bool IsValid, List<string> Errors) ValidateCredentials(object credentials)
        {
            var errors = new List<string>();

            try
            {
                // Convert credentials to JsonDocument for validation
                var credentialsJson = JsonSerializer.Serialize(credentials);
                var credentialsDoc = JsonDocument.Parse(credentialsJson);

                // Validate credentials object size and content
                var validationResult = _credentialValidationService.ValidateCredentialsObject(
                    credentialsDoc, 
                    _validationSettings.MaxCredentialsObjectSize,
                    _validationSettings.MaxCredentialValueLength,
                    _validationSettings.MinCredentialValueLength
                );

                if (!validationResult.IsValid)
                {
                    errors.AddRange(validationResult.Errors);
                }

                return (errors.Count == 0, errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials");
                errors.Add($"Error validating credentials: {ex.Message}");
                return (false, errors);
            }
        }

        /// <summary>
        /// Get all surcharge providers for the specified merchant
        /// </summary>
        /// <param name="merchantId">Merchant ID</param>
        /// <param name="includeDeleted">Optional: Include deleted providers in the response (default: false)</param>
        /// <returns>List of surcharge providers</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllProviders(string merchantId, [FromQuery] bool includeDeleted = false)
        {
            try
            {
                _logger.LogInformation("Getting surcharge providers for merchant: {MerchantId} (includeDeleted: {IncludeDeleted})", merchantId, includeDeleted);
                
                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                }

                // Get all providers created by this merchant (with optional includeDeleted parameter)
                var providers = await _surchargeProviderService.GetByMerchantIdAsync(merchantId, includeDeleted);
                return Ok(providers.ToResponse());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while getting providers for merchant {MerchantId}", merchantId);
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND),
                    SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND,
                    ex.Message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surcharge providers for merchant: {MerchantId}", merchantId);
                return StatusCode(500, ApiErrorResponse.InternalServerError());
            }
        }

        /// <summary>
        /// Get a surcharge provider by ID for the specified merchant
        /// </summary>
        /// <param name="merchantId">Merchant ID</param>
        /// <param name="id">Provider ID</param>
        /// <returns>Surcharge provider details</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProviderById(string merchantId, Guid id)
        {
            try
            {
                _logger.LogInformation("Getting surcharge provider by ID: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                
                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                }

                // Get provider and verify it was created by this merchant
                var provider = await _surchargeProviderService.GetByIdAsync(id);
                if (provider == null)
                {
                    return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
                }

                // Verify the provider was created by this merchant
                if (provider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized access attempt: Merchant {MerchantId} tried to access provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, provider.CreatedBy);
                    return StatusCode(403, ApiErrorResponse.UnauthorizedAccess());
                }

                return Ok(provider.ToResponse());
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Provider not found: {ProviderId} for merchant {MerchantId}", id, merchantId);
                return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while getting provider {ProviderId} for merchant {MerchantId}", id, merchantId);
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND),
                    SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND,
                    ex.Message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surcharge provider by ID: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                return StatusCode(500, ApiErrorResponse.InternalServerError());
            }
        }

        /// <summary>
        /// Update a surcharge provider for the specified merchant
        /// </summary>
        /// <param name="merchantId">Merchant ID</param>
        /// <param name="id">Provider ID</param>
        /// <param name="request">Surcharge provider update request</param>
        /// <returns>Updated surcharge provider</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProvider(string merchantId, Guid id, [FromBody] SurchargeProviderUpdateRequest request)
        {
            try
            {
                // Log the request with masked sensitive data
                var maskedRequest = SensitiveDataMasker.MaskSensitiveData(request);
                _logger.LogInformation("Updating surcharge provider: {ProviderId} for merchant: {MerchantId}. Request:\n{MaskedRequest}", 
                    id, merchantId, maskedRequest);

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                }

                // Get the existing provider and verify it was created by this merchant
                var existingProvider = await _surchargeProviderService.GetByIdAsync(id);
                if (existingProvider == null)
                {
                    return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
                }

                // Verify the provider was created by this merchant
                if (existingProvider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized update attempt: Merchant {MerchantId} tried to update provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, existingProvider.CreatedBy);
                    return StatusCode(403, ApiErrorResponse.UnauthorizedAccess());
                }

                // Validate the credentials schema structure only if provided
                if (request.CredentialsSchema != null)
                {
                    _logger.LogDebug("CredentialsSchema is not null, validating...");
                    if (!request.ValidateCredentialsSchema(out var schemaErrors, _validationSettings))
                    {
                        _logger.LogWarning("Credentials schema validation failed: {Errors}", string.Join(", ", schemaErrors));
                        return BadRequest(ApiErrorResponse.InvalidCredentialsSchema(schemaErrors));
                    }
                    _logger.LogDebug("Credentials schema validation passed");
                }
                else
                {
                    _logger.LogDebug("CredentialsSchema is null, skipping validation");
                }

                // Validate status code format if provided
                if (!string.IsNullOrEmpty(request.StatusCode))
                {
                    var validStatusCodes = new[] { "ACTIVE", "INACTIVE", "DELETED", "SUSPENDED" };
                    if (!validStatusCodes.Contains(request.StatusCode.ToUpperInvariant()))
                    {
                        return BadRequest(ApiErrorResponse.InvalidStatusCode(request.StatusCode));
                    }
                }

                // Get the status ID from the code (default to ACTIVE if not provided)
                var statusCode = request.StatusCode ?? "ACTIVE";
                var status = await _surchargeProviderService.GetStatusByCodeAsync(statusCode);
                if (status == null)
                {
                    return BadRequest(ApiErrorResponse.InvalidStatusCode(statusCode));
                }

                // Update the provider
                existingProvider.Name = request.Name;
                existingProvider.Code = request.Code;
                existingProvider.Description = request.Description;
                existingProvider.BaseUrl = request.BaseUrl;
                existingProvider.AuthenticationType = request.AuthenticationType;
                
                // Only update credentials schema if provided
                if (request.CredentialsSchema != null)
                {
                    var credentialsSchema = JsonSerializer.SerializeToDocument(request.CredentialsSchema);
                    existingProvider.CredentialsSchema = credentialsSchema;
                }
                // If not provided, keep existing schema unchanged
                
                existingProvider.StatusId = status.StatusId;
                existingProvider.UpdatedBy = merchantId;

                var result = await _surchargeProviderService.UpdateAsync(existingProvider);
                
                // Log successful update
                _logger.LogInformation("Successfully updated surcharge provider: {ProviderId} ({ProviderName}) for merchant: {MerchantId}", 
                    result.Id, result.Name, merchantId);

                return Ok(result.ToResponse());
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Provider not found while updating: {ProviderId} for merchant {MerchantId}", id, merchantId);
                return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating provider {ProviderId} for merchant {MerchantId}", id, merchantId);
                if (ex.Message.Contains("already exists"))
                {
                    return BadRequest(ApiErrorResponse.ProviderCodeExists(request.Code));
                }
                else if (ex.Message.Contains("Invalid credentials schema"))
                {
                    return BadRequest(ApiErrorResponse.InvalidCredentialsSchema(new List<string> { ex.Message }));
                }
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Validation.INVALID_DATA),
                    SurchargeErrorCodes.Validation.INVALID_DATA,
                    ex.Message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                return StatusCode(500, ApiErrorResponse.InternalServerError());
            }
        }

        /// <summary>
        /// Delete (soft delete) a surcharge provider for the specified merchant
        /// </summary>
        /// <param name="merchantId">Merchant ID</param>
        /// <param name="id">Provider ID</param>
        /// <returns>Deleted surcharge provider</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProvider(string merchantId, Guid id)
        {
            try
            {
                _logger.LogInformation("Soft deleting surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                }

                // Get the existing provider and verify it was created by this merchant
                var existingProvider = await _surchargeProviderService.GetByIdAsync(id);
                if (existingProvider == null)
                {
                    return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
                }

                // Verify the provider was created by this merchant
                if (existingProvider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized delete attempt: Merchant {MerchantId} tried to delete provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, existingProvider.CreatedBy);
                    return StatusCode(403, ApiErrorResponse.UnauthorizedAccess());
                }

                // Check if provider is already deleted
                if (existingProvider.Status != null && existingProvider.Status.Code == "DELETED")
                {
                    return BadRequest(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND),
                        SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND,
                        "Provider is already deleted"
                    ));
                }

                var success = await _surchargeProviderService.SoftDeleteAsync(id, merchantId);
                if (!success)
                {
                    return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
                }

                // Get the updated provider with DELETED status to return in response
                // Use includeDeleted: true to get the deleted provider
                var deletedProvider = await _surchargeProviderService.GetByIdAsync(id, includeDeleted: true);
                if (deletedProvider == null)
                {
                    return Ok(new { success = true, message = "Provider soft deleted successfully" });
                }

                // Debug: Log the status we're getting back
                _logger.LogDebug("Retrieved deleted provider {ProviderId} with status: {Status}", 
                    id, deletedProvider.Status?.Code ?? "NULL");

                // Verify the status is actually DELETED
                if (deletedProvider.Status?.Code != "DELETED")
                {
                    _logger.LogWarning("Provider {ProviderId} was soft deleted but status is {Status}, not DELETED", 
                        id, deletedProvider.Status?.Code ?? "NULL");
                }

                return Ok(deletedProvider.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                return StatusCode(500, ApiErrorResponse.InternalServerError());
            }
        }

        /// <summary>
        /// Restore a soft-deleted surcharge provider for the specified merchant
        /// </summary>
        /// <param name="merchantId">Merchant ID</param>
        /// <param name="id">Provider ID</param>
        /// <returns>Restored surcharge provider</returns>
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreProvider(string merchantId, Guid id)
        {
            try
            {
                _logger.LogInformation("Restoring surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                }

                // Get the existing provider (including deleted ones) and verify it was created by this merchant
                var existingProvider = await _surchargeProviderService.GetByIdAsync(id, includeDeleted: true);
                if (existingProvider == null)
                {
                    return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
                }

                // Verify the provider was created by this merchant
                if (existingProvider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized restore attempt: Merchant {MerchantId} tried to restore provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, existingProvider.CreatedBy);
                    return StatusCode(403, ApiErrorResponse.UnauthorizedAccess());
                }

                // Check if provider is actually deleted
                if (existingProvider.Status?.Code != "DELETED")
                {
                    return BadRequest(new ApiErrorResponse(
                        SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND),
                        SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND,
                        "Provider is not deleted and cannot be restored"
                    ));
                }

                var success = await _surchargeProviderService.RestoreAsync(id, merchantId);
                if (!success)
                {
                    return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
                }

                // Get the updated provider to return in response
                var restoredProvider = await _surchargeProviderService.GetByIdAsync(id);
                if (restoredProvider == null)
                {
                    return Ok(new { success = true, message = "Provider restored successfully" });
                }

                return Ok(restoredProvider.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                return StatusCode(500, ApiErrorResponse.InternalServerError());
            }
        }
    }
} 