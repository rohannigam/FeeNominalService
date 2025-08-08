using System;
using System.Text.Json;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Response model for surcharge provider API endpoints
    /// </summary>
    public class SurchargeProviderResponse
    {
        /// <summary>
        /// Unique identifier for the provider
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the provider
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unique code for the provider
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Description of the provider
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Base URL for the provider's API
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// Type of authentication used by the provider
        /// </summary>
        public string AuthenticationType { get; set; } = string.Empty;

        /// <summary>
        /// Schema defining the required credentials
        /// </summary>
        public JsonDocument CredentialsSchema { get; set; } = JsonDocument.Parse("{}");

        /// <summary>
        /// Status of the provider (e.g., "ACTIVE", "INACTIVE", "SUSPENDED")
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// When the provider was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the provider was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// ID of the merchant who created this provider
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// ID of the merchant who last updated this provider
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Configuration created along with the provider (if provided in request)
        /// </summary>
        public ProviderConfigurationResponse? Configuration { get; set; }

        /// <summary>
        /// Type of the provider (e.g., INTERPAYMENTS, OTHERPROVIDER)
        /// </summary>
        public string ProviderType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response model for provider configuration
    /// </summary>
    public class ProviderConfigurationResponse
    {
        /// <summary>
        /// Unique identifier for the configuration
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Name of the configuration
        /// </summary>
        public string ConfigName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this configuration is currently active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Whether this is the primary configuration
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// The actual credentials (JWT tokens, API keys, etc.)
        /// </summary>
        public JsonDocument Credentials { get; set; } = JsonDocument.Parse("{}");

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int? RetryCount { get; set; }

        /// <summary>
        /// Delay between retries in seconds
        /// </summary>
        public int? RetryDelay { get; set; }

        /// <summary>
        /// Rate limit (requests per period)
        /// </summary>
        public int? RateLimit { get; set; }

        /// <summary>
        /// Rate limit period in seconds
        /// </summary>
        public int? RateLimitPeriod { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public JsonDocument? Metadata { get; set; }

        /// <summary>
        /// When the configuration was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the configuration was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// When the configuration was last used
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// When the configuration was last successful
        /// </summary>
        public DateTime? LastSuccessAt { get; set; }

        /// <summary>
        /// When the configuration last encountered an error
        /// </summary>
        public DateTime? LastErrorAt { get; set; }

        /// <summary>
        /// Last error message encountered
        /// </summary>
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// Number of successful requests
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Number of failed requests
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Average response time in milliseconds
        /// </summary>
        public double? AverageResponseTime { get; set; }
    }
} 