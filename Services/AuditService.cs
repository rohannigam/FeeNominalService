using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using FeeNominalService.Models;
using FeeNominalService.Data;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using FeeNominalService.Utils;
using FeeNominalService.Settings;

namespace FeeNominalService.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;
        private readonly IOptionsMonitor<AuditLoggingSettings> _settings;

        public AuditService(ApplicationDbContext context, ILogger<AuditService> logger, IOptionsMonitor<AuditLoggingSettings> settings)
        {
            _context = context;
            _logger = logger;
            _settings = settings;
        }

        public async Task LogAuditAsync(
            string entityType,
            Guid entityId,
            string action,
            string? userId = null,
            Dictionary<string, (string? OldValue, string? NewValue)>? fieldChanges = null)
        {
            var config = _settings.CurrentValue;
            if (!config.Enabled) return;
            if (config.Endpoints.TryGetValue(entityType, out var enabled) && !enabled) return;

            try
            {
                var auditLog = new AuditLog
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Action = action,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                if (fieldChanges != null && fieldChanges.Count > 0)
                {
                    foreach (var kvp in fieldChanges)
                    {
                        // Sanitize sensitive field values before storing in audit log
                        var sanitizedOldValue = IsSensitiveField(kvp.Key) ? "[REDACTED]" : kvp.Value.OldValue;
                        var sanitizedNewValue = IsSensitiveField(kvp.Key) ? "[REDACTED]" : kvp.Value.NewValue;

                        var detail = new AuditLogDetail
                        {
                            AuditLogId = auditLog.Id,
                            FieldName = kvp.Key,
                            OldValue = sanitizedOldValue,
                            NewValue = sanitizedNewValue,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Set<AuditLogDetail>().Add(detail);
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation(
                    "Audit log created for {EntityType} {EntityId}, Action: {Action}, UserId: {UserId}",
                    LogSanitizer.SanitizeString(entityType), LogSanitizer.SanitizeGuid(entityId), LogSanitizer.SanitizeString(action), LogSanitizer.SanitizeString(userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error creating audit log for {EntityType} {EntityId}, Action: {Action}",
                    LogSanitizer.SanitizeString(entityType), LogSanitizer.SanitizeGuid(entityId), LogSanitizer.SanitizeString(action));
                // Swallow exception to avoid impacting main workflow
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
                    query = query.Where(a => a.CreatedAt >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(a => a.CreatedAt <= endDate.Value);

                return await query
                    .OrderByDescending(a => a.CreatedAt)
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

        /// <summary>
        /// Determines if a field name contains sensitive data that should be redacted
        /// </summary>
        /// <param name="fieldName">The field name to check</param>
        /// <returns>True if the field contains sensitive data</returns>
        private static bool IsSensitiveField(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return false;

            var sensitiveFieldNames = new[]
            {
                "credentials",
                "secret",
                "password",
                "token",
                "key",
                "api_key",
                "jwt",
                "authorization",
                "auth"
            };

            return sensitiveFieldNames.Any(sensitive => 
                fieldName.Contains(sensitive, StringComparison.OrdinalIgnoreCase));
        }
    }
} 