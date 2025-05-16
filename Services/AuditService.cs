using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Models;
using FeeNominalService.Data;

namespace FeeNominalService.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogAuditAsync(
            string entityType,
            Guid entityId,
            string action,
            JsonDocument? oldValues,
            JsonDocument? newValues,
            string performedBy,
            string? ipAddress = null,
            string? userAgent = null,
            JsonDocument? additionalInfo = null)
        {
            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    OldValues = oldValues,
                    NewValues = newValues,
                    PerformedBy = performedBy,
                    PerformedAt = DateTime.UtcNow,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    AdditionalInfo = additionalInfo
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Audit log created for {EntityType} {EntityId}, Action: {Action}, PerformedBy: {PerformedBy}",
                    entityType, entityId, action, performedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating audit log for {EntityType} {EntityId}, Action: {Action}",
                    entityType, entityId, action);
                throw;
            }
        }

        public async Task<AuditLog[]> GetAuditLogsAsync(
            string? entityType = null,
            Guid? entityId = null,
            string? action = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int skip = 0,
            int take = 100)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();

                if (!string.IsNullOrEmpty(entityType))
                    query = query.Where(a => a.EntityType == entityType);

                if (entityId.HasValue)
                    query = query.Where(a => a.EntityId == entityId.Value);

                if (!string.IsNullOrEmpty(action))
                    query = query.Where(a => a.Action == action);

                if (startDate.HasValue)
                    query = query.Where(a => a.PerformedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.PerformedAt <= endDate.Value);

                return await query
                    .OrderByDescending(a => a.PerformedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToArrayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving audit logs for {EntityType} {EntityId}",
                    entityType, entityId);
                throw;
            }
        }
    }
} 