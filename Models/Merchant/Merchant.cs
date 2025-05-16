using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// External identifier for the merchant
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("external_id")]
    public string ExternalId { get; set; } = string.Empty;

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
    public Guid StatusId { get; set; }

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
    [InverseProperty("Merchants")]
    public virtual MerchantStatus Status { get; set; } = null!;

    /// <summary>
    /// Navigation property for the merchant's API keys
    /// </summary>
    public virtual ICollection<FeeNominalService.Models.ApiKey.ApiKey> ApiKeys { get; set; } = new List<FeeNominalService.Models.ApiKey.ApiKey>();
} 