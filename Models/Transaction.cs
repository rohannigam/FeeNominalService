using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string TransactionId { get; set; } = null!;

        [Required]
        public string MerchantId { get; set; } = null!;

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public decimal SurchargeAmount { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = null!;

        [Required]
        public string PaymentMethod { get; set; } = null!;

        [Required]
        public string TransactionType { get; set; } = null!;

        [Required]
        public string Status { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public string? ErrorMessage { get; set; }

        public string? ReferenceId { get; set; }

        public string? Description { get; set; }

        [NotMapped]
        public Dictionary<string, string>? Metadata { get; set; }

        public string? SurchargeCurrency { get; set; }
    }
} 