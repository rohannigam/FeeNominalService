using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Represents a field in the credentials schema
    /// </summary>
    public class CredentialField
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        public bool Required { get; set; } = true;

        public bool Sensitive { get; set; } = false;

        [StringLength(1000)]
        [JsonPropertyName("default_value")]
        public string? DefaultValue { get; set; }

        [StringLength(1000)]
        public string? Pattern { get; set; }

        [JsonPropertyName("min_length")]
        public int? MinLength { get; set; }

        [JsonPropertyName("max_length")]
        public int? MaxLength { get; set; }

        [JsonPropertyName("allowed_values")]
        public List<string>? AllowedValues { get; set; }

        [StringLength(1000)]
        [JsonPropertyName("validation_message")]
        public string? ValidationMessage { get; set; }

        [JsonPropertyName("additional_properties")]
        public Dictionary<string, object>? AdditionalProperties { get; set; }
    }

    /// <summary>
    /// Represents the complete credentials schema for a provider
    /// </summary>
    public class CredentialsSchema
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Version { get; set; } = "1.0";

        [Required]
        [JsonPropertyName("required_fields")]
        public List<CredentialField> RequiredFields { get; set; } = new();

        [JsonPropertyName("optional_fields")]
        public List<CredentialField>? OptionalFields { get; set; }

        [StringLength(1000)]
        [JsonPropertyName("documentation_url")]
        public string? DocumentationUrl { get; set; }

        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Validates the credentials schema structure
        /// </summary>
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();

            // Validate basic properties
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("Schema name is required");

            if (string.IsNullOrWhiteSpace(Description))
                errors.Add("Schema description is required");

            if (string.IsNullOrWhiteSpace(Version))
                errors.Add("Schema version is required");

            // Validate required fields
            if (RequiredFields == null || RequiredFields.Count == 0)
            {
                errors.Add("At least one required field must be defined");
            }
            else
            {
                for (int i = 0; i < RequiredFields.Count; i++)
                {
                    var field = RequiredFields[i];
                    var fieldErrors = ValidateField(field, $"RequiredFields[{i}]");
                    errors.AddRange(fieldErrors);
                }
            }

            // Validate optional fields if present
            if (OptionalFields != null)
            {
                for (int i = 0; i < OptionalFields.Count; i++)
                {
                    var field = OptionalFields[i];
                    var fieldErrors = ValidateField(field, $"OptionalFields[{i}]");
                    errors.AddRange(fieldErrors);
                }
            }

            return errors.Count == 0;
        }

        private List<string> ValidateField(CredentialField field, string fieldPath)
        {
            var errors = new List<string>();

            // Validate field name
            if (string.IsNullOrWhiteSpace(field.Name))
                errors.Add($"{fieldPath}.Name is required");

            if (field.Name.Length > 100)
                errors.Add($"{fieldPath}.Name cannot exceed 100 characters");

            // Validate field type
            if (string.IsNullOrWhiteSpace(field.Type))
                errors.Add($"{fieldPath}.Type is required");

            if (!IsValidFieldType(field.Type))
                errors.Add($"{fieldPath}.Type '{field.Type}' is not a valid field type");

            // Validate field description
            if (string.IsNullOrWhiteSpace(field.Description))
                errors.Add($"{fieldPath}.Description is required");

            if (field.Description.Length > 500)
                errors.Add($"{fieldPath}.Description cannot exceed 500 characters");

            // Validate length constraints
            if (field.MinLength.HasValue && field.MaxLength.HasValue && field.MinLength > field.MaxLength)
                errors.Add($"{fieldPath}.MinLength cannot be greater than MaxLength");

            if (field.MinLength.HasValue && field.MinLength < 0)
                errors.Add($"{fieldPath}.MinLength cannot be negative");

            if (field.MaxLength.HasValue && field.MaxLength < 0)
                errors.Add($"{fieldPath}.MaxLength cannot be negative");

            return errors;
        }

        private bool IsValidFieldType(string type)
        {
            var validTypes = new[]
            {
                "string", "number", "integer", "boolean", "email", "url", "password",
                "jwt", "api_key", "client_id", "client_secret", "access_token", "refresh_token",
                "username", "certificate", "private_key", "public_key", "base64", "json"
            };

            return validTypes.Contains(type.ToLowerInvariant());
        }

        /// <summary>
        /// Converts the schema to JSON Schema format
        /// </summary>
        public string ToJsonSchema()
        {
            var schema = new
            {
                type = "object",
                title = Name,
                description = Description,
                version = Version,
                properties = new Dictionary<string, object>(),
                required = new List<string>(),
                additionalProperties = false
            };

            // Add required fields
            foreach (var field in RequiredFields)
            {
                schema.properties[field.Name] = CreateFieldSchema(field);
                schema.required.Add(field.Name);
            }

            // Add optional fields
            if (OptionalFields != null)
            {
                foreach (var field in OptionalFields)
                {
                    schema.properties[field.Name] = CreateFieldSchema(field);
                }
            }

            return System.Text.Json.JsonSerializer.Serialize(schema, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        private object CreateFieldSchema(CredentialField field)
        {
            var fieldSchema = new Dictionary<string, object>
            {
                ["type"] = GetJsonSchemaType(field.Type),
                ["title"] = field.DisplayName ?? field.Name,
                ["description"] = field.Description
            };

            // Add length constraints
            if (field.MinLength.HasValue)
                fieldSchema["minLength"] = field.MinLength.Value;

            if (field.MaxLength.HasValue)
                fieldSchema["maxLength"] = field.MaxLength.Value;

            // Add pattern if specified
            if (!string.IsNullOrEmpty(field.Pattern))
                fieldSchema["pattern"] = field.Pattern;

            // Add default value if specified
            if (!string.IsNullOrEmpty(field.DefaultValue))
                fieldSchema["default"] = field.DefaultValue;

            // Add allowed values if specified
            if (field.AllowedValues != null && field.AllowedValues.Count > 0)
                fieldSchema["enum"] = field.AllowedValues;

            // Add format for specific types
            if (field.Type.ToLowerInvariant() == "email")
                fieldSchema["format"] = "email";
            else if (field.Type.ToLowerInvariant() == "url")
                fieldSchema["format"] = "uri";

            return fieldSchema;
        }

        private string GetJsonSchemaType(string fieldType)
        {
            return fieldType.ToLowerInvariant() switch
            {
                "number" => "number",
                "integer" => "integer",
                "boolean" => "boolean",
                _ => "string" // Default to string for all other types
            };
        }
    }

    /// <summary>
    /// Predefined credential schemas for common authentication types
    /// </summary>
    public static class CredentialSchemas
    {
        public static CredentialsSchema BasicAuth => new()
        {
            Name = "Basic Authentication",
            Description = "Username and password authentication",
            Version = "1.0",
            RequiredFields = new List<CredentialField>
            {
                new()
                {
                    Name = "username",
                    Type = "string",
                    Description = "Username for authentication",
                    DisplayName = "Username",
                    Required = true,
                    Sensitive = false,
                    MinLength = 1,
                    MaxLength = 100
                },
                new()
                {
                    Name = "password",
                    Type = "password",
                    Description = "Password for authentication",
                    DisplayName = "Password",
                    Required = true,
                    Sensitive = true,
                    MinLength = 1,
                    MaxLength = 255
                }
            }
        };

        public static CredentialsSchema ApiKey => new()
        {
            Name = "API Key Authentication",
            Description = "API key based authentication",
            Version = "1.0",
            RequiredFields = new List<CredentialField>
            {
                new()
                {
                    Name = "api_key",
                    Type = "api_key",
                    Description = "API key for authentication",
                    DisplayName = "API Key",
                    Required = true,
                    Sensitive = true,
                    MinLength = 1,
                    MaxLength = 500
                },
                new()
                {
                    Name = "api_key_header",
                    Type = "string",
                    Description = "HTTP header name for the API key (e.g., X-API-Key, Authorization)",
                    DisplayName = "API Key Header",
                    Required = true,
                    Sensitive = false,
                    DefaultValue = "X-API-Key",
                    MinLength = 1,
                    MaxLength = 100
                }
            }
        };

        public static CredentialsSchema OAuth2 => new()
        {
            Name = "OAuth 2.0 Authentication",
            Description = "OAuth 2.0 client credentials flow",
            Version = "1.0",
            RequiredFields = new List<CredentialField>
            {
                new()
                {
                    Name = "client_id",
                    Type = "client_id",
                    Description = "OAuth 2.0 client identifier",
                    DisplayName = "Client ID",
                    Required = true,
                    Sensitive = false,
                    MinLength = 1,
                    MaxLength = 255
                },
                new()
                {
                    Name = "client_secret",
                    Type = "client_secret",
                    Description = "OAuth 2.0 client secret",
                    DisplayName = "Client Secret",
                    Required = true,
                    Sensitive = true,
                    MinLength = 1,
                    MaxLength = 255
                },
                new()
                {
                    Name = "token_url",
                    Type = "url",
                    Description = "OAuth 2.0 token endpoint URL",
                    DisplayName = "Token URL",
                    Required = true,
                    Sensitive = false,
                    Pattern = "^https?://.+"
                }
            },
            OptionalFields = new List<CredentialField>
            {
                new()
                {
                    Name = "scope",
                    Type = "string",
                    Description = "OAuth 2.0 scope (optional)",
                    DisplayName = "Scope",
                    Required = false,
                    Sensitive = false,
                    MaxLength = 500
                }
            }
        };

        public static CredentialsSchema Jwt => new()
        {
            Name = "JWT Authentication",
            Description = "JSON Web Token based authentication",
            Version = "1.0",
            RequiredFields = new List<CredentialField>
            {
                new()
                {
                    Name = "jwt_token",
                    Type = "jwt",
                    Description = "JSON Web Token for authentication",
                    DisplayName = "JWT Token",
                    Required = true,
                    Sensitive = true,
                    MinLength = 1,
                    MaxLength = 2000
                }
            },
            OptionalFields = new List<CredentialField>
            {
                new()
                {
                    Name = "token_type",
                    Type = "string",
                    Description = "Token type (e.g., Bearer)",
                    DisplayName = "Token Type",
                    Required = false,
                    Sensitive = false,
                    DefaultValue = "Bearer",
                    AllowedValues = new List<string> { "Bearer", "JWT" }
                }
            }
        };

        public static CredentialsSchema Custom => new()
        {
            Name = "Custom Authentication",
            Description = "Custom authentication schema with flexible fields",
            Version = "1.0",
            RequiredFields = new List<CredentialField>
            {
                new()
                {
                    Name = "custom_field",
                    Type = "string",
                    Description = "Custom authentication field",
                    DisplayName = "Custom Field",
                    Required = true,
                    Sensitive = false,
                    MinLength = 1,
                    MaxLength = 500
                }
            }
        };
    }
} 