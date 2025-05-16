using System;

namespace FeeNominalService.Models.ApiKey
{
    /// <summary>
    /// Response model containing API key information
    /// </summary>
    public class ApiKeyInfo
    {
        /// <summary>
        /// The API key value
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// The API key secret
        /// </summary>
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the API key
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Rate limit for the API key
        /// </summary>
        public int RateLimit { get; set; }

        /// <summary>
        /// List of allowed endpoints
        /// </summary>
        public string[] AllowedEndpoints { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Status of the API key
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// When the API key expires
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// When the API key was created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the API key was last rotated
        /// </summary>
        public DateTime? LastRotatedAt { get; set; }

        /// <summary>
        /// When the API key was revoked, if applicable
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// Whether the API key has been revoked
        /// </summary>
        public bool IsRevoked { get; set; }

        /// <summary>
        /// Whether the API key has expired
        /// </summary>
        public bool IsExpired { get; set; }
    }
} 