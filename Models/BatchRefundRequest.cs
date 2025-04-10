namespace FeeNominalService.Models;

public class BatchRefundRequest
{
    public required List<RefundRequest> Refunds { get; set; }
} 