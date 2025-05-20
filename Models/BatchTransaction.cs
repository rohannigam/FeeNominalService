using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace FeeNominalService.Models
{
    [Table("batch_transactions")]
    public class BatchTransaction
    {
        [Key]
        [Column("batch_transaction_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("merchant_id")]
        public Guid MerchantId { get; set; }

        [Required]
        [Column("batch_id")]
        [StringLength(100)]
        public string BatchId { get; set; } = null!;

        [Required]
        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = null!;

        [Required]
        [Column("total_amount")]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column("total_surcharge")]
        [Range(0, double.MaxValue)]
        public decimal TotalSurcharge { get; set; }

        [Required]
        [Column("currency")]
        [StringLength(3)]
        public string Currency { get; set; } = null!;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("error_message")]
        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [NotMapped]
        public Dictionary<string, string>? Metadata { get; set; }

        [ForeignKey("MerchantId")]
        public virtual Merchant.Merchant? Merchant { get; set; }
    }
} 