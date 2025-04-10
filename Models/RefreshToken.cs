namespace FeeNominalService.Models
{
    public class RefreshToken
    {
        public required string Token { get; set; }
        public required DateTime ExpiresAt { get; set; }
        public required string UserId { get; set; }
        public bool IsRevoked { get; set; }
    }
} 