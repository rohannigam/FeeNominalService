using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Represents a status for surcharge providers in the system
    /// </summary>
    [Table("surcharge_provider_statuses")]
    public class SurchargeProviderStatus
    {
        [Key]
        [Column("status_id")]
        public int StatusId { get; set; }

        [Required]
        [Column("code")]
        [MaxLength(20)]
        public required string Code { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(50)]
        public required string Name { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<SurchargeProvider> Providers { get; set; } = new List<SurchargeProvider>();
    }
} 