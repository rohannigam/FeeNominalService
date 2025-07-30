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
    // This controller uses SecureCredentialsSchema wrapper for secure handling of credentials schema data
    // Enhanced security: Uses SecureString and proper disposal to prevent memory dumps and exposure
    [ApiController]
    [Route("api/v1/merchants/{merchantId}/surcharge-providers")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "ApiKeyAccess")]
    public class SurchargeProviderController : ControllerBase
    {
        private readonly ISurchargeProviderService _surchargeProviderService;
        private readonly ISurchargeProviderConfigService _surchargeProviderConfigService;
        private readonly ICredentialValidationService _credentialValidationService;
        private readonly SurchargeProviderValidationSettings _validationSettings;
        private readonly ILogger<SurchargeProviderController> _logger;
        private readonly IAuditService _auditService;

        public SurchargeProviderController(
            ISurchargeProviderService surchargeProviderService,
            ISurchargeProviderConfigService surchargeProviderConfigService,
            ICredentialValidationService credentialValidationService,
            SurchargeProviderValidationSettings validationSettings,
            ILogger<SurchargeProviderController> logger,
            IAuditService auditService)
        {
            _surchargeProviderService = surchargeProviderService;
            _surchargeProviderConfigService = surchargeProviderConfigService;
            _credentialValidationService = credentialValidationService;
            _validationSettings = validationSettings;
            _logger = logger;
            _auditService = auditService;
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
                // Log the request with masked sensitive data (excluding CredentialsSchema)
                var requestForLogging = new {
                    request.Name,
                    request.Code,
                    request.Description,
                    request.BaseUrl,
                    request.AuthenticationType,
                    // request.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    Configuration = request.Configuration != null ? new {
                        request.Configuration.ConfigName,
                        request.Configuration.IsPrimary,
                        request.Configuration.Timeout,
                        request.Configuration.RetryCount,
                        request.Configuration.RetryDelay,
                        request.Configuration.RateLimit,
                        request.Configuration.RateLimitPeriod,
                        // request.Configuration.Credentials, // Excluded for security - contains sensitive credential information
                        request.Configuration.Metadata
                    } : null
                };
                var maskedRequest = SensitiveDataMasker.MaskSensitiveData(requestForLogging);
                
                _logger.LogInformation("Creating surcharge provider for merchant: {MerchantId}. Request:\n{MaskedRequest}", 
                    LogSanitizer.SanitizeMerchantId(merchantId), maskedRequest);

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeMerchantId(authenticatedMerchantId));
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

                // Advanced BaseUrl validation
                if (!UrlSecurityValidator.IsValidBaseUrl(request.BaseUrl, out var baseUrlError))
                {
                    return BadRequest(ApiErrorResponse.InvalidConfiguration(new List<string> { baseUrlError }));
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

                // Convert the credentials schema to SecureCredentialsSchema for secure processing
                using var secureCredentialsSchema = SecureCredentialsSchema.FromJsonDocument(JsonSerializer.SerializeToDocument(request.CredentialsSchema));

                var provider = new SurchargeProvider
                {
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    BaseUrl = request.BaseUrl,
                    AuthenticationType = request.AuthenticationType,
                    CredentialsSchema = JsonDocument.Parse("{}"), // Redacted/empty value for security
                    StatusId = status.StatusId,
                    CreatedBy = merchantId,
                    UpdatedBy = merchantId
                };

                SurchargeProvider result;

                // If configuration is provided, create both provider and configuration
                if (request.Configuration != null)
                {
                    // Handle configuration credentials securely before passing to service
                    if (request.Configuration.Credentials != null)
                    {
                        using var secureConfigCredentials = SecureCredentials.FromJsonDocument(JsonSerializer.SerializeToDocument(request.Configuration.Credentials));
                        _logger.LogInformation("Creating provider with secure configuration credentials for merchant {MerchantId}", 
                            LogSanitizer.SanitizeMerchantId(merchantId));
                    }
                    
                    // Pass secureCredentialsSchema to the service
                    result = await _surchargeProviderService.CreateWithConfigurationAsync(provider, request.Configuration, merchantId, secureCredentialsSchema);
                }
                else
                {
                    // Otherwise, just create the provider
                    // Pass secureCredentialsSchema to the service
                    result = await _surchargeProviderService.CreateAsync(provider, secureCredentialsSchema);
                }

                // Audit log: provider creation (excluding sensitive credentials schema)
                var auditProvider = new {
                    result.Id,
                    result.Name,
                    result.Code,
                    result.Description,
                    result.BaseUrl,
                    result.AuthenticationType,
                    // result.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    result.StatusId,
                    result.CreatedBy,
                    result.UpdatedBy,
                    result.CreatedAt,
                    result.UpdatedAt
                };
                await _auditService.LogAuditAsync(
                    entityType: "SurchargeProvider",
                    entityId: result.Id,
                    action: "Create",
                    userId: merchantId,
                    fieldChanges: new Dictionary<string, (string? OldValue, string? NewValue)>
                    {
                        { "FullObject", (null, JsonSerializer.Serialize(auditProvider)) },
                        { "CredentialsSchema", (null, "[REDACTED - Contains sensitive credential information]") }
                    }
                );

                // Log successful creation
                _logger.LogInformation("Successfully created surcharge provider: {ProviderId} ({ProviderName}) for merchant: {MerchantId}", 
                    LogSanitizer.SanitizeGuid(result.Id), LogSanitizer.SanitizeString(result.Name), LogSanitizer.SanitizeMerchantId(merchantId));

                return Ok(result.ToResponse());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while creating provider for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                
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
                _logger.LogWarning(ex, "Key not found while creating provider for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND),
                    SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND,
                    ex.Message
                ));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument while creating provider for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Validation.INVALID_DATA),
                    SurchargeErrorCodes.Validation.INVALID_DATA,
                    ex.Message
                ));
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "JSON parsing error while creating provider for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
                return BadRequest(new ApiErrorResponse(
                    SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Validation.INVALID_DATA),
                    SurchargeErrorCodes.Validation.INVALID_DATA,
                    ex.Message
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating surcharge provider for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
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
                // Allow admin-scope keys to read any merchant's providers
                var scopeClaim = User.FindFirst("Scope")?.Value;
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (scopeClaim != "admin")
                {
                    // For merchants, enforce merchantId claim check
                    if (string.IsNullOrEmpty(authenticatedMerchantId))
                    {
                        return BadRequest(new { message = "Merchant ID not found in claims" });
                    }
                    if (authenticatedMerchantId != merchantId)
                    {
                        _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeMerchantId(authenticatedMerchantId));
                        return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                    }
                }
                var providers = await _surchargeProviderService.GetByMerchantIdAsync(merchantId, includeDeleted);
                return Ok(providers.Select(p => p.ToResponse()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers for merchant {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantId));
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
                // Allow admin-scope keys to read any merchant's provider
                var scopeClaim = User.FindFirst("Scope")?.Value;
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (scopeClaim != "admin")
                {
                    // For merchants, enforce merchantId claim check
                    if (string.IsNullOrEmpty(authenticatedMerchantId))
                    {
                        return BadRequest(new { message = "Merchant ID not found in claims" });
                    }
                    if (authenticatedMerchantId != merchantId)
                    {
                        _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeMerchantId(authenticatedMerchantId));
                        return StatusCode(403, ApiErrorResponse.MerchantIdMismatch());
                    }
                }
                var provider = await _surchargeProviderService.GetByIdAsync(id);
                if (provider == null)
                {
                    return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
                }
                return Ok(provider.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID {ProviderId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));
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
                // Log the request with masked sensitive data (excluding CredentialsSchema)
                var requestForLogging = new {
                    request.Name,
                    request.Code,
                    request.Description,
                    request.BaseUrl,
                    request.AuthenticationType,
                    // request.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    request.StatusCode
                };
                var maskedRequest = SensitiveDataMasker.MaskSensitiveData(requestForLogging);
                _logger.LogInformation("Updating surcharge provider: {ProviderId} for merchant: {MerchantId}. Request:\n{MaskedRequest}", 
                    LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId), maskedRequest);

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeMerchantId(authenticatedMerchantId));
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
                        LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(existingProvider.CreatedBy));
                    return StatusCode(403, ApiErrorResponse.UnauthorizedAccess());
                }

                // Validate the credentials schema structure only if provided
                if (request.CredentialsSchema != null)
                {
                    _logger.LogDebug("CredentialsSchema is not null, validating...");
                    if (!request.ValidateCredentialsSchema(out var schemaErrors, _validationSettings))
                    {
                        _logger.LogWarning("Credentials schema validation failed: {Errors}", LogSanitizer.SanitizeString(string.Join(", ", schemaErrors)));
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

                // Advanced BaseUrl validation for update
                if (!UrlSecurityValidator.IsValidBaseUrl(request.BaseUrl, out var updateBaseUrlError))
                {
                    return BadRequest(ApiErrorResponse.InvalidConfiguration(new List<string> { updateBaseUrlError }));
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
                    // This method uses SecureCredentialsSchema wrapper for secure handling
                    // Enhanced security: Uses SecureString and proper disposal to prevent memory dumps
                    using var secureCredentialsSchema = SecureCredentialsSchema.FromJsonDocument(JsonSerializer.SerializeToDocument(request.CredentialsSchema));
                    var updateCredentialsSchema = JsonSerializer.SerializeToDocument(request.CredentialsSchema);
                    using var secureUpdateCredentialsSchema = SecureCredentialsSchema.FromJsonDocument(updateCredentialsSchema);
                    existingProvider.CredentialsSchema = updateCredentialsSchema;
                }
                // If not provided, keep existing schema unchanged
                
                existingProvider.StatusId = status.StatusId;
                existingProvider.UpdatedBy = merchantId;

                var result = await _surchargeProviderService.UpdateAsync(existingProvider);

                // Audit log: provider update
                var oldProviderAudit = new {
                    existingProvider.Id,
                    existingProvider.Name,
                    existingProvider.Code,
                    existingProvider.Description,
                    existingProvider.BaseUrl,
                    existingProvider.AuthenticationType,
                    // existingProvider.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    existingProvider.StatusId,
                    existingProvider.CreatedBy,
                    existingProvider.UpdatedBy,
                    existingProvider.CreatedAt,
                    existingProvider.UpdatedAt
                };
                var updatedProviderAudit = new {
                    result.Id,
                    result.Name,
                    result.Code,
                    result.Description,
                    result.BaseUrl,
                    result.AuthenticationType,
                    // result.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    result.StatusId,
                    result.CreatedBy,
                    result.UpdatedBy,
                    result.CreatedAt,
                    result.UpdatedAt
                };
                await _auditService.LogAuditAsync(
                    entityType: "SurchargeProvider",
                    entityId: result.Id,
                    action: "Update",
                    userId: merchantId,
                    fieldChanges: new Dictionary<string, (string? OldValue, string? NewValue)>
                    {
                        { "FullObject", (JsonSerializer.Serialize(oldProviderAudit), JsonSerializer.Serialize(updatedProviderAudit)) },
                        { "CredentialsSchema", ("[REDACTED - Contains sensitive credential information]", "[REDACTED - Contains sensitive credential information]") }
                    }
                );

                // If a new configuration is provided, update or create the config as appropriate
                SurchargeProviderConfig? updatedOrNewConfig = null;
                if (request.Configuration != null)
                {
                    // Validate configuration
                    if (!request.ValidateConfiguration(out var configErrors, _validationSettings))
                    {
                        return BadRequest(ApiErrorResponse.InvalidConfiguration(configErrors));
                    }

                    // Validate credentials if provided
                    if (request.Configuration.Credentials != null)
                    {
                        var credentialsValidation = ValidateCredentials(request.Configuration.Credentials);
                        if (!credentialsValidation.IsValid)
                        {
                            return BadRequest(ApiErrorResponse.InvalidCredentials(credentialsValidation.Errors));
                        }
                    }

                    // Check for an existing active config for this provider
                    var configs = await _surchargeProviderConfigService.GetByProviderIdAsync(id);
                    var activeConfig = configs.FirstOrDefault(c => c.IsActive);

                    if (activeConfig != null)
                    {
                        // Update the existing active config
                        bool changed = false;
                        if (activeConfig.ConfigName != request.Configuration.ConfigName) { activeConfig.ConfigName = request.Configuration.ConfigName; changed = true; }
                        if (activeConfig.IsPrimary != request.Configuration.IsPrimary) { activeConfig.IsPrimary = request.Configuration.IsPrimary; changed = true; }
                        if (activeConfig.Timeout != request.Configuration.Timeout) { activeConfig.Timeout = request.Configuration.Timeout; changed = true; }
                        if (activeConfig.RetryCount != request.Configuration.RetryCount) { activeConfig.RetryCount = request.Configuration.RetryCount; changed = true; }
                        if (activeConfig.RetryDelay != request.Configuration.RetryDelay) { activeConfig.RetryDelay = request.Configuration.RetryDelay; changed = true; }
                        if (activeConfig.RateLimit != request.Configuration.RateLimit) { activeConfig.RateLimit = request.Configuration.RateLimit; changed = true; }
                        if (activeConfig.RateLimitPeriod != request.Configuration.RateLimitPeriod) { activeConfig.RateLimitPeriod = request.Configuration.RateLimitPeriod; changed = true; }
                        if (request.Configuration.Metadata != null) {
                            var newMeta = JsonSerializer.SerializeToDocument(request.Configuration.Metadata);
                            if (!JsonSerializer.Serialize(activeConfig.Metadata).Equals(JsonSerializer.Serialize(newMeta))) {
                                activeConfig.Metadata = newMeta; changed = true;
                            }
                        }
                        if (request.Configuration.Credentials != null) {
                            using var secureNewCredentials = SecureCredentials.FromJsonDocument(JsonSerializer.SerializeToDocument(request.Configuration.Credentials));
                            using var secureStoredCredentials = SecureCredentials.FromJsonDocument(activeConfig.Credentials);
                            
                            var credentialsChanged = secureNewCredentials.ProcessCredentialsSecurely(newCreds => 
                                secureStoredCredentials.ProcessCredentialsSecurely(storedCreds => {
                                    return !JsonSerializer.Serialize(newCreds).Equals(JsonSerializer.Serialize(storedCreds));
                                })
                            );
                            
                            if (credentialsChanged) {
                                var newCredentials = secureNewCredentials.GetCredentials();
                                if (newCredentials != null)
                                {
                                    activeConfig.Credentials = newCredentials;
                                    changed = true;
                                }
                            }
                        }
                        if (changed) {
                            activeConfig.UpdatedAt = DateTime.UtcNow;
                            activeConfig.UpdatedBy = merchantId;
                            updatedOrNewConfig = await _surchargeProviderConfigService.UpdateAsync(activeConfig, merchantId);
                        } else {
                            updatedOrNewConfig = activeConfig;
                        }
                    }
                    else
                    {
                        // No active config exists, create a new one and mark as active
                        // Handle credentials securely
                        JsonDocument? secureCredentials = null;
                        if (request.Configuration.Credentials != null)
                        {
                            using var secureCredentialsWrapper = SecureCredentials.FromJsonDocument(JsonSerializer.SerializeToDocument(request.Configuration.Credentials));
                            secureCredentials = secureCredentialsWrapper.GetCredentials();
                        }

                        var config = new SurchargeProviderConfig
                        {
                            MerchantId = Guid.Parse(merchantId),
                            ProviderId = id,
                            ConfigName = request.Configuration.ConfigName,
                            Credentials = secureCredentials ?? JsonSerializer.SerializeToDocument(request.Configuration.Credentials),
                            IsActive = true,
                            IsPrimary = request.Configuration.IsPrimary,
                            Timeout = request.Configuration.Timeout,
                            RetryCount = request.Configuration.RetryCount,
                            RetryDelay = request.Configuration.RetryDelay,
                            RateLimit = request.Configuration.RateLimit,
                            RateLimitPeriod = request.Configuration.RateLimitPeriod,
                            Metadata = request.Configuration.Metadata != null ? JsonSerializer.SerializeToDocument(request.Configuration.Metadata) : null,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            CreatedBy = merchantId,
                            UpdatedBy = merchantId
                        };
                        updatedOrNewConfig = await _surchargeProviderConfigService.CreateAsync(config, merchantId);
                    }
                }

                // Log successful update
                _logger.LogInformation("Successfully updated surcharge provider: {ProviderId} ({ProviderName}) for merchant: {MerchantId}", 
                    LogSanitizer.SanitizeGuid(result.Id), LogSanitizer.SanitizeString(result.Name), LogSanitizer.SanitizeMerchantId(merchantId));

                // Return both the updated provider and updated/new config (if any)
                return Ok(new { Provider = result.ToResponse(), Config = updatedOrNewConfig });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Provider not found while updating: {ProviderId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));
                return NotFound(ApiErrorResponse.ProviderNotFound(id.ToString()));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while updating provider {ProviderId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));
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
                _logger.LogError(ex, "Error updating surcharge provider: {ProviderId} for merchant: {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));
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
                _logger.LogInformation("Soft deleting surcharge provider: {ProviderId} for merchant: {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeMerchantId(authenticatedMerchantId));
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
                        LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(existingProvider.CreatedBy));
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

                // Audit log: provider deletion (excluding sensitive credentials schema)
                var deletedProviderAudit = new {
                    existingProvider.Id,
                    existingProvider.Name,
                    existingProvider.Code,
                    existingProvider.Description,
                    existingProvider.BaseUrl,
                    existingProvider.AuthenticationType,
                    // existingProvider.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    existingProvider.StatusId,
                    existingProvider.CreatedBy,
                    existingProvider.UpdatedBy,
                    existingProvider.CreatedAt,
                    existingProvider.UpdatedAt
                };
                await _auditService.LogAuditAsync(
                    entityType: "SurchargeProvider",
                    entityId: id,
                    action: "Delete",
                    userId: merchantId,
                    fieldChanges: new Dictionary<string, (string? OldValue, string? NewValue)>
                    {
                        { "FullObject", (JsonSerializer.Serialize(deletedProviderAudit), null) },
                        { "CredentialsSchema", ("[REDACTED - Contains sensitive credential information]", null) }
                    }
                );

                // Get the updated provider with DELETED status to return in response
                // Use includeDeleted: true to get the deleted provider
                var deletedProvider = await _surchargeProviderService.GetByIdAsync(id, includeDeleted: true);
                if (deletedProvider == null)
                {
                    return Ok(new { success = true, message = "Provider soft deleted successfully" });
                }

                // Debug: Log the status we're getting back
                _logger.LogDebug("Retrieved deleted provider {ProviderId} with status: {Status}", 
                    LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(deletedProvider.Status?.Code ?? "NULL"));

                // Verify the status is actually DELETED
                if (deletedProvider.Status?.Code != "DELETED")
                {
                    _logger.LogWarning("Provider {ProviderId} was soft deleted but status is {Status}, not DELETED", 
                        LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(deletedProvider.Status?.Code ?? "NULL"));
                }

                return Ok(deletedProvider.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting surcharge provider: {ProviderId} for merchant: {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));
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
                _logger.LogInformation("Restoring surcharge provider: {ProviderId} for merchant: {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));

                // Validate merchant ID from URL matches authenticated merchant
                var authenticatedMerchantId = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(authenticatedMerchantId))
                {
                    return BadRequest(ApiErrorResponse.MerchantIdMismatch());
                }

                if (authenticatedMerchantId != merchantId)
                {
                    _logger.LogWarning("Merchant ID mismatch: URL {UrlMerchantId} vs authenticated {AuthMerchantId}", 
                        LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeMerchantId(authenticatedMerchantId));
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
                        LogSanitizer.SanitizeMerchantId(merchantId), LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(existingProvider.CreatedBy));
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

                // Audit log: provider restoration
                var restoredProviderResult = await _surchargeProviderService.GetByIdAsync(id);
                if (restoredProviderResult == null)
                {
                    return Ok(new { success = true, message = "Provider restored successfully" });
                }
                var restoredProviderAuditOld = new {
                    existingProvider.Id,
                    existingProvider.Name,
                    existingProvider.Code,
                    existingProvider.Description,
                    existingProvider.BaseUrl,
                    existingProvider.AuthenticationType,
                    // existingProvider.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    existingProvider.StatusId,
                    existingProvider.CreatedBy,
                    existingProvider.UpdatedBy,
                    existingProvider.CreatedAt,
                    existingProvider.UpdatedAt
                };
                var restoredProviderAuditNew = new {
                    restoredProviderResult.Id,
                    restoredProviderResult.Name,
                    restoredProviderResult.Code,
                    restoredProviderResult.Description,
                    restoredProviderResult.BaseUrl,
                    restoredProviderResult.AuthenticationType,
                    // restoredProviderResult.CredentialsSchema, // Excluded for security - contains sensitive credential information
                    restoredProviderResult.StatusId,
                    restoredProviderResult.CreatedBy,
                    restoredProviderResult.UpdatedBy,
                    restoredProviderResult.CreatedAt,
                    restoredProviderResult.UpdatedAt
                };
                await _auditService.LogAuditAsync(
                    entityType: "SurchargeProvider",
                    entityId: id,
                    action: "Restore",
                    userId: merchantId,
                    fieldChanges: new Dictionary<string, (string? OldValue, string? NewValue)>
                    {
                        { "FullObject", (JsonSerializer.Serialize(restoredProviderAuditOld), JsonSerializer.Serialize(restoredProviderAuditNew)) },
                        { "CredentialsSchema", ("[REDACTED - Contains sensitive credential information]", "[REDACTED - Contains sensitive credential information]") }
                    }
                );
                return Ok(restoredProviderResult.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring surcharge provider: {ProviderId} for merchant: {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeMerchantId(merchantId));
                return StatusCode(500, ApiErrorResponse.InternalServerError());
            }
        }
    }
} 
