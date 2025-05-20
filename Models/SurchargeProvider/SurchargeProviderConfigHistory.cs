using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Represents the history of changes to surcharge provider configurations
    /// </summary>
    [Table("surcharge_provider_config_history")]
    public class SurchargeProviderConfigHistory
    {
        [Key]
        [Column("surcharge_provider_config_history_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("config_id")]
        public Guid ConfigId { get; set; }

        [Required]
        [Column("changed_at")]
        public DateTime ChangedAt { get; set; }

        [Required]
        [Column("changed_by")]
        [MaxLength(50)]
        public required string ChangedBy { get; set; }

        [Required]
        [Column("change_type")]
        [MaxLength(20)]
        public required string ChangeType { get; set; }

        [Column("change_reason")]
        public string? ChangeReason { get; set; }

        [Required]
        [Column("previous_values")]
        public required JsonDocument PreviousValues { get; set; }

        [Required]
        [Column("new_values")]
        public required JsonDocument NewValues { get; set; }

        // Navigation properties
        [ForeignKey("ConfigId")]
        public virtual SurchargeProviderConfig? Config { get; set; }
    }
} 