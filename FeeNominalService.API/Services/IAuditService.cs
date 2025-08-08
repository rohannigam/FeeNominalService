using System;
using System.Threading.Tasks;
using System.Text.Json;
using FeeNominalService.Models;
using System.Collections.Generic;

namespace FeeNominalService.Services
{
    public interface IAuditService
    {
        Task LogAuditAsync(
            string entityType,
            Guid entityId,
            string action,
            string? userId = null,
            Dictionary<string, (string? OldValue, string? NewValue)>? fieldChanges = null
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