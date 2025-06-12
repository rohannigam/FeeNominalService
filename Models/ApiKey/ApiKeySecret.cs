namespace FeeNominalService.Models.ApiKey
{
    public class ApiKeySecret
    {
        public int Id { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public Guid MerchantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastRotated { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public DateTime? ExpiresAt { get; set; }
    }
} 