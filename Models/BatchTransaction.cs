using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models
{
    public class BatchTransaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string BatchId { get; set; } = null!;

        [Required]
        public Guid MerchantId { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public decimal TotalSurchargeAmount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = null!;

        [Required]
        public int TotalTransactions { get; set; }

        [Required]
        public int SuccessfulTransactions { get; set; }

        [Required]
        public int FailedTransactions { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public string? ErrorMessage { get; set; }

        [ForeignKey("MerchantId")]
        public virtual Merchant.Merchant? Merchant { get; set; }
    }
} 