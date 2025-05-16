using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using Microsoft.EntityFrameworkCore;

namespace FeeNominalService.Services
{
    public class ApiKeyExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApiKeyExpirationService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public ApiKeyExpirationService(
            IServiceProvider serviceProvider,
            ILogger<ApiKeyExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("API Key Expiration Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndUpdateExpiredKeysAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for expired API keys.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task CheckAndUpdateExpiredKeysAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var expiredKeys = await context.ApiKeys
                .Where(k => k.ExpiresAt < DateTime.UtcNow && k.Status == "ACTIVE")
                .ToListAsync();

            if (expiredKeys.Any())
            {
                _logger.LogInformation("Found {Count} expired API keys to update.", expiredKeys.Count);

                foreach (var key in expiredKeys)
                {
                    key.Status = "EXPIRED";
                    key.UpdatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Marking API key {ApiKey} as expired.", key.Key);
                }

                await context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated {Count} expired API keys.", expiredKeys.Count);
            }
        }
    }
} 