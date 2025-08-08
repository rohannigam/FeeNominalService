using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeeNominalService.Models
{
    public class AuditLogDetail
    {
        [Key]
        [Column("detail_id")]
        public Guid Id { get; set; }

        [Required]
        [Column("audit_log_id")]
        public Guid AuditLogId { get; set; }

        [Required]
        [Column("field_name", TypeName = "varchar(255)")]
        public string FieldName { get; set; } = string.Empty;

        [Column("old_value")]
        public string? OldValue { get; set; }

        [Column("new_value")]
        public string? NewValue { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 