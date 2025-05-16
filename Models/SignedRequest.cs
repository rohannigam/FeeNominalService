namespace FeeNominalService.Models
{
    public class SignedRequest
    {
        public required string MerchantId { get; set; }
        public required string Timestamp { get; set; }
        public required string Signature { get; set; }
        public required string Nonce { get; set; }
    }
} 