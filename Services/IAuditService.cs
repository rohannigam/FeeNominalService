using System;
using System.Threading.Tasks;
using System.Text.Json;
using FeeNominalService.Models;

namespace FeeNominalService.Services
{
    public interface IAuditService
    {
        Task LogAuditAsync(
            string entityType,
            Guid entityId,
            string action,
            JsonDocument? oldValues,
            JsonDocument? newValues,
            string performedBy,
            string? ipAddress = null,
            string? userAgent = null,
            JsonDocument? additionalInfo = null
        );

        Task<AuditLog[]> GetAuditLogsAsync(
            string? entityType = null,
            Guid? entityId = null,
            string? action = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int skip = 0,
            int take = 100
        );
    }
} 