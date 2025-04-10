namespace FeeNominalService.Models;

public class BatchSaleRequest
{
    public required List<SaleRequest> Sales { get; set; }
} 