using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models.ApiKey
{
    /// <summary>
    /// Represents the status of an API key
    /// </summary>
    public enum ApiKeyStatus
    {
        Active,
        Inactive,
        Suspended,
        Revoked,
        Expired,
        Rotated
    }

    /// <summary>
    /// Represents an API key for authentication
    /// </summary>
    public class ApiKey
    {
        /// <summary>
        /// Unique identifier for the API key
        /// </summary>
        [Key]
        [Column("api_key_id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Reference to the merchant who owns this API key
        /// </summary>
        [Required]
        [Column("merchant_id")]
        public Guid MerchantId { get; set; }

        /// <summary>
        /// The actual API key value
        /// </summary>
        [Required]
        [StringLength(64)]
        [Column("key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Name or description of the API key
        /// </summary>
        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the API key's purpose
        /// </summary>
        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Rate limit for this API key
        /// </summary>
        [Required]
        [Column("rate_limit")]
        public int RateLimit { get; set; }

        /// <summary>
        /// List of allowed endpoints
        /// </summary>
        [Required]
        [Column("allowed_endpoints", TypeName = "text[]")]
        public string[] AllowedEndpoints { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Comma-separated list of allowed endpoints (for serialization)
        /// </summary>
        [NotMapped]
        public string AllowedEndpointsString
        {
            get => string.Join(",", AllowedEndpoints);
            set => AllowedEndpoints = value?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Current status of the API key
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } = ApiKeyStatus.Active.ToString();

        /// <summary>
        /// Number of days until the key expires
        /// </summary>
        [Required]
        [Column("expiration_days")]
        public int ExpirationDays { get; set; }

        /// <summary>
        /// When the API key expires
        /// </summary>
        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// When the API key was created
        /// </summary>
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the API key was last updated
        /// </summary>
        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the API key was last used
        /// </summary>
        [Column("last_used_at")]
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// When the API key was last rotated
        /// </summary>
        [Column("last_rotated_at")]
        public DateTime? LastRotatedAt { get; set; }

        /// <summary>
        /// When the API key was revoked
        /// </summary>
        [Column("revoked_at")]
        public DateTime? RevokedAt { get; set; }

        /// <summary>
        /// The user who created the API key
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Reference to the onboarding process
        /// </summary>
        [StringLength(50)]
        [Column("onboarding_reference")]
        public string? OnboardingReference { get; set; }

        /// <summary>
        /// Timestamp for onboarding/rotation/update event
        /// </summary>
        [Column("onboarding_timestamp")]
        public DateTime? OnboardingTimestamp { get; set; }

        /// <summary>
        /// Navigation property for the merchant who owns this API key
        /// </summary>
        [ForeignKey("MerchantId")]
        public virtual FeeNominalService.Models.Merchant.Merchant? Merchant { get; set; }

        /// <summary>
        /// Collection of usage records for this API key
        /// </summary>
        public virtual ICollection<ApiKeyUsage> UsageRecords { get; set; } = new List<ApiKeyUsage>();

        /// <summary>
        /// Checks if the API key is active
        /// </summary>
        public bool IsActive => Status == ApiKeyStatus.Active.ToString();

        /// <summary>
        /// Purpose of the API key
        /// </summary>
        [StringLength(50)]
        [Column("purpose")]
        public string? Purpose { get; set; }
    }
} 