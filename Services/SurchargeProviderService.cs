using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Models.Common;
using FeeNominalService.Repositories;
using Microsoft.Extensions.Logging;
using Json.Schema;
using FeeNominalService.Utils;
using FeeNominalService.Settings;

namespace FeeNominalService.Services
{
    public class SurchargeProviderService : ISurchargeProviderService
    {
        private readonly ISurchargeProviderRepository _repository;
        private readonly ISurchargeProviderConfigService _configService;
        private readonly ILogger<SurchargeProviderService> _logger;
        private readonly SurchargeProviderValidationSettings _validationSettings;

        public SurchargeProviderService(
            ISurchargeProviderRepository repository,
            ISurchargeProviderConfigService configService,
            ILogger<SurchargeProviderService> logger,
            SurchargeProviderValidationSettings validationSettings)
        {
            _repository = repository;
            _configService = configService;
            _logger = logger;
            _validationSettings = validationSettings;
        }

        public async Task<SurchargeProvider?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting provider by ID: {ProviderId}", LogSanitizer.SanitizeGuid(id));
                return await _repository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID: {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<SurchargeProvider?> GetByIdAsync(Guid id, bool includeDeleted)
        {
            try
            {
                _logger.LogInformation("Getting provider by ID: {ProviderId} (includeDeleted: {IncludeDeleted})", LogSanitizer.SanitizeGuid(id), includeDeleted);
                return await _repository.GetByIdAsync(id, includeDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by ID: {ProviderId} (includeDeleted: {IncludeDeleted})", LogSanitizer.SanitizeGuid(id), includeDeleted);
                throw;
            }
        }

        public async Task<SurchargeProvider?> GetByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("Getting provider by code {ProviderCode}", LogSanitizer.SanitizeString(code));
                return await _repository.GetByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting provider by code {ProviderCode}", LogSanitizer.SanitizeString(code));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Getting all providers");
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all providers");
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId)
        {
            try
            {
                _logger.LogInformation("Getting providers for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                return await _repository.GetByMerchantIdAsync(merchantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting providers for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetByMerchantIdAsync(string merchantId, bool includeDeleted)
        {
            try
            {
                _logger.LogInformation("Getting providers for merchant {MerchantId} (includeDeleted: {IncludeDeleted})", LogSanitizer.SanitizeString(merchantId), includeDeleted);
                return await _repository.GetByMerchantIdAsync(merchantId, includeDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting providers for merchant {MerchantId} (includeDeleted: {IncludeDeleted})", LogSanitizer.SanitizeString(merchantId), includeDeleted);
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetConfiguredProvidersByMerchantIdAsync(string merchantId)
        {
            try
            {
                _logger.LogInformation("Getting configured providers for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                return await _repository.GetConfiguredProvidersByMerchantIdAsync(merchantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configured providers for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                throw;
            }
        }

        public async Task<bool> HasConfigurationAsync(string merchantId, Guid providerId)
        {
            try
            {
                _logger.LogInformation("Checking if merchant {MerchantId} has configuration for provider {ProviderId}", LogSanitizer.SanitizeString(merchantId), LogSanitizer.SanitizeGuid(providerId));
                return await _repository.HasConfigurationAsync(merchantId, providerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking configuration for merchant {MerchantId} and provider {ProviderId}", LogSanitizer.SanitizeString(merchantId), LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        public async Task<IEnumerable<SurchargeProvider>> GetActiveAsync()
        {
            try
            {
                _logger.LogInformation("Getting active providers");
                return await _repository.GetActiveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active providers");
                throw;
            }
        }

        public async Task<SurchargeProvider> CreateAsync(SurchargeProvider provider)
        {
            try
            {
                _logger.LogInformation("Creating provider {ProviderName} for merchant {MerchantId}", LogSanitizer.SanitizeString(provider.Name), LogSanitizer.SanitizeString(provider.CreatedBy));

                // Validate provider code uniqueness for this merchant only
                if (await _repository.ExistsByCodeAndMerchantAsync(provider.Code, provider.CreatedBy))
                {
                    throw new InvalidOperationException($"Provider with code {provider.Code} already exists for this merchant");
                }

                // Validate credentials schema
                if (provider.CredentialsSchema == null || string.IsNullOrEmpty(provider.CredentialsSchema.RootElement.GetRawText()))
                {
                    throw new InvalidOperationException("Credentials schema is required and cannot be empty");
                }

                // Validate the schema structure
                var schemaValidation = ValidateCredentialsSchemaStructure(provider.CredentialsSchema);
                if (!schemaValidation.IsValid)
                {
                    throw new InvalidOperationException($"Invalid credentials schema: {string.Join(", ", schemaValidation.Errors)}");
                }

                // Set timestamps
                provider.CreatedAt = DateTime.UtcNow;
                provider.UpdatedAt = DateTime.UtcNow;

                // Get the status ID from the code
                var status = await _repository.GetStatusByCodeAsync("ACTIVE");
                if (status == null)
                {
                    throw new InvalidOperationException("ACTIVE status not found in the database");
                }
                provider.StatusId = status.StatusId;
                provider.Status = status;

                // Set ProviderType if not already set
                if (string.IsNullOrWhiteSpace(provider.ProviderType))
                {
                    provider.ProviderType = "INTERPAYMENTS"; // Or infer from template/logic
                }

                // Use transaction-based validation to prevent race conditions
                return await _repository.AddWithLimitCheckAsync(provider, _validationSettings.MaxProvidersPerMerchant);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider {ProviderName} for merchant {MerchantId}", LogSanitizer.SanitizeString(provider.Name), LogSanitizer.SanitizeString(provider.CreatedBy));
                throw;
            }
        }

        public async Task<SurchargeProvider> CreateWithConfigurationAsync(SurchargeProvider provider, ProviderConfigurationRequest configuration, string merchantId)
        {
            try
            {
                _logger.LogInformation("Creating provider {ProviderName} with configuration for merchant {MerchantId}", LogSanitizer.SanitizeString(provider.Name), LogSanitizer.SanitizeString(merchantId));

                // First, create the provider
                var createdProvider = await CreateAsync(provider);

                // Then create the configuration
                var config = new SurchargeProviderConfig
                {
                    MerchantId = Guid.Parse(merchantId),
                    ProviderId = createdProvider.Id,
                    ConfigName = configuration.ConfigName,
                    Credentials = JsonSerializer.SerializeToDocument(configuration.Credentials),
                    IsActive = true,
                    IsPrimary = configuration.IsPrimary,
                    Timeout = configuration.Timeout,
                    RetryCount = configuration.RetryCount,
                    RetryDelay = configuration.RetryDelay,
                    RateLimit = configuration.RateLimit,
                    RateLimitPeriod = configuration.RateLimitPeriod,
                    Metadata = configuration.Metadata != null ? JsonSerializer.SerializeToDocument(configuration.Metadata) : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = provider.CreatedBy,
                    UpdatedBy = provider.CreatedBy
                };

                // Save the configuration to the database (now with requestor)
                var savedConfig = await _configService.CreateAsync(config, merchantId);

                // Add the configuration to the provider for the response
                createdProvider.Configurations = new List<SurchargeProviderConfig> { savedConfig };

                _logger.LogInformation("Successfully created provider {ProviderId} with configuration {ConfigId} for merchant {MerchantId}", 
                    LogSanitizer.SanitizeGuid(createdProvider.Id), LogSanitizer.SanitizeGuid(savedConfig.Id), LogSanitizer.SanitizeString(merchantId));

                return createdProvider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating provider with configuration for merchant {MerchantId}", LogSanitizer.SanitizeString(merchantId));
                throw;
            }
        }

        public async Task<SurchargeProvider> UpdateAsync(SurchargeProvider provider)
        {
            try
            {
                _logger.LogInformation("Updating provider {ProviderId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(provider.Id), LogSanitizer.SanitizeString(provider.UpdatedBy));

                // Check if provider exists
                var existingProvider = await _repository.GetByIdAsync(provider.Id);
                if (existingProvider == null)
                {
                    throw new KeyNotFoundException($"Provider with ID {provider.Id} not found");
                }

                // Validate provider code uniqueness for this merchant only if changed
                if (provider.Code != existingProvider.Code && await _repository.ExistsByCodeAndMerchantAsync(provider.Code, provider.UpdatedBy))
                {
                    throw new InvalidOperationException($"Provider with code {provider.Code} already exists for this merchant");
                }

                // Update timestamp
                provider.UpdatedAt = DateTime.UtcNow;

                return await _repository.UpdateAsync(provider);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating provider {ProviderId} for merchant {MerchantId}", LogSanitizer.SanitizeGuid(provider.Id), LogSanitizer.SanitizeString(provider.UpdatedBy));
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Soft deleting provider {ProviderId}", LogSanitizer.SanitizeGuid(id));
                
                // Use soft delete instead of hard delete
                return await _repository.SoftDeleteAsync(id, "system");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting provider {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<bool> SoftDeleteAsync(Guid id, string deletedBy)
        {
            try
            {
                _logger.LogInformation("Soft deleting provider {ProviderId} by {DeletedBy}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(deletedBy));
                
                return await _repository.SoftDeleteAsync(id, deletedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error soft deleting provider {ProviderId} by {DeletedBy}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(deletedBy));
                throw;
            }
        }

        public async Task<bool> RestoreAsync(Guid id, string restoredBy)
        {
            try
            {
                _logger.LogInformation("Restoring provider {ProviderId} by {RestoredBy}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(restoredBy));
                
                return await _repository.RestoreAsync(id, restoredBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring provider {ProviderId} by {RestoredBy}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeString(restoredBy));
                throw;
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            try
            {
                return await _repository.ExistsAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence {ProviderId}", LogSanitizer.SanitizeGuid(id));
                throw;
            }
        }

        public async Task<bool> ExistsByCodeAsync(string code)
        {
            try
            {
                return await _repository.ExistsByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking provider existence by code {ProviderCode}", LogSanitizer.SanitizeString(code));
                throw;
            }
        }

        public async Task<bool> ValidateCredentialsSchemaAsync(Guid providerId, JsonDocument credentials)
        {
            try
            {
                _logger.LogInformation("Validating credentials schema for provider {ProviderId}", LogSanitizer.SanitizeGuid(providerId));

                var provider = await _repository.GetByIdAsync(providerId);
                if (provider == null)
                {
                    throw new KeyNotFoundException($"Provider with ID {providerId} not found");
                }

                if (string.IsNullOrEmpty(provider.CredentialsSchema.RootElement.GetRawText()))
                {
                    return true; // No schema defined, consider valid
                }

                // Parse the schema
                var schema = JsonSchema.FromText(provider.CredentialsSchema.RootElement.GetRawText());
                if (schema == null)
                {
                    _logger.LogError("Invalid schema format for provider {ProviderId}", LogSanitizer.SanitizeGuid(providerId));
                    return false;
                }

                // Validate credentials against schema
                var validationResult = schema.Evaluate(credentials.RootElement);
                
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "Credentials validation failed for provider {ProviderId}. Details: {Details}",
                        LogSanitizer.SanitizeGuid(providerId),
                        LogSanitizer.SanitizeString(string.Join(", ", validationResult.Details.Select(d => d.ToString())))
                    );
                    return false;
                }

                _logger.LogInformation("Credentials validation successful for provider {ProviderId}", LogSanitizer.SanitizeGuid(providerId));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credentials schema for provider {ProviderId}", LogSanitizer.SanitizeGuid(providerId));
                throw;
            }
        }

        // Helper method to generate a schema for a provider
        public static string GenerateCredentialsSchema(string providerType)
        {
            try
            {
                string schemaJson = providerType.ToLower() switch
                {
                    "basic" => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""username"": { ""type"": ""string"", ""minLength"": 1 },
                            ""password"": { ""type"": ""string"", ""minLength"": 1 }
                        },
                        ""required"": [""username"", ""password""]
                    }",
                    "api_key" => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""api_key"": { ""type"": ""string"", ""minLength"": 1 },
                            ""header_name"": { ""type"": ""string"", ""default"": ""X-API-Key"" }
                        },
                        ""required"": [""api_key""]
                    }",
                    "oauth2" => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""client_id"": { ""type"": ""string"", ""minLength"": 1 },
                            ""client_secret"": { ""type"": ""string"", ""minLength"": 1 },
                            ""token_url"": { ""type"": ""string"", ""format"": ""uri"" }
                        },
                        ""required"": [""client_id"", ""client_secret"", ""token_url""]
                    }",
                    "jwt" => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""jwt_token"": { ""type"": ""string"", ""minLength"": 1 },
                            ""token_type"": { ""type"": ""string"", ""default"": ""Bearer"" }
                        },
                        ""required"": [""jwt_token""]
                    }",
                    _ => @"{
                        ""type"": ""object"",
                        ""properties"": {
                            ""custom_field"": { ""type"": ""string"", ""minLength"": 1 }
                        },
                        ""required"": [""custom_field""]
                    }"
                };

                return schemaJson;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating schema for provider type {providerType}: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates the structure of a credentials schema
        /// </summary>
        private (bool IsValid, List<string> Errors) ValidateCredentialsSchemaStructure(JsonDocument schema)
        {
            var errors = new List<string>();

            try
            {
                var root = schema.RootElement;

                // Check if it's an object
                if (root.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("Credentials schema must be a JSON object");
                    return (false, errors);
                }

                // Validate required top-level properties
                if (!root.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
                    errors.Add("Credentials schema must have a 'name' property");

                if (!root.TryGetProperty("description", out var descElement) || string.IsNullOrWhiteSpace(descElement.GetString()))
                    errors.Add("Credentials schema must have a 'description' property");

                if (!root.TryGetProperty("required_fields", out var requiredFieldsElement))
                    errors.Add("Credentials schema must have a 'required_fields' property");

                // Validate required_fields array
                if (requiredFieldsElement.ValueKind == JsonValueKind.Array)
                {
                    var requiredFields = requiredFieldsElement.EnumerateArray();
                    if (!requiredFields.Any())
                    {
                        errors.Add("At least one required field must be defined");
                    }
                    else
                    {
                        int fieldIndex = 0;
                        foreach (var field in requiredFields)
                        {
                            var fieldErrors = ValidateCredentialField(field, $"required_fields[{fieldIndex}]");
                            errors.AddRange(fieldErrors);
                            fieldIndex++;
                        }
                    }
                }
                else
                {
                    errors.Add("'required_fields' must be an array");
                }

                // Validate optional_fields array if present
                if (root.TryGetProperty("optional_fields", out var optionalFieldsElement))
                {
                    if (optionalFieldsElement.ValueKind == JsonValueKind.Array)
                    {
                        int fieldIndex = 0;
                        foreach (var field in optionalFieldsElement.EnumerateArray())
                        {
                            var fieldErrors = ValidateCredentialField(field, $"optional_fields[{fieldIndex}]");
                            errors.AddRange(fieldErrors);
                            fieldIndex++;
                        }
                    }
                    else
                    {
                        errors.Add("'optional_fields' must be an array");
                    }
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"Invalid JSON format: {ex.Message}");
            }
            catch (Exception ex)
            {
                errors.Add($"Validation error: {ex.Message}");
            }

            return (errors.Count == 0, errors);
        }

        private List<string> ValidateCredentialField(JsonElement field, string fieldPath)
        {
            var errors = new List<string>();

            // Validate field is an object
            if (field.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"{fieldPath} must be an object");
                return errors;
            }

            // Validate required field properties
            if (!field.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
                errors.Add($"{fieldPath}.name is required");

            if (!field.TryGetProperty("type", out var typeElement) || string.IsNullOrWhiteSpace(typeElement.GetString()))
                errors.Add($"{fieldPath}.type is required");

            if (!field.TryGetProperty("description", out var descElement) || string.IsNullOrWhiteSpace(descElement.GetString()))
                errors.Add($"{fieldPath}.description is required");

            // Validate field type if present
            if (typeElement.ValueKind == JsonValueKind.String)
            {
                var type = typeElement.GetString()?.ToLowerInvariant();
                var validTypes = new[]
                {
                    "string", "number", "integer", "boolean", "email", "url", "password",
                    "jwt", "api_key", "client_id", "client_secret", "access_token", "refresh_token",
                    "username", "certificate", "private_key", "public_key", "base64", "json"
                };

                if (!validTypes.Contains(type))
                    errors.Add($"{fieldPath}.type '{type}' is not a valid field type");
            }

            // Validate string length constraints
            if (nameElement.ValueKind == JsonValueKind.String && nameElement.GetString()?.Length > 100)
                errors.Add($"{fieldPath}.name cannot exceed 100 characters");

            if (descElement.ValueKind == JsonValueKind.String && descElement.GetString()?.Length > 500)
                errors.Add($"{fieldPath}.description cannot exceed 500 characters");

            return errors;
        }

        public async Task<SurchargeProviderStatus?> GetStatusByCodeAsync(string code)
        {
            try
            {
                _logger.LogInformation("Getting status by code {StatusCode}", LogSanitizer.SanitizeString(code));
                return await _repository.GetStatusByCodeAsync(code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting status by code {StatusCode}", LogSanitizer.SanitizeString(code));
                throw;
            }
        }
    }
} 