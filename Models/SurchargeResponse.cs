namespace FeeNominalService.Models;

//cURRENTLY NOT USED Need to standardize the response
public class SurchargeResponse
{
    public required string TransactionId { get; set; }
    public required decimal OriginalAmount { get; set; }
    public required decimal SurchargeAmount { get; set; }
    public required decimal TotalAmount { get; set; }
    public required string Currency { get; set; }
    public required string Status { get; set; }
    public string? ErrorMessage { get; set; }
} 