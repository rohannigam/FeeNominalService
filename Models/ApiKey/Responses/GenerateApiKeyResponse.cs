using System;

namespace FeeNominalService.Models.ApiKey.Responses
{
    public class GenerateApiKeyResponse
    {
        /// <summary>
        /// The internal merchant ID (GUID)
        /// </summary>
        public Guid MerchantId { get; set; }

        /// <summary>
        /// The external merchant ID
        /// </summary>
        public string ExternalMerchantId { get; set; } = string.Empty;

        /// <summary>
        /// The merchant name
        /// </summary>
        public string MerchantName { get; set; } = string.Empty;

        /// <summary>
        /// The generated API key
        /// </summary>
        public required string ApiKey { get; set; }

        /// <summary>
        /// The generated secret
        /// </summary>
        public required string Secret { get; set; }

        /// <summary>
        /// When the API key expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Rate limit for the API key
        /// </summary>
        public int? RateLimit { get; set; }

        /// <summary>
        /// List of allowed endpoints
        /// </summary>
        public string[] AllowedEndpoints { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Purpose of the API key
        /// </summary>
        public string? Purpose { get; set; }
    }
} 