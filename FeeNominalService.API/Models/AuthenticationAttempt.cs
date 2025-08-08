using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Models
{
    /// <summary>
    /// Represents an authentication attempt in the system
    /// </summary>
    [Table("authentication_attempts")]
    public class AuthenticationAttempt
    {
        [Key]
        [Column("authentication_attempt_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("merchant_id")]
        public Guid MerchantId { get; set; }

        [Required]
        [Column("api_key_id")]
        public Guid ApiKeyId { get; set; }

        [Required]
        [Column("ip_address")]
        [StringLength(45)]
        public string IpAddress { get; set; } = null!;

        [Required]
        [Column("user_agent")]
        [StringLength(500)]
        public string UserAgent { get; set; } = null!;

        [Required]
        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = null!;

        [Required]
        [Column("attempted_at")]
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        [Column("failure_reason")]
        [StringLength(500)]
        public string? FailureReason { get; set; }

        [ForeignKey("MerchantId")]
        public virtual Merchant.Merchant? Merchant { get; set; }

        [ForeignKey("ApiKeyId")]
        public virtual FeeNominalService.Models.ApiKey.ApiKey? ApiKeyEntity { get; set; }

        /// <summary>
        /// Indicates whether the authentication attempt was successful
        /// </summary>
        [NotMapped]
        public bool IsSuccessful => Status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
    }
} 