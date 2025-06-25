using System.Collections.Generic;
using System.Linq;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Extension methods for SurchargeProvider entities
    /// </summary>
    public static class SurchargeProviderExtensions
    {
        /// <summary>
        /// Converts a SurchargeProvider entity to a SurchargeProviderResponse
        /// </summary>
        /// <param name="provider">The provider entity to convert</param>
        /// <returns>A response object with clean status representation</returns>
        public static SurchargeProviderResponse ToResponse(this SurchargeProvider provider)
        {
            var response = new SurchargeProviderResponse
            {
                Id = provider.Id,
                Name = provider.Name,
                Code = provider.Code,
                Description = provider.Description,
                BaseUrl = provider.BaseUrl,
                AuthenticationType = provider.AuthenticationType,
                CredentialsSchema = provider.CredentialsSchema,
                Status = provider.Status?.Code ?? "UNKNOWN",
                CreatedAt = provider.CreatedAt,
                UpdatedAt = provider.UpdatedAt,
                CreatedBy = provider.CreatedBy,
                UpdatedBy = provider.UpdatedBy
            };

            // Include configuration if it exists
            if (provider.Configurations != null && provider.Configurations.Any())
            {
                var config = provider.Configurations.First(); // Get the first configuration
                response.Configuration = new ProviderConfigurationResponse
                {
                    Id = config.Id,
                    ConfigName = config.ConfigName,
                    IsActive = config.IsActive,
                    IsPrimary = config.IsPrimary,
                    Credentials = config.Credentials,
                    Timeout = config.Timeout,
                    RetryCount = config.RetryCount,
                    RetryDelay = config.RetryDelay,
                    RateLimit = config.RateLimit,
                    RateLimitPeriod = config.RateLimitPeriod,
                    Metadata = config.Metadata,
                    CreatedAt = config.CreatedAt,
                    UpdatedAt = config.UpdatedAt,
                    LastUsedAt = config.LastUsedAt,
                    LastSuccessAt = config.LastSuccessAt,
                    LastErrorAt = config.LastErrorAt,
                    LastErrorMessage = config.LastErrorMessage,
                    SuccessCount = config.SuccessCount,
                    ErrorCount = config.ErrorCount,
                    AverageResponseTime = config.AverageResponseTime
                };
            }

            return response;
        }

        /// <summary>
        /// Converts a collection of SurchargeProvider entities to SurchargeProviderResponse objects
        /// </summary>
        /// <param name="providers">The collection of provider entities to convert</param>
        /// <returns>A collection of response objects with clean status representation</returns>
        public static IEnumerable<SurchargeProviderResponse> ToResponse(this IEnumerable<SurchargeProvider> providers)
        {
            return providers.Select(p => p.ToResponse());
        }
    }
} 