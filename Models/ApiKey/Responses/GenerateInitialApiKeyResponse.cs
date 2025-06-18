using System;
using System.Text.Json.Serialization;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.ApiKey.Requests;

namespace FeeNominalService.Models.ApiKey.Responses;

public class GenerateInitialApiKeyResponse
{
    public Guid MerchantId { get; set; }
    public string ExternalMerchantId { get; set; } = string.Empty;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ExternalMerchantGuid { get; set; }
    
    public string MerchantName { get; set; } = string.Empty;
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public Guid ApiKeyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Secret { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RateLimit { get; set; }
    public string[] AllowedEndpoints { get; set; } = Array.Empty<string>();
    public string? Description { get; set; }
    public string? Purpose { get; set; }
    
    /// <summary>
    /// Metadata about the onboarding process
    /// </summary>
    public OnboardingMetadata OnboardingMetadata { get; set; } = new();
} 