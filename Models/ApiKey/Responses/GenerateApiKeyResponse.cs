using System;

namespace FeeNominalService.Models.ApiKey.Responses
{
    public class GenerateApiKeyResponse
    {
        public required string ApiKey { get; set; }
        public required string Secret { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int? RateLimit { get; set; }
        public string[] AllowedEndpoints { get; set; } = Array.Empty<string>();
        public string? Purpose { get; set; }
    }
} 