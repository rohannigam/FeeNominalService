using System;

namespace FeeNominalService.Models.ApiKey.Responses
{
    public class GenerateApiKeyResponse
    {
        public required string ApiKey { get; set; }
        public required string Secret { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
} 