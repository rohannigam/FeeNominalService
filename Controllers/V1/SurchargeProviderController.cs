using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Services;
using FeeNominalService.Settings;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

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

        [HttpPost]
        public async Task<IActionResult> CreateProvider(string merchantId, [FromBody] SurchargeProviderRequest request)
        {
            try
            {
                _logger.LogInformation("Creating surcharge provider for merchant: {MerchantId}", merchantId);

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
                    return Forbid("Merchant ID in URL does not match authenticated merchant");
                }

                // Validate the credentials schema structure with enhanced validation
                if (!request.ValidateCredentialsSchema(out var schemaErrors, _validationSettings))
                {
                    return BadRequest(new { 
                        message = "Invalid credentials schema", 
                        errors = schemaErrors 
                    });
                }

                // Validate configuration if provided with enhanced validation
                if (!request.ValidateConfiguration(out var configErrors, _validationSettings))
                {
                    return BadRequest(new { 
                        message = "Invalid configuration", 
                        errors = configErrors 
                    });
                }

                // Validate actual credentials if configuration is provided
                if (request.Configuration?.Credentials != null)
                {
                    var credentialsValidation = ValidateCredentials(request.Configuration.Credentials);
                    if (!credentialsValidation.IsValid)
                    {
                        return BadRequest(new { 
                            message = "Invalid credentials", 
                            errors = credentialsValidation.Errors 
                        });
                    }
                }
                
                // Get the status ID from the code (default to ACTIVE if not provided)
                var statusCode = request.StatusCode ?? "ACTIVE";
                var status = await _surchargeProviderService.GetStatusByCodeAsync(statusCode);
                if (status == null)
                {
                    return BadRequest(new { message = $"Invalid status code: {statusCode}" });
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

                return Ok(result.ToResponse());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating provider");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating surcharge provider");
                return StatusCode(500, new { message = "An error occurred while creating the surcharge provider" });
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

        [HttpGet]
        public async Task<IActionResult> GetAllProviders(string merchantId)
        {
            try
            {
                _logger.LogInformation("Getting surcharge providers for merchant: {MerchantId}", merchantId);
                
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
                    return Forbid("Merchant ID in URL does not match authenticated merchant");
                }

                // Get all providers created by this merchant
                var providers = await _surchargeProviderService.GetByMerchantIdAsync(merchantId);
                return Ok(providers.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surcharge providers for merchant: {MerchantId}", merchantId);
                return StatusCode(500, new { message = "An error occurred while retrieving surcharge providers" });
            }
        }

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
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return Forbid("Merchant ID in URL does not match authenticated merchant");
                }

                // Get provider and verify it was created by this merchant
                var provider = await _surchargeProviderService.GetByIdAsync(id);
                if (provider == null)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                // Verify the provider was created by this merchant
                if (provider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized access attempt: Merchant {MerchantId} tried to access provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, provider.CreatedBy);
                    return Forbid("You do not have permission to access this provider");
                }

                return Ok(provider.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surcharge provider by ID: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                return StatusCode(500, new { message = "An error occurred while retrieving the surcharge provider" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProvider(string merchantId, Guid id, [FromBody] SurchargeProviderRequest request)
        {
            try
            {
                _logger.LogInformation("Updating surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);

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
                    return Forbid("Merchant ID in URL does not match authenticated merchant");
                }

                // Get the existing provider and verify it was created by this merchant
                var existingProvider = await _surchargeProviderService.GetByIdAsync(id);
                if (existingProvider == null)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                // Verify the provider was created by this merchant
                if (existingProvider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized update attempt: Merchant {MerchantId} tried to update provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, existingProvider.CreatedBy);
                    return Forbid("You do not have permission to update this provider");
                }

                // Validate the credentials schema structure
                if (!request.ValidateCredentialsSchema(out var schemaErrors))
                {
                    return BadRequest(new { 
                        message = "Invalid credentials schema", 
                        errors = schemaErrors 
                    });
                }

                // Get the status ID from the code (default to ACTIVE if not provided)
                var statusCode = request.StatusCode ?? "ACTIVE";
                var status = await _surchargeProviderService.GetStatusByCodeAsync(statusCode);
                if (status == null)
                {
                    return BadRequest(new { message = $"Invalid status code: {statusCode}" });
                }

                // Convert the credentials schema to JsonDocument
                var credentialsSchema = JsonSerializer.SerializeToDocument(request.CredentialsSchema);

                // Update the provider
                existingProvider.Name = request.Name;
                existingProvider.Code = request.Code;
                existingProvider.Description = request.Description;
                existingProvider.BaseUrl = request.BaseUrl;
                existingProvider.AuthenticationType = request.AuthenticationType;
                existingProvider.CredentialsSchema = credentialsSchema;
                existingProvider.StatusId = status.StatusId;
                existingProvider.UpdatedBy = merchantId;

                var result = await _surchargeProviderService.UpdateAsync(existingProvider);
                return Ok(result.ToResponse());
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Provider not found while updating");
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating provider");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                return StatusCode(500, new { message = "An error occurred while updating the surcharge provider" });
            }
        }

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
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return Forbid("Merchant ID in URL does not match authenticated merchant");
                }

                // Get the existing provider and verify it was created by this merchant
                var existingProvider = await _surchargeProviderService.GetByIdAsync(id);
                if (existingProvider == null)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                // Verify the provider was created by this merchant
                if (existingProvider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized delete attempt: Merchant {MerchantId} tried to delete provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, existingProvider.CreatedBy);
                    return Forbid("You do not have permission to delete this provider");
                }

                // Check if provider is already deleted
                if (existingProvider.Status != null && existingProvider.Status.Code == "DELETED")
                {
                    return BadRequest(new { message = "Provider is already deleted" });
                }

                var success = await _surchargeProviderService.SoftDeleteAsync(id, merchantId);
                if (!success)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                // Get the updated provider with DELETED status to return in response
                var deletedProvider = await _surchargeProviderService.GetByIdAsync(id, includeDeleted: true);
                if (deletedProvider == null)
                {
                    return Ok(new { success = true, message = "Provider soft deleted successfully" });
                }

                return Ok(deletedProvider.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting surcharge provider: {ProviderId} for merchant: {MerchantId}", id, merchantId);
                return StatusCode(500, new { message = "An error occurred while soft deleting the surcharge provider" });
            }
        }

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
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        merchantId, authenticatedMerchantId);
                    return Forbid("Merchant ID in URL does not match authenticated merchant");
                }

                // Get the existing provider (including deleted ones) and verify it was created by this merchant
                var existingProvider = await _surchargeProviderService.GetByIdAsync(id, includeDeleted: true);
                if (existingProvider == null)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                // Verify the provider was created by this merchant
                if (existingProvider.CreatedBy != merchantId)
                {
                    _logger.LogWarning("Unauthorized restore attempt: Merchant {MerchantId} tried to restore provider {ProviderId} created by {ProviderCreator}", 
                        merchantId, id, existingProvider.CreatedBy);
                    return Forbid("You do not have permission to restore this provider");
                }

                // Check if provider is actually deleted
                if (existingProvider.Status?.Code != "DELETED")
                {
                    return BadRequest(new { message = "Provider is not deleted and cannot be restored" });
                }

                var success = await _surchargeProviderService.RestoreAsync(id, merchantId);
                if (!success)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                // Get the updated provider with ACTIVE status to return in response
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
                return StatusCode(500, new { message = "An error occurred while restoring the surcharge provider" });
            }
        }
    }
} 