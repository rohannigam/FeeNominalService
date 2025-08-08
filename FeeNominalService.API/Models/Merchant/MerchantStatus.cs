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
        public int MerchantStatusId { get; set; }

        /// <summary>
        /// Status code (e.g., "ACTIVE", "INACTIVE")
        /// </summary>
        [Required]
        [Column("code")]
        [StringLength(50)]
        public required string Code { get; set; }

        /// <summary>
        /// Display name of the status
        /// </summary>
        [Required]
        [Column("name")]
        [StringLength(100)]
        public required string Name { get; set; }

        /// <summary>
        /// Description of the status
        /// </summary>
        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// When the status was created
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the status was last updated
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Merchant> Merchants { get; set; } = new List<Merchant>();
    }
} 