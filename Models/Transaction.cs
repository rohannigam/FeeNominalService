using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        [Column("transaction_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("external_transaction_id")]
        [StringLength(255)]
        public string TransactionId { get; set; } = null!;

        [Required]
        [Column("merchant_id")]
        public Guid MerchantId { get; set; }

        [Required]
        [Column("amount")]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [Column("surcharge_amount")]
        [Range(0, double.MaxValue)]
        public decimal SurchargeAmount { get; set; }

        [Required]
        [Column("total_amount")]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        [Column("currency")]
        [StringLength(3)]
        public string Currency { get; set; } = null!;

        [Required]
        [Column("payment_method")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = null!;

        [Required]
        [Column("transaction_type")]
        [StringLength(50)]
        public string TransactionType { get; set; } = null!;

        [Required]
        [Column("status")]
        [StringLength(20)]
        public string Status { get; set; } = null!;

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("completed_at")]
        public DateTime? CompletedAt { get; set; }

        [Column("error_message")]
        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        [Column("reference_id")]
        [StringLength(255)]
        public string? ReferenceId { get; set; }

        [Column("description")]
        [StringLength(255)]
        public string? Description { get; set; }

        [NotMapped]
        public Dictionary<string, string>? Metadata { get; set; }

        [Column("surcharge_currency")]
        [StringLength(3)]
        public string? SurchargeCurrency { get; set; }

        [ForeignKey("MerchantId")]
        public virtual Merchant.Merchant? Merchant { get; set; }
    }
} 