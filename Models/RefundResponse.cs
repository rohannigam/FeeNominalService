namespace FeeNominalService.Models;

public class RefundResponse
{
    public required string TransactionId { get; set; }
    public required decimal OriginalAmount { get; set; }
    public required decimal RefundAmount { get; set; }
    public required decimal RemainingAmount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RefundId { get; set; }
    public DateTime? ProcessedAt { get; set; }
} 