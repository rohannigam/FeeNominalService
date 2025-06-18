using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Repositories;

public interface IApiKeyUsageRepository
{
    /// <summary>
    /// Gets the current usage record for an API key in the specified time window
    /// </summary>
    Task<ApiKeyUsage?> GetCurrentUsageAsync(Guid apiKeyId, string endpoint, string ipAddress, DateTime windowStart, DateTime windowEnd);

    /// <summary>
    /// Creates a new usage record for an API key
    /// </summary>
    Task<ApiKeyUsage> CreateUsageAsync(ApiKeyUsage usage);

    /// <summary>
    /// Updates an existing usage record
    /// </summary>
    Task<ApiKeyUsage> UpdateUsageAsync(ApiKeyUsage usage);

    /// <summary>
    /// Gets the total request count for an API key in the specified time window
    /// </summary>
    Task<int> GetTotalRequestCountAsync(Guid apiKeyId, DateTime windowStart, DateTime windowEnd);
} 