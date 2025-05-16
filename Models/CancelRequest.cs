namespace FeeNominalService.Models
{
    public class CancelRequest
    {
        public required string sTxId { get; set; }
        public string? mTxId { get; set; }
        public string? cardToken { get; set; }        
        public List<string> data { get; set; } = new List<string>();
        public string? authCode { get; set; }
    }
} 