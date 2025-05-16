namespace FeeNominalService.Models.ApiKey
{
    public class ApiKeySecret
    {
        public string ApiKey { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastRotated { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string Status { get; set; } = "ACTIVE";
    }
} 