using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Represents a merchant-specific configuration for a surcharge provider
    /// </summary>
    [Table("surcharge_provider_configs")]
    public class SurchargeProviderConfig
    {
        [Key]
        [Column("surcharge_provider_config_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("merchant_id")]
        public Guid MerchantId { get; set; }

        [Required]
        [Column("surcharge_provider_id")]
        public Guid ProviderId { get; set; }

        [Required]
        [Column("config_name")]
        [MaxLength(100)]
        public required string ConfigName { get; set; } = string.Empty;

        [Required]
        [Column("credentials")]
        public required JsonDocument Credentials { get; set; } = JsonDocument.Parse("{}");

        /// <summary>
        /// Whether this configuration is currently active
        /// </summary>
        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        [Column("rate_limit")]
        public int? RateLimit { get; set; }

        [Column("rate_limit_period")]
        public int? RateLimitPeriod { get; set; }

        [Column("timeout")]
        public int? Timeout { get; set; }

        [Column("retry_count")]
        public int? RetryCount { get; set; }

        [Column("retry_delay")]
        public int? RetryDelay { get; set; }

        [Column("metadata")]
        public JsonDocument? Metadata { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("last_used_at")]
        public DateTime? LastUsedAt { get; set; }

        [Column("last_success_at")]
        public DateTime? LastSuccessAt { get; set; }

        [Column("last_error_at")]
        public DateTime? LastErrorAt { get; set; }

        [Column("last_error_message")]
        public string? LastErrorMessage { get; set; }

        [Column("success_count")]
        public int SuccessCount { get; set; }

        [Column("error_count")]
        public int ErrorCount { get; set; }

        [Column("average_response_time")]
        public double? AverageResponseTime { get; set; }

        // Navigation properties
        [ForeignKey("ProviderId")]
        public virtual SurchargeProvider? Provider { get; set; }

        public ICollection<SurchargeProviderConfigHistory>? History { get; set; }
    }
} 