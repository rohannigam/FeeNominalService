using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models.Merchant
{
    /// <summary>
    /// Represents a merchant status in the system
    /// </summary>
    public class MerchantStatus
    {
        /// <summary>
        /// Unique identifier for the status
        /// </summary>
        [Key]
        [Column("merchant_status_id")]
        public int Id { get; set; }

        /// <summary>
        /// Status code (e.g., "ACTIVE", "INACTIVE")
        /// </summary>
        [Required]
        [StringLength(20)]
        [Column("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Display name of the status
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the status
        /// </summary>
        [StringLength(255)]
        [Column("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Whether this status is active
        /// </summary>
        [Required]
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the status was created
        /// </summary>
        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the status was last updated
        /// </summary>
        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
} 