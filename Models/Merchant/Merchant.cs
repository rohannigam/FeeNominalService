using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Models.Merchant;

/// <summary>
/// Represents a merchant in the system
/// </summary>
public class Merchant
{
    /// <summary>
    /// Unique identifier for the merchant
    /// </summary>
    [Key]
    [Column("merchant_id")]
    public Guid MerchantId { get; set; }

    /// <summary>
    /// External identifier for the merchant
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("external_merchant_id")]
    public string ExternalMerchantId { get; set; } = string.Empty;

    /// <summary>
    /// External GUID for the merchant (optional)
    /// </summary>
    [Column("external_merchant_guid")]
    public Guid? ExternalMerchantGuid { get; set; }

    /// <summary>
    /// Name of the merchant
    /// </summary>
    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the merchant's status
    /// </summary>
    [Required]
    [Column("status_id")]
    public int StatusId { get; set; }

    /// <summary>
    /// When the merchant was created
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the merchant was last updated
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who created the merchant
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("created_by")]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for the merchant's status
    /// </summary>
    [ForeignKey("StatusId")]
    [JsonIgnore]
    public virtual MerchantStatus Status { get; set; } = null!;

    /// <summary>
    /// Collection of API keys associated with this merchant
    /// </summary>
    [InverseProperty("Merchant")]
    [JsonIgnore]
    public virtual ICollection<Models.ApiKey.ApiKey> ApiKeys { get; set; } = new List<Models.ApiKey.ApiKey>();
} 