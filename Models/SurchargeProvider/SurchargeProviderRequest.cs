using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FeeNominalService.Settings;
using FeeNominalService.Services;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Request model for creating or updating a surcharge provider
    /// </summary>
    public class SurchargeProviderRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(200)]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AuthenticationType { get; set; } = string.Empty;

        [Required]
        public object? CredentialsSchema { get; set; }

        /// <summary>
        /// Optional configuration to create along with the provider
        /// This allows creating both provider and configuration in a single API call
        /// </summary>
        public ProviderConfigurationRequest? Configuration { get; set; }

        /// <summary>
        /// Validates the credentials schema structure using configuration settings
        /// </summary>
        public virtual bool ValidateCredentialsSchema(out List<string> errors, SurchargeProviderValidationSettings? settings = null)
        {
            errors = new List<string>();

            // Check if credentials schema is provided
            if (CredentialsSchema == null)
            {
                errors.Add("Credentials schema is required");
                return false;
            }

            try
            {
                // Convert to JSON to validate structure
                var jsonString = JsonSerializer.Serialize(CredentialsSchema);
                
                // Check if the serialized result is an empty object or invalid
                if (jsonString == "{}" || jsonString == "null")
                {
                    errors.Add("Credentials schema cannot be empty");
                    return false;
                }

                var jsonDocument = JsonDocument.Parse(jsonString);
                var root = jsonDocument.RootElement;

                // Check total schema size if settings provided
                if (settings != null && jsonString.Length > settings.MaxSchemaObjectSize)
                {
                    errors.Add($"Schema object size ({jsonString.Length} characters) exceeds maximum allowed size ({settings.MaxSchemaObjectSize} characters)");
                }

                // Check if it's an object
                if (root.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("Credentials schema must be a JSON object");
                    return false;
                }

                // Check if the object has any properties
                if (!root.EnumerateObject().Any())
                {
                    errors.Add("Credentials schema cannot be an empty object");
                    return false;
                }

                // Validate required top-level properties
                if (!root.TryGetProperty("name", out var nameElement) || string.IsNullOrWhiteSpace(nameElement.GetString()))
                    errors.Add("Credentials schema must have a 'name' property");

                if (!root.TryGetProperty("description", out var descElement) || string.IsNullOrWhiteSpace(descElement.GetString()))
                    errors.Add("Credentials schema must have a 'description' property");

                if (!root.TryGetProperty("required_fields", out var requiredFieldsElement))
                    errors.Add("Credentials schema must have a 'required_fields' property");

                // Validate schema name and description length if settings provided
                if (settings != null)
                {
                    if (nameElement.ValueKind == JsonValueKind.String && nameElement.GetString()?.Length > settings.MaxSchemaNameLength)
                        errors.Add($"Schema name cannot exceed {settings.MaxSchemaNameLength} characters");

                    if (descElement.ValueKind == JsonValueKind.String && descElement.GetString()?.Length > settings.MaxSchemaDescriptionLength)
                        errors.Add($"Schema description cannot exceed {settings.MaxSchemaDescriptionLength} characters");
                }

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
                        var fieldCount = 0;
                        int fieldIndex = 0;
                        foreach (var field in requiredFields)
                        {
                            fieldCount++;
                            var fieldErrors = ValidateCredentialField(field, $"required_fields[{fieldIndex}]", settings);
                            errors.AddRange(fieldErrors);
                            fieldIndex++;
                        }

                        // Check maximum required fields if settings provided
                        if (settings != null && fieldCount > settings.MaxRequiredFields)
                        {
                            errors.Add($"Number of required fields ({fieldCount}) exceeds maximum allowed ({settings.MaxRequiredFields})");
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
                        var fieldCount = 0;
                        int fieldIndex = 0;
                        foreach (var field in optionalFieldsElement.EnumerateArray())
                        {
                            fieldCount++;
                            var fieldErrors = ValidateCredentialField(field, $"optional_fields[{fieldIndex}]", settings);
                            errors.AddRange(fieldErrors);
                            fieldIndex++;
                        }

                        // Check maximum optional fields if settings provided
                        if (settings != null && fieldCount > settings.MaxOptionalFields)
                        {
                            errors.Add($"Number of optional fields ({fieldCount}) exceeds maximum allowed ({settings.MaxOptionalFields})");
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

            return errors.Count == 0;
        }

        /// <summary>
        /// Validates the configuration if provided using configuration settings
        /// </summary>
        public bool ValidateConfiguration(out List<string> errors, SurchargeProviderValidationSettings? settings = null)
        {
            errors = new List<string>();

            if (Configuration == null)
                return true; // Configuration is optional

            // Validate configuration name
            if (string.IsNullOrWhiteSpace(Configuration.ConfigName))
                errors.Add("Configuration name is required");

            var maxConfigNameLength = settings?.MaxConfigNameLength ?? 100;
            if (Configuration.ConfigName?.Length > maxConfigNameLength)
                errors.Add($"Configuration name cannot exceed {maxConfigNameLength} characters");

            // Validate credentials
            if (Configuration.Credentials == null)
                errors.Add("Configuration credentials are required");

            // Validate credentials against schema if both are provided
            if (Configuration.Credentials != null && CredentialsSchema != null)
            {
                var schemaValidationErrors = ValidateCredentialsAgainstSchema(Configuration.Credentials, settings);
                errors.AddRange(schemaValidationErrors);
            }

            // Validate timeout if provided
            var maxTimeout = settings?.MaxTimeoutSeconds ?? 300;
            if (Configuration.Timeout.HasValue && (Configuration.Timeout.Value < 1 || Configuration.Timeout.Value > maxTimeout))
                errors.Add($"Timeout must be between 1 and {maxTimeout} seconds");

            // Validate retry count if provided
            var maxRetryCount = settings?.MaxRetryCount ?? 10;
            if (Configuration.RetryCount.HasValue && (Configuration.RetryCount.Value < 0 || Configuration.RetryCount.Value > maxRetryCount))
                errors.Add($"Retry count must be between 0 and {maxRetryCount}");

            // Validate retry delay if provided
            var maxRetryDelay = settings?.MaxRetryDelaySeconds ?? 60;
            if (Configuration.RetryDelay.HasValue && (Configuration.RetryDelay.Value < 1 || Configuration.RetryDelay.Value > maxRetryDelay))
                errors.Add($"Retry delay must be between 1 and {maxRetryDelay} seconds");

            // Validate rate limit if provided
            var maxRateLimit = settings?.MaxRateLimit ?? 10000;
            if (Configuration.RateLimit.HasValue && (Configuration.RateLimit.Value < 1 || Configuration.RateLimit.Value > maxRateLimit))
                errors.Add($"Rate limit must be between 1 and {maxRateLimit}");

            // Validate rate limit period if provided
            var maxRateLimitPeriod = settings?.MaxRateLimitPeriodSeconds ?? 3600;
            if (Configuration.RateLimitPeriod.HasValue && (Configuration.RateLimitPeriod.Value < 1 || Configuration.RateLimitPeriod.Value > maxRateLimitPeriod))
                errors.Add($"Rate limit period must be between 1 and {maxRateLimitPeriod} seconds");

            return errors.Count == 0;
        }

        /// <summary>
        /// Validates that configuration credentials match the defined schema requirements
        /// </summary>
        private List<string> ValidateCredentialsAgainstSchema(object credentials, SurchargeProviderValidationSettings? settings = null)
        {
            var errors = new List<string>();

            try
            {
                // Parse the credentials schema to extract required fields
                var schemaJson = JsonSerializer.Serialize(CredentialsSchema);
                var schemaDoc = JsonDocument.Parse(schemaJson);
                var schemaRoot = schemaDoc.RootElement;

                // Extract required field names from schema
                var requiredFieldNames = new List<string>();
                if (schemaRoot.TryGetProperty("required_fields", out var requiredFieldsElement) && 
                    requiredFieldsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var field in requiredFieldsElement.EnumerateArray())
                    {
                        if (field.TryGetProperty("name", out var nameElement))
                        {
                            var fieldName = nameElement.GetString();
                            if (!string.IsNullOrWhiteSpace(fieldName))
                            {
                                requiredFieldNames.Add(fieldName);
                            }
                        }
                    }
                }

                // Parse configuration credentials
                var credentialsJson = JsonSerializer.Serialize(credentials);
                var credentialsDoc = JsonDocument.Parse(credentialsJson);
                var credentialsRoot = credentialsDoc.RootElement;

                // Check that all required fields are present in credentials
                foreach (var requiredFieldName in requiredFieldNames)
                {
                    if (!credentialsRoot.TryGetProperty(requiredFieldName, out var fieldValue))
                    {
                        errors.Add($"Required field '{requiredFieldName}' is missing from configuration credentials");
                        continue;
                    }

                    // Check that required fields have non-empty values
                    if (fieldValue.ValueKind == JsonValueKind.Null || 
                        (fieldValue.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(fieldValue.GetString())))
                    {
                        errors.Add($"Required field '{requiredFieldName}' cannot be null or empty");
                    }

                    // Validate field value length if settings provided
                    if (settings != null && fieldValue.ValueKind == JsonValueKind.String)
                    {
                        var fieldValueString = fieldValue.GetString() ?? string.Empty;
                        
                        if (fieldValueString.Length < settings.MinCredentialValueLength)
                        {
                            errors.Add($"Field '{requiredFieldName}' value length ({fieldValueString.Length}) is below minimum ({settings.MinCredentialValueLength})");
                        }

                        if (fieldValueString.Length > settings.MaxCredentialValueLength)
                        {
                            errors.Add($"Field '{requiredFieldName}' value length ({fieldValueString.Length}) exceeds maximum ({settings.MaxCredentialValueLength})");
                        }
                    }
                }

                // Check for extra fields not defined in schema (optional validation)
                if (settings != null && settings.ValidateExtraFields)
                {
                    var schemaFieldNames = new HashSet<string>(requiredFieldNames, StringComparer.OrdinalIgnoreCase);
                    
                    // Add optional field names if they exist in schema
                    if (schemaRoot.TryGetProperty("optional_fields", out var optionalFieldsElement) && 
                        optionalFieldsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var field in optionalFieldsElement.EnumerateArray())
                        {
                            if (field.TryGetProperty("name", out var nameElement))
                            {
                                var fieldName = nameElement.GetString();
                                if (!string.IsNullOrWhiteSpace(fieldName))
                                {
                                    schemaFieldNames.Add(fieldName);
                                }
                            }
                        }
                    }

                    foreach (var property in credentialsRoot.EnumerateObject())
                    {
                        if (!schemaFieldNames.Contains(property.Name))
                        {
                            errors.Add($"Field '{property.Name}' is not defined in the credentials schema");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                errors.Add($"Error parsing credentials or schema: {ex.Message}");
            }
            catch (Exception ex)
            {
                errors.Add($"Error validating credentials against schema: {ex.Message}");
            }

            return errors;
        }

        private List<string> ValidateCredentialField(JsonElement field, string fieldPath, SurchargeProviderValidationSettings? settings = null)
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
                var allowedTypes = settings?.AllowedFieldTypes ?? new[]
                {
                    "string", "number", "integer", "boolean", "email", "url", "password",
                    "jwt", "api_key", "client_id", "client_secret", "access_token", "refresh_token",
                    "username", "certificate", "private_key", "public_key", "base64", "json"
                };

                if (!allowedTypes.Contains(type))
                    errors.Add($"{fieldPath}.type '{type}' is not a valid field type");
            }

            // Validate string length constraints
            var maxFieldNameLength = settings?.MaxFieldNameLength ?? 100;
            var maxFieldDescLength = settings?.MaxFieldDescriptionLength ?? 500;

            if (nameElement.ValueKind == JsonValueKind.String && nameElement.GetString()?.Length > maxFieldNameLength)
                errors.Add($"{fieldPath}.name cannot exceed {maxFieldNameLength} characters");

            if (descElement.ValueKind == JsonValueKind.String && descElement.GetString()?.Length > maxFieldDescLength)
                errors.Add($"{fieldPath}.description cannot exceed {maxFieldDescLength} characters");

            return errors;
        }
    }
} 