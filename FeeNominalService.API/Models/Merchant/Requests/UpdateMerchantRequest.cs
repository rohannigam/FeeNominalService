using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Merchant.Requests;

/// <summary>
/// Request model for updating merchant details
/// </summary>
public class UpdateMerchantRequest
{
    /// <summary>
    /// Name of the merchant
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Status of the merchant
    /// </summary>
    [Required]
    public int StatusId { get; set; }

    /// <summary>
    /// External merchant ID
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ExternalMerchantId { get; set; } = string.Empty;

    /// <summary>
    /// External merchant GUID
    /// </summary>
    public Guid? ExternalMerchantGuid { get; set; }
} 