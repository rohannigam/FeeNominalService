using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models
{
    public class AuthenticationAttempt
    {
        [Key]
        public int Id { get; set; }
        public string MerchantId { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public DateTime Timestamp { get; set; }
        public string? FailureReason { get; set; }
        public string AuthenticationType { get; set; } = "APIKey"; // Default to APIKey
        public string? UserAgent { get; set; }
    }
} 