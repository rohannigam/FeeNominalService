using System;

public class ApiKeyResponse
{
    public string ApiKey { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int? RateLimit { get; set; }
    public string[] AllowedEndpoints { get; set; } = Array.Empty<string>();
    public string? Purpose { get; set; }
} 