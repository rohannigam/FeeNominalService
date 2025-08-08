using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FeeNominalService.Data;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Utils;

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
                .Where(k => k.ExpiresAt < DateTime.UtcNow && k.Status == ApiKeyStatus.Active.ToString())
                .Select(k => new { k.Id, k.Key, k.Status, k.UpdatedAt })
                .ToListAsync();

            if (expiredKeys.Any())
            {
                _logger.LogInformation("Found {Count} expired API keys to update", expiredKeys.Count);

                foreach (var key in expiredKeys)
                {
                    try
                    {
                        var apiKey = await context.ApiKeys.FindAsync(key.Id);
                        if (apiKey != null)
                        {
                            apiKey.Status = ApiKeyStatus.Expired.ToString();
                            apiKey.UpdatedAt = DateTime.UtcNow;
                            _logger.LogInformation("Marking API key {ApiKey} as expired", LogSanitizer.SanitizeString(key.Key));
                        }
                        else
                        {
                            _logger.LogWarning("API key {ApiKey} not found in database", LogSanitizer.SanitizeString(key.Key));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating API key {ApiKey}", LogSanitizer.SanitizeString(key.Key));
                    }
                }

                try
                {
                    await context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated {Count} expired API keys", expiredKeys.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving changes to database");
                }
            }
        }
    }
} 