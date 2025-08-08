using System;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models
{
    public class AuditLog
    {
        [Key]
        [Column("audit_log_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("entity_type", TypeName = "varchar(50)")]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        [Column("entity_id")]
        public Guid EntityId { get; set; }

        [Required]
        [Column("action", TypeName = "varchar(50)")]
        public string Action { get; set; } = string.Empty;

        [Column("user_id", TypeName = "varchar(255)")]
        public string? UserId { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 