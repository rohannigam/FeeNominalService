using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.ApiKey.Requests;

/// <summary>
/// Request model for rotating an API key
/// </summary>
public class RotateApiKeyRequest
{
    /// <summary>
    /// The merchant ID associated with the API key to rotate
    /// </summary>
    [Required]
    public string MerchantId { get; set; } = string.Empty;

    /// <summary>
    /// The API key to rotate
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;
} 