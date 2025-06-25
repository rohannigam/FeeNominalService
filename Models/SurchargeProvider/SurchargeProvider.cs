using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Represents a surcharge service provider in the system
    /// </summary>
    [Table("surcharge_providers")]
    public class SurchargeProvider
    {
        [Key]
        [Column("surcharge_provider_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(100)]
        public required string Name { get; set; }

        [Required]
        [Column("code")]
        [MaxLength(20)]
        public required string Code { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("base_url")]
        [MaxLength(255)]
        public required string BaseUrl { get; set; }

        [Required]
        [Column("authentication_type")]
        [MaxLength(50)]
        public required string AuthenticationType { get; set; }

        [Required]
        [Column("credentials_schema")]
        public required JsonDocument CredentialsSchema { get; set; }

        [Required]
        [Column("status_id")]
        public int StatusId { get; set; }

        [ForeignKey("StatusId")]
        public virtual SurchargeProviderStatus Status { get; set; } = null!;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("created_by")]
        [MaxLength(50)]
        public required string CreatedBy { get; set; }

        [Required]
        [Column("updated_by")]
        [MaxLength(50)]
        public required string UpdatedBy { get; set; }

        // Navigation properties
        public virtual ICollection<SurchargeProviderConfig>? Configurations { get; set; }
    }
} 