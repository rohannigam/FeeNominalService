using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models
{
    /// <summary>
    /// Represents an authentication attempt in the system
    /// </summary>
    [Table("authentication_attempts")]
    public class AuthenticationAttempt
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("merchant_id")]
        [MaxLength(50)]
        public required string MerchantId { get; set; }

        [Required]
        [Column("is_successful")]
        public bool IsSuccessful { get; set; }

        [Column("failure_reason")]
        [MaxLength(255)]
        public string? FailureReason { get; set; }

        [Required]
        [Column("timestamp")]
        public DateTime Timestamp { get; set; }

        [Required]
        [Column("ip_address")]
        [MaxLength(45)]
        public required string IpAddress { get; set; }

        [Column("user_agent")]
        [MaxLength(255)]
        public string? UserAgent { get; set; }
    }
} 