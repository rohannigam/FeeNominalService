namespace FeeNominalService
{
    public static class ApiConstants
    {
        //API Endpoints
        public const string InterpaymentsBaseAddress = "https://api-test.interpayments.com/v1/ch";
        public const string InterpaymentsBaseSaleAddress = "https://api-test.interpayments.com/v1/ch/sale";
        public const string InterpaymentsBaseRefundAddress = "https://api-test.interpayments.com/v1/ch/refund";
        public const string InterpaymentsBaseCancelAddress = "https://api-test.interpayments.com/v1/ch/cancel";

        // Headers
        public const string ContentTypeHeader = "application/json";
        public const string AcceptHeader = "application/json";

        // Timeout
        public const int DefaultTimeoutSeconds = 30;

        // Retry Policy
        public const int MaxRetryAttempts = 3;
        public const int RetryDelayMilliseconds = 1000;

        // Supported Currencies
        public static readonly string[] SupportedCurrencies = { "USD", "CAD" };

        // Rate Limits
        public const int MaxRequestsPerMinute = 60;

    }
}