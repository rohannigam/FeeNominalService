using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.ApiKey.Requests;

/// <summary>
/// Request model for updating an API key
/// </summary>
public class UpdateApiKeyRequest
{
    /// <summary>
    /// The merchant ID associated with the API key to update
    /// </summary>
    [Required]
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// The API key to update
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Description of the API key
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Rate limit for the API key
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? RateLimit { get; set; }

    /// <summary>
    /// List of allowed endpoints
    /// </summary>
    public string[]? AllowedEndpoints { get; set; }
} 