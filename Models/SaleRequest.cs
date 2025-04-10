namespace FeeNominalService.Models
{
    public class SaleRequest
    {
        public required string sTxId { get; set; }
        public string? mTxId { get; set; }
    }
} 