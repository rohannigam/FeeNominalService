namespace FeeNominalService.Models;

public class BatchCancelRequest
{
    public required List<CancelRequest> Cancellations { get; set; }
} 