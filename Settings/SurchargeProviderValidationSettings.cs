namespace FeeNominalService.Settings
{
    /// <summary>
    /// Settings for surcharge provider validation
    /// </summary>
    public class SurchargeProviderValidationSettings
    {
        /// <summary>
        /// Maximum length for schema name
        /// </summary>
        public int MaxSchemaNameLength { get; set; } = 100;

        /// <summary>
        /// Maximum length for schema description
        /// </summary>
        public int MaxSchemaDescriptionLength { get; set; } = 500;

        /// <summary>
        /// Maximum length for field name
        /// </summary>
        public int MaxFieldNameLength { get; set; } = 100;

        /// <summary>
        /// Maximum length for field description
        /// </summary>
        public int MaxFieldDescriptionLength { get; set; } = 500;

        /// <summary>
        /// Maximum length for individual credential values
        /// </summary>
        public int MaxCredentialValueLength { get; set; } = 10000;

        /// <summary>
        /// Maximum size for entire credentials object (JSON string length)
        /// </summary>
        public int MaxCredentialsObjectSize { get; set; } = 50000;

        /// <summary>
        /// Maximum size for entire schema object (JSON string length)
        /// </summary>
        public int MaxSchemaObjectSize { get; set; } = 10000;

        /// <summary>
        /// Maximum number of required fields allowed in a schema
        /// </summary>
        public int MaxRequiredFields { get; set; } = 20;

        /// <summary>
        /// Maximum number of optional fields allowed in a schema
        /// </summary>
        public int MaxOptionalFields { get; set; } = 10;

        /// <summary>
        /// Maximum number of providers allowed per merchant
        /// </summary>
        public int MaxProvidersPerMerchant { get; set; } = 25;

        /// <summary>
        /// Allowed field types for credential schemas
        /// </summary>
        public string[] AllowedFieldTypes { get; set; } = new[]
        {
            "string", "number", "integer", "boolean", "email", "url", "password",
            "jwt", "api_key", "client_id", "client_secret", "access_token", "refresh_token",
            "username", "certificate", "private_key", "public_key", "base64", "json"
        };

        /// <summary>
        /// Whether to validate JWT token format
        /// </summary>
        public bool ValidateJwtFormat { get; set; } = true;

        /// <summary>
        /// Whether to validate API key format
        /// </summary>
        public bool ValidateApiKeyFormat { get; set; } = true;

        /// <summary>
        /// Whether to validate email format
        /// </summary>
        public bool ValidateEmailFormat { get; set; } = true;

        /// <summary>
        /// Whether to validate URL format
        /// </summary>
        public bool ValidateUrlFormat { get; set; } = true;

        /// <summary>
        /// Minimum length for credential values
        /// </summary>
        public int MinCredentialValueLength { get; set; } = 1;

        /// <summary>
        /// Maximum length for configuration name
        /// </summary>
        public int MaxConfigNameLength { get; set; } = 100;

        /// <summary>
        /// Maximum timeout value in seconds
        /// </summary>
        public int MaxTimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// Maximum retry count
        /// </summary>
        public int MaxRetryCount { get; set; } = 10;

        /// <summary>
        /// Maximum retry delay in seconds
        /// </summary>
        public int MaxRetryDelaySeconds { get; set; } = 60;

        /// <summary>
        /// Maximum rate limit
        /// </summary>
        public int MaxRateLimit { get; set; } = 10000;

        /// <summary>
        /// Maximum rate limit period in seconds
        /// </summary>
        public int MaxRateLimitPeriodSeconds { get; set; } = 3600;

        /// <summary>
        /// Whether to validate that credentials only contain fields defined in the schema
        /// </summary>
        public bool ValidateExtraFields { get; set; } = false;

        public bool UseProviderBulkSale { get; set; } = false;
        public int MaxRequestsPerBulkSale { get; set; } = 5000;
    }
} 