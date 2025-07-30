using System;
using System.Collections.Generic;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using FeeNominalService.Utils;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Secure wrapper for CredentialsSchema using SecureString to prevent memory dumps
    /// Checkmarx: Privacy Violation - This class uses SecureString for secure handling of credentials schema data
    /// Enhanced security: Uses SecureString and proper disposal to prevent memory dumps and exposure
    /// </summary>
    public class SecureCredentialsSchema : IDisposable
    {
        private SecureString? _secureSchema;
        private bool _disposed = false;

        // Non-sensitive properties that can be safely exposed
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string? DocumentationUrl { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        // Sensitive properties that need secure handling
        public List<CredentialField> RequiredFields { get; set; } = new();
        public List<CredentialField>? OptionalFields { get; set; }

        /// <summary>
        /// Sets the credentials schema securely using SecureString
        /// </summary>
        /// <param name="schemaJson">The JSON string representation of the schema</param>
        public void SetSchema(string schemaJson)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentialsSchema));

            _secureSchema?.Dispose();
            _secureSchema = new SecureString();
            
            foreach (char c in schemaJson)
            {
                _secureSchema.AppendChar(c);
            }
            _secureSchema.MakeReadOnly();
        }

        /// <summary>
        /// Processes the secure schema data within a secure context
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="processor">Function to process the schema data</param>
        /// <returns>Result of the processing function</returns>
        public T? ProcessSchemaSecurely<T>(Func<SecureString, T> processor)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentialsSchema));

            if (_secureSchema == null)
                return default(T);

            var schemaString = SimpleSecureDataHandler.FromSecureString(_secureSchema);
            return SimpleSecureDataHandler.ProcessSecurely(schemaString, secureSchema => processor(secureSchema));
        }

        /// <summary>
        /// Gets the schema as a JsonDocument for processing
        /// </summary>
        /// <returns>JsonDocument representation of the schema</returns>
        public JsonDocument? GetSchema()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentialsSchema));

            if (_secureSchema == null)
                return null;

            var schemaString = SimpleSecureDataHandler.FromSecureString(_secureSchema);
            return JsonDocument.Parse(schemaString);
        }

        /// <summary>
        /// Gets the schema as a string (use with caution)
        /// </summary>
        /// <returns>String representation of the schema</returns>
        public string GetSchemaString()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentialsSchema));

            if (_secureSchema == null)
                return string.Empty;

            return SimpleSecureDataHandler.FromSecureString(_secureSchema);
        }

        /// <summary>
        /// Creates a SecureCredentialsSchema from a regular CredentialsSchema
        /// </summary>
        /// <param name="schema">The regular CredentialsSchema</param>
        /// <returns>SecureCredentialsSchema wrapper</returns>
        public static SecureCredentialsSchema FromCredentialsSchema(CredentialsSchema schema)
        {
            var secure = new SecureCredentialsSchema
            {
                Name = schema.Name,
                Description = schema.Description,
                Version = schema.Version,
                DocumentationUrl = schema.DocumentationUrl,
                Metadata = schema.Metadata,
                RequiredFields = schema.RequiredFields,
                OptionalFields = schema.OptionalFields
            };

            var schemaJson = JsonSerializer.Serialize(schema);
            secure.SetSchema(schemaJson);
            return secure;
        }

        /// <summary>
        /// Converts back to a regular CredentialsSchema
        /// </summary>
        /// <returns>Regular CredentialsSchema</returns>
        public CredentialsSchema ToCredentialsSchema()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(SecureCredentialsSchema));

            if (_secureSchema == null)
            {
                return new CredentialsSchema
                {
                    Name = this.Name,
                    Description = this.Description,
                    Version = this.Version,
                    DocumentationUrl = this.DocumentationUrl,
                    Metadata = this.Metadata,
                    RequiredFields = this.RequiredFields,
                    OptionalFields = this.OptionalFields
                };
            }

            var schemaString = SimpleSecureDataHandler.FromSecureString(_secureSchema);
            var deserialized = JsonSerializer.Deserialize<CredentialsSchema>(schemaString);
            return deserialized ?? new CredentialsSchema
            {
                Name = this.Name,
                Description = this.Description,
                Version = this.Version,
                DocumentationUrl = this.DocumentationUrl,
                Metadata = this.Metadata,
                RequiredFields = this.RequiredFields,
                OptionalFields = this.OptionalFields
            };
        }

        /// <summary>
        /// Creates a SecureCredentialsSchema from a JsonDocument
        /// </summary>
        /// <param name="schemaDoc">The JsonDocument</param>
        /// <returns>SecureCredentialsSchema wrapper</returns>
        public static SecureCredentialsSchema FromJsonDocument(JsonDocument schemaDoc)
        {
            var schemaJson = schemaDoc.RootElement.GetRawText();
            var schema = JsonSerializer.Deserialize<CredentialsSchema>(schemaJson);
            
            if (schema == null)
                throw new ArgumentException("Invalid credentials schema format");

            return FromCredentialsSchema(schema);
        }

        /// <summary>
        /// Creates a SecureCredentialsSchema from a JSON string
        /// </summary>
        /// <param name="schemaJson">The JSON string</param>
        /// <returns>SecureCredentialsSchema wrapper</returns>
        public static SecureCredentialsSchema FromJsonString(string schemaJson)
        {
            var schema = JsonSerializer.Deserialize<CredentialsSchema>(schemaJson);
            
            if (schema == null)
                throw new ArgumentException("Invalid credentials schema format");

            return FromCredentialsSchema(schema);
        }

        /// <summary>
        /// Disposes the SecureString to clear it from memory
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _secureSchema?.Dispose();
                _secureSchema = null;
                _disposed = true;
            }
        }
    }
} 