namespace FeeNominalService.Models.ApiKey.Responses
{
    public class ApiKeyRevokeResponse
    {
        public string ApiKey { get; set; } = string.Empty;
        public DateTime RevokedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
} 