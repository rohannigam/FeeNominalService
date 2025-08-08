using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models.Merchant;

/// <summary>
/// Represents an audit trail entry for merchant changes
/// </summary>
public class MerchantAuditTrail
{
    /// <summary>
    /// Unique identifier for the audit trail entry
    /// </summary>
    [Key]
    [Column("merchant_audit_trail_id")]
    public Guid MerchantAuditTrailId { get; set; }

    /// <summary>
    /// Reference to the merchant
    /// </summary>
    [Required]
    [Column("merchant_id")]
    public Guid MerchantId { get; set; }

    /// <summary>
    /// Action performed (e.g., "CREATE", "UPDATE")
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity that was changed
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Name of the property that was changed
    /// </summary>
    [StringLength(255)]
    [Column("property_name")]
    public string? PropertyName { get; set; }

    /// <summary>
    /// Previous value before the change
    /// </summary>
    [Column("old_value")]
    public string? OldValue { get; set; }

    /// <summary>
    /// New value after the change
    /// </summary>
    [Column("new_value")]
    public string? NewValue { get; set; }

    /// <summary>
    /// When the change was made
    /// </summary>
    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who made the change
    /// </summary>
    [Required]
    [StringLength(50)]
    [Column("updated_by")]
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for the merchant
    /// </summary>
    [ForeignKey("MerchantId")]
    public virtual Merchant Merchant { get; set; } = null!;
} 