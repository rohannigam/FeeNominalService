using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Request model for provider configuration
    /// </summary>
    public class ProviderConfigurationRequest
    {
        [Required]
        [StringLength(100)]
        public string ConfigName { get; set; } = string.Empty;

        [Required]
        public object Credentials { get; set; } = new();

        public bool IsPrimary { get; set; } = true;

        [Range(1, 300)]
        public int? Timeout { get; set; }

        [Range(0, 10)]
        public int? RetryCount { get; set; }

        [Range(1, 60)]
        public int? RetryDelay { get; set; }

        [Range(1, 10000)]
        public int? RateLimit { get; set; }

        [Range(1, 3600)]
        public int? RateLimitPeriod { get; set; }

        public object? Metadata { get; set; }
    }
} 