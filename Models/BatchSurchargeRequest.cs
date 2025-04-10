namespace FeeNominalService.Models;

public class BatchSurchargeRequest
{
    public required List<SurchargeRequest> Transactions { get; set; }
} 