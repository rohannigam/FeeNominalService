namespace FeeNominalService.Models.Client
{
    public class SignedRequestModel
    {
        public required string MerchantId { get; set; }
        public required string Timestamp { get; set; }
        public required string Nonce { get; set; }
        public required string Signature { get; set; }
        public required string RequestBody { get; set; }
    }
} 