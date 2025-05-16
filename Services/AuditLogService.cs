using System.Text.Json;
using FeeNominalService.Models;

namespace FeeNominalService.Services
{
    public interface IAuditLogService
    {
        Task LogAuthenticationAttemptAsync(AuthenticationAttempt attempt);
        Task<IEnumerable<AuthenticationAttempt>> GetAuthenticationAttemptsAsync(string merchantId);
        Task<IEnumerable<AuthenticationAttempt>> GetFailedAttemptsAsync(DateTime? since = null);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly ILogger<AuditLogService> _logger;
        private static readonly List<AuthenticationAttempt> _auditLogs = new();
        private static readonly object _lock = new();

        public AuditLogService(ILogger<AuditLogService> logger)
        {
            _logger = logger;
        }

        public Task LogAuthenticationAttemptAsync(AuthenticationAttempt attempt)
        {
            lock (_lock)
            {
                _auditLogs.Add(attempt);
                // Keep only last 1000 attempts
                if (_auditLogs.Count > 1000)
                {
                    _auditLogs.RemoveRange(0, _auditLogs.Count - 1000);
                }
            }

            _logger.LogInformation(
                "Authentication attempt for merchant {MerchantId} - Success: {IsSuccessful} - IP: {IpAddress}",
                attempt.MerchantId,
                attempt.IsSuccessful,
                attempt.IpAddress
            );

            return Task.CompletedTask;
        }

        public Task<IEnumerable<AuthenticationAttempt>> GetAuthenticationAttemptsAsync(string merchantId)
        {
            lock (_lock)
            {
                return Task.FromResult(_auditLogs.Where(a => a.MerchantId == merchantId));
            }
        }

        public Task<IEnumerable<AuthenticationAttempt>> GetFailedAttemptsAsync(DateTime? since = null)
        {
            lock (_lock)
            {
                var query = _auditLogs.Where(a => !a.IsSuccessful);
                if (since.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= since.Value);
                }
                return Task.FromResult(query);
            }
        }
    }
} 