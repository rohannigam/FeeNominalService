namespace FeeNominalService.Models
{
    public class RefundRequest
    {
        public string? sTxId { get; set; }
        public string? mTxId { get; set; }
        public string? cardToken { get; set; }
        public double? amount { get; set; }
        public List<string> data { get; set; } = new List<string>();
    }
} 