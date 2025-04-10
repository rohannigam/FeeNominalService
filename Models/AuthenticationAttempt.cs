namespace FeeNominalService.Models
{
    public class AuthenticationAttempt
    {
        public required string UserId { get; set; }
        public required string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Success { get; set; }
        public required string AuthenticationType { get; set; } // "JWT" or "APIKey"
        public string? FailureReason { get; set; }
        public required string UserAgent { get; set; }
    }
} 