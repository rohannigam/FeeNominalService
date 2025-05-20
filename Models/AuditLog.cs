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
        [Column(TypeName = "varchar(50)")]
        public string EntityType { get; set; } = string.Empty;

        [Required]
        public Guid EntityId { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string Action { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public JsonDocument? OldValues { get; set; }

        [Column(TypeName = "jsonb")]
        public JsonDocument? NewValues { get; set; }

        [Required]
        [Column(TypeName = "varchar(50)")]
        public string PerformedBy { get; set; } = string.Empty;

        [Required]
        public DateTime PerformedAt { get; set; }

        [Column(TypeName = "varchar(45)")]
        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        [Column(TypeName = "jsonb")]
        public JsonDocument? AdditionalInfo { get; set; }
    }
} 