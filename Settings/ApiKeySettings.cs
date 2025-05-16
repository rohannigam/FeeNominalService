namespace FeeNominalService.Settings
{
    /// <summary>
    /// Settings for API key management
    /// </summary>
    public class ApiKeySettings
    {
        /// <summary>
        /// Default rate limit for API keys
        /// </summary>
        public int DefaultRateLimit { get; set; } = 1000;

        /// <summary>
        /// Default number of days until an API key expires
        /// </summary>
        public int DefaultExpirationDays { get; set; } = 365;

        /// <summary>
        /// Maximum number of active API keys per merchant
        /// </summary>
        public int MaxActiveKeysPerMerchant { get; set; } = 5;

        /// <summary>
        /// Minimum length for API key descriptions
        /// </summary>
        public int MinDescriptionLength { get; set; } = 10;

        /// <summary>
        /// Maximum length for API key descriptions
        /// </summary>
        public int MaxDescriptionLength { get; set; } = 255;
    }
} 