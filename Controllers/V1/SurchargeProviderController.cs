using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Security.Claims;

namespace FeeNominalService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/surcharge/providers")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "ApiKeyAccess")]
    public class SurchargeProviderController : ControllerBase
    {
        private readonly ISurchargeProviderService _surchargeProviderService;
        private readonly ILogger<SurchargeProviderController> _logger;

        public SurchargeProviderController(
            ISurchargeProviderService surchargeProviderService,
            ILogger<SurchargeProviderController> logger)
        {
            _surchargeProviderService = surchargeProviderService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProvider([FromBody] SurchargeProviderRequest request)
        {
            try
            {
                _logger.LogInformation("Creating new surcharge provider: {ProviderName}", request.Name);
                
                // Get the status ID from the code
                var status = await _surchargeProviderService.GetStatusByCodeAsync(request.StatusCode);
                if (status == null)
                {
                    return BadRequest(new { message = $"Invalid status code: {request.StatusCode}" });
                }

                // Get the merchant ID from claims
                var merchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantId))
                {
                    return BadRequest(new { message = "Merchant ID not found in claims" });
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

                var result = await _surchargeProviderService.CreateAsync(provider);
                return Ok(result);
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

        [HttpGet]
        public async Task<IActionResult> GetAllProviders()
        {
            try
            {
                _logger.LogInformation("Getting all surcharge providers");
                var providers = await _surchargeProviderService.GetAllAsync();
                return Ok(providers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all surcharge providers");
                return StatusCode(500, new { message = "An error occurred while retrieving surcharge providers" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProviderById(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting surcharge provider by ID: {ProviderId}", id);
                var provider = await _surchargeProviderService.GetByIdAsync(id);
                
                if (provider == null)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                return Ok(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surcharge provider by ID: {ProviderId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the surcharge provider" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProvider(Guid id, [FromBody] SurchargeProviderRequest request)
        {
            try
            {
                _logger.LogInformation("Updating surcharge provider: {ProviderId}", id);

                // Get the existing provider
                var existingProvider = await _surchargeProviderService.GetByIdAsync(id);
                if (existingProvider == null)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                // Get the status ID from the code
                var status = await _surchargeProviderService.GetStatusByCodeAsync(request.StatusCode);
                if (status == null)
                {
                    return BadRequest(new { message = $"Invalid status code: {request.StatusCode}" });
                }

                // Get the merchant ID from claims
                var merchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantId))
                {
                    return BadRequest(new { message = "Merchant ID not found in claims" });
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
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Provider not found: {ProviderId}", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating provider: {ProviderId}", id);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating surcharge provider: {ProviderId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the surcharge provider" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProvider(Guid id)
        {
            try
            {
                _logger.LogInformation("Deleting surcharge provider: {ProviderId}", id);
                var result = await _surchargeProviderService.DeleteAsync(id);
                
                if (!result)
                {
                    return NotFound(new { message = $"Provider with ID {id} not found" });
                }

                return Ok(new { message = "Provider deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting surcharge provider: {ProviderId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the surcharge provider" });
            }
        }
    }
} 