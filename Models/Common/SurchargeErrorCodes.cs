namespace FeeNominalService.Models.Common;

/// <summary>
/// Comprehensive error code system for surcharge operations
/// </summary>
public static class SurchargeErrorCodes
{
    // Authentication & Authorization Errors (1000-1999)
    public static class Auth
    {
        public const string INVALID_API_KEY = "SURCH_1001";
        public const string INVALID_MERCHANT_ID = "SURCH_1002";
        public const string MERCHANT_NOT_FOUND = "SURCH_1003";
        public const string MERCHANT_INACTIVE = "SURCH_1004";
        public const string INSUFFICIENT_PERMISSIONS = "SURCH_1005";
        public const string INVALID_SIGNATURE = "SURCH_1006";
        public const string EXPIRED_API_KEY = "SURCH_1007";
        public const string RATE_LIMIT_EXCEEDED = "SURCH_1008";
    }

    // Provider Configuration Errors (2000-2999)
    public static class Provider
    {
        public const string PROVIDER_NOT_FOUND = "SURCH_2001";
        public const string PROVIDER_INACTIVE = "SURCH_2002";
        public const string PROVIDER_CODE_INVALID = "SURCH_2003";
        public const string PROVIDER_CONFIG_MISSING = "SURCH_2004";
        public const string PROVIDER_CREDENTIALS_INVALID = "SURCH_2005";
        public const string PROVIDER_CREDENTIALS_EXPIRED = "SURCH_2006";
        public const string PROVIDER_BASE_URL_INVALID = "SURCH_2007";
        public const string PROVIDER_AUTHENTICATION_FAILED = "SURCH_2008";
        public const string PROVIDER_NOT_AVAILABLE = "SURCH_2009";
        public const string PROVIDER_RATE_LIMIT_EXCEEDED = "SURCH_2010";
        public const string PROVIDER_LIMIT_EXCEEDED = "SURCH_2011";
    }

    // Request Validation Errors (3000-3999)
    public static class Validation
    {
        public const string INVALID_AMOUNT = "SURCH_3001";
        public const string INVALID_COUNTRY = "SURCH_3002";
        public const string INVALID_CORRELATION_ID = "SURCH_3003";
        public const string INVALID_MERCHANT_TRANSACTION_ID = "SURCH_3004";
        public const string INVALID_PROVIDER_CODE = "SURCH_3005";
        public const string INVALID_POSTAL_CODE = "SURCH_3006";
        public const string INVALID_BIN_VALUE = "SURCH_3007";
        public const string INVALID_SURCHARGE_PROCESSOR = "SURCH_3008";
        public const string INVALID_ENTRY_METHOD = "SURCH_3009";
        public const string INVALID_CARD_TOKEN = "SURCH_3010";
        public const string INVALID_CAMPAIGN = "SURCH_3011";
        public const string INVALID_DATA = "SURCH_3012";
        public const string INVALID_NON_SURCHARGEABLE_AMOUNT = "SURCH_3013";
        public const string INVALID_PROVIDER_TRANSACTION_ID = "SURCH_3014";
        public const string INVALID_TOTAL_AMOUNT = "SURCH_3015";
    }

    // Transaction Processing Errors (4000-4999)
    public static class Transaction
    {
        public const string DUPLICATE_TRANSACTION = "SURCH_4001";
        public const string TRANSACTION_NOT_FOUND = "SURCH_4002";
        public const string TRANSACTION_ALREADY_PROCESSED = "SURCH_4003";
        public const string TRANSACTION_EXPIRED = "SURCH_4004";
        public const string TRANSACTION_CANCELLED = "SURCH_4005";
        public const string TRANSACTION_FAILED = "SURCH_4006";
        public const string INVALID_TRANSACTION_STATE = "SURCH_4007";
        public const string TRANSACTION_TIMEOUT = "SURCH_4008";
        public const string TRANSACTION_ROLLBACK_FAILED = "SURCH_4009";
        public const string TRANSACTION_IDEMPOTENCY_VIOLATION = "SURCH_4010";
    }

    // External Provider Integration Errors (5000-5999)
    public static class ExternalProvider
    {
        public const string PROVIDER_CONNECTION_FAILED = "SURCH_5001";
        public const string PROVIDER_TIMEOUT = "SURCH_5002";
        public const string PROVIDER_SERVICE_UNAVAILABLE = "SURCH_5003";
        public const string PROVIDER_INVALID_RESPONSE = "SURCH_5004";
        public const string PROVIDER_AUTHENTICATION_ERROR = "SURCH_5005";
        public const string PROVIDER_RATE_LIMIT_HIT = "SURCH_5006";
        public const string PROVIDER_INVALID_REQUEST = "SURCH_5007";
        public const string PROVIDER_BUSINESS_RULE_VIOLATION = "SURCH_5008";
        public const string PROVIDER_SYSTEM_ERROR = "SURCH_5009";
        public const string PROVIDER_MAINTENANCE_MODE = "SURCH_5010";
        public const string PROVIDER_CERTIFICATE_ERROR = "SURCH_5011";
        public const string PROVIDER_SSL_ERROR = "SURCH_5012";
        public const string PROVIDER_DNS_ERROR = "SURCH_5013";
        public const string PROVIDER_GATEWAY_ERROR = "SURCH_5014";
    }

    // Database & Storage Errors (6000-6999)
    public static class Database
    {
        public const string DATABASE_CONNECTION_FAILED = "SURCH_6001";
        public const string DATABASE_TIMEOUT = "SURCH_6002";
        public const string DATABASE_DEADLOCK = "SURCH_6003";
        public const string DATABASE_CONSTRAINT_VIOLATION = "SURCH_6004";
        public const string DATABASE_TRANSACTION_FAILED = "SURCH_6005";
        public const string DATABASE_BACKUP_ERROR = "SURCH_6006";
        public const string DATABASE_MAINTENANCE_MODE = "SURCH_6007";
        public const string DATABASE_QUOTA_EXCEEDED = "SURCH_6008";
        public const string DATABASE_INDEX_ERROR = "SURCH_6009";
        public const string DATABASE_SCHEMA_ERROR = "SURCH_6010";
    }

    // Audit & Logging Errors (7000-7999)
    public static class Audit
    {
        public const string AUDIT_LOG_FAILED = "SURCH_7001";
        public const string AUDIT_DATA_CORRUPTION = "SURCH_7002";
        public const string AUDIT_STORAGE_FULL = "SURCH_7003";
        public const string AUDIT_RETENTION_VIOLATION = "SURCH_7004";
        public const string AUDIT_ENCRYPTION_ERROR = "SURCH_7005";
        public const string AUDIT_COMPLIANCE_VIOLATION = "SURCH_7006";
    }

    // Configuration & Environment Errors (8000-8999)
    public static class Configuration
    {
        public const string MISSING_CONFIGURATION = "SURCH_8001";
        public const string INVALID_CONFIGURATION = "SURCH_8002";
        public const string ENVIRONMENT_MISMATCH = "SURCH_8003";
        public const string FEATURE_FLAG_DISABLED = "SURCH_8004";
        public const string SERVICE_DEPENDENCY_MISSING = "SURCH_8005";
        public const string SECRET_MANAGEMENT_ERROR = "SURCH_8006";
        public const string CONFIGURATION_RELOAD_FAILED = "SURCH_8007";
        public const string ENVIRONMENT_VARIABLE_MISSING = "SURCH_8008";
    }

    // System & Infrastructure Errors (9000-9999)
    public static class System
    {
        public const string INTERNAL_SERVER_ERROR = "SURCH_9001";
        public const string SERVICE_UNAVAILABLE = "SURCH_9002";
        public const string MEMORY_EXHAUSTION = "SURCH_9003";
        public const string CPU_OVERLOAD = "SURCH_9004";
        public const string DISK_SPACE_FULL = "SURCH_9005";
        public const string NETWORK_ERROR = "SURCH_9006";
        public const string LOAD_BALANCER_ERROR = "SURCH_9007";
        public const string CACHE_ERROR = "SURCH_9008";
        public const string QUEUE_OVERFLOW = "SURCH_9009";
        public const string THREAD_POOL_EXHAUSTION = "SURCH_9010";
        public const string GARBAGE_COLLECTION_ERROR = "SURCH_9011";
        public const string CLOCK_SYNC_ERROR = "SURCH_9012";
        public const string FILE_SYSTEM_ERROR = "SURCH_9013";
        public const string PROCESS_CRASH = "SURCH_9014";
        public const string OUT_OF_MEMORY = "SURCH_9015";
        public const string INVALID_REQUEST = "SURCH_9016";
    }

    // Business Logic Errors (10000-10999)
    public static class Business
    {
        public const string SURCHARGE_CALCULATION_FAILED = "SURCH_10001";
        public const string SURCHARGE_NOT_SUPPORTED = "SURCH_10002";
        public const string SURCHARGE_AMOUNT_TOO_SMALL = "SURCH_10003";
        public const string SURCHARGE_AMOUNT_TOO_LARGE = "SURCH_10004";
        public const string SURCHARGE_COUNTRY_NOT_SUPPORTED = "SURCH_10005";
        public const string SURCHARGE_CARD_TYPE_NOT_SUPPORTED = "SURCH_10006";
        public const string SURCHARGE_MERCHANT_NOT_SUPPORTED = "SURCH_10007";
        public const string SURCHARGE_PROVIDER_NOT_SUPPORTED = "SURCH_10008";
        public const string SURCHARGE_TRANSACTION_TYPE_NOT_SUPPORTED = "SURCH_10009";
        public const string SURCHARGE_ENTRY_METHOD_NOT_SUPPORTED = "SURCH_10010";
        public const string SURCHARGE_POSTAL_CODE_NOT_SUPPORTED = "SURCH_10011";
        public const string SURCHARGE_BIN_NOT_SUPPORTED = "SURCH_10012";
        public const string SURCHARGE_PROCESSOR_NOT_SUPPORTED = "SURCH_10013";
        public const string SURCHARGE_CAMPAIGN_NOT_SUPPORTED = "SURCH_10014";
        public const string SURCHARGE_DATA_NOT_SUPPORTED = "SURCH_10015";
        public const string SURCHARGE_CARD_TOKEN_NOT_SUPPORTED = "SURCH_10016";
    }

    // Onboarding, Merchant, ApiKey Errors (11000-11999)
    public static class Onboarding
    {
        public const string AUDIT_TRAIL_FAILED = "SURCH_11001";
        public const string MERCHANT_NOT_FOUND = "SURCH_11002";
        public const string AUTHENTICATION_FAILED = "SURCH_11003";
        public const string INVALID_MERCHANT_ID_HEADER = "SURCH_11004";
        public const string MISSING_API_KEY_HEADER = "SURCH_11005";
        public const string INVALID_OR_INACTIVE_API_KEY = "SURCH_11006";
        public const string API_KEY_UPDATE_FAILED = "SURCH_11007";
        public const string MERCHANT_CREATE_FAILED = "SURCH_11008";
        public const string API_KEY_GENERATE_FAILED = "SURCH_11009";
        public const string API_KEY_ROTATE_FAILED = "SURCH_11010";
        public const string METADATA_PARSE_FAILED = "SURCH_11011";
        public const string MERCHANT_NOT_FOUND_EXTERNAL_ID = "SURCH_11012";
        public const string API_KEY_GENERIC_ERROR = "SURCH_11013";
        public const string API_KEY_NOT_FOUND = "SURCH_11014";
    }

    // Provider Validation Errors (12000-12999)
    public static class ProviderValidation
    {
        public const string PROVIDER_CODE_NULL_OR_EMPTY = "SURCH_12001";
        public const string PROVIDER_CODE_NOT_SUPPORTED_OR_INACTIVE = "SURCH_12002";
    }

    // InterPayments/External Provider Specific Errors (13000-13999)
    public static class InterPayments
    {
        public const string SEND_REQUEST_FAILED = "SURCH_13001";
        public const string API_ERROR = "SURCH_13002";
        public const string MISSING_JWT_TOKEN = "SURCH_13003";
        public const string POSTAL_CODE_REQUIRED = "SURCH_13004";
        public const string POSTAL_CODE_INVALID_US = "SURCH_13005";
        public const string POSTAL_CODE_INVALID_CANADA = "SURCH_13006";
    }

    /// <summary>
    /// Get error message for a given error code
    /// </summary>
    public static string GetErrorMessage(string errorCode)
    {
        return errorCode switch
        {
            // Authentication & Authorization
            Auth.INVALID_API_KEY => "Invalid API key provided",
            Auth.INVALID_MERCHANT_ID => "Invalid merchant ID provided",
            Auth.MERCHANT_NOT_FOUND => "Merchant not found",
            Auth.MERCHANT_INACTIVE => "Merchant account is inactive",
            Auth.INSUFFICIENT_PERMISSIONS => "Insufficient permissions for this operation",
            Auth.INVALID_SIGNATURE => "Invalid request signature",
            Auth.EXPIRED_API_KEY => "API key has expired",
            Auth.RATE_LIMIT_EXCEEDED => "Rate limit exceeded",

            // Provider Configuration
            Provider.PROVIDER_NOT_FOUND => "Surcharge provider not found",
            Provider.PROVIDER_INACTIVE => "Surcharge provider is inactive",
            Provider.PROVIDER_CODE_INVALID => "Invalid provider code format",
            Provider.PROVIDER_CONFIG_MISSING => "Provider configuration is missing",
            Provider.PROVIDER_CREDENTIALS_INVALID => "Provider credentials are invalid",
            Provider.PROVIDER_CREDENTIALS_EXPIRED => "Provider credentials have expired",
            Provider.PROVIDER_BASE_URL_INVALID => "Provider base URL is invalid",
            Provider.PROVIDER_AUTHENTICATION_FAILED => "Provider authentication failed",
            Provider.PROVIDER_NOT_AVAILABLE => "Provider is not available",
            Provider.PROVIDER_RATE_LIMIT_EXCEEDED => "Provider rate limit exceeded",
            Provider.PROVIDER_LIMIT_EXCEEDED => "Provider limit exceeded",

            // Request Validation
            Validation.INVALID_AMOUNT => "Invalid transaction amount",
            Validation.INVALID_COUNTRY => "Invalid country code",
            Validation.INVALID_CORRELATION_ID => "Invalid correlation ID",
            Validation.INVALID_MERCHANT_TRANSACTION_ID => "Invalid merchant transaction ID",
            Validation.INVALID_PROVIDER_CODE => "Invalid provider code format",
            Validation.INVALID_POSTAL_CODE => "Invalid postal code",
            Validation.INVALID_BIN_VALUE => "Invalid BIN value",
            Validation.INVALID_SURCHARGE_PROCESSOR => "Invalid surcharge processor identifier",
            Validation.INVALID_ENTRY_METHOD => "Invalid entry method",
            Validation.INVALID_CARD_TOKEN => "Invalid card token",
            Validation.INVALID_CAMPAIGN => "Invalid campaign identifier",
            Validation.INVALID_DATA => "Invalid data provided",
            Validation.INVALID_NON_SURCHARGEABLE_AMOUNT => "Invalid non-surchargable amount",
            Validation.INVALID_PROVIDER_TRANSACTION_ID => "Invalid provider transaction ID",
            Validation.INVALID_TOTAL_AMOUNT => "Invalid total amount",

            // Transaction Processing
            Transaction.DUPLICATE_TRANSACTION => "Duplicate transaction detected",
            Transaction.TRANSACTION_NOT_FOUND => "Transaction not found",
            Transaction.TRANSACTION_ALREADY_PROCESSED => "Transaction already processed",
            Transaction.TRANSACTION_EXPIRED => "Transaction has expired",
            Transaction.TRANSACTION_CANCELLED => "Transaction was cancelled",
            Transaction.TRANSACTION_FAILED => "Transaction processing failed",
            Transaction.INVALID_TRANSACTION_STATE => "Invalid transaction state",
            Transaction.TRANSACTION_TIMEOUT => "Transaction processing timeout",
            Transaction.TRANSACTION_ROLLBACK_FAILED => "Transaction rollback failed",
            Transaction.TRANSACTION_IDEMPOTENCY_VIOLATION => "Idempotency violation detected",

            // External Provider
            ExternalProvider.PROVIDER_CONNECTION_FAILED => "Failed to connect to external provider",
            ExternalProvider.PROVIDER_TIMEOUT => "External provider request timeout",
            ExternalProvider.PROVIDER_SERVICE_UNAVAILABLE => "External provider service unavailable",
            ExternalProvider.PROVIDER_INVALID_RESPONSE => "Invalid response from external provider",
            ExternalProvider.PROVIDER_AUTHENTICATION_ERROR => "External provider authentication error",
            ExternalProvider.PROVIDER_RATE_LIMIT_HIT => "External provider rate limit hit",
            ExternalProvider.PROVIDER_INVALID_REQUEST => "Invalid request to external provider",
            ExternalProvider.PROVIDER_BUSINESS_RULE_VIOLATION => "External provider business rule violation",
            ExternalProvider.PROVIDER_SYSTEM_ERROR => "External provider system error",
            ExternalProvider.PROVIDER_MAINTENANCE_MODE => "External provider in maintenance mode",
            ExternalProvider.PROVIDER_CERTIFICATE_ERROR => "External provider certificate error",
            ExternalProvider.PROVIDER_SSL_ERROR => "External provider SSL error",
            ExternalProvider.PROVIDER_DNS_ERROR => "External provider DNS error",
            ExternalProvider.PROVIDER_GATEWAY_ERROR => "External provider gateway error",

            // Database
            Database.DATABASE_CONNECTION_FAILED => "Database connection failed",
            Database.DATABASE_TIMEOUT => "Database operation timeout",
            Database.DATABASE_DEADLOCK => "Database deadlock detected",
            Database.DATABASE_CONSTRAINT_VIOLATION => "Database constraint violation",
            Database.DATABASE_TRANSACTION_FAILED => "Database transaction failed",
            Database.DATABASE_BACKUP_ERROR => "Database backup error",
            Database.DATABASE_MAINTENANCE_MODE => "Database in maintenance mode",
            Database.DATABASE_QUOTA_EXCEEDED => "Database quota exceeded",
            Database.DATABASE_INDEX_ERROR => "Database index error",
            Database.DATABASE_SCHEMA_ERROR => "Database schema error",

            // Audit
            Audit.AUDIT_LOG_FAILED => "Audit logging failed",
            Audit.AUDIT_DATA_CORRUPTION => "Audit data corruption detected",
            Audit.AUDIT_STORAGE_FULL => "Audit storage is full",
            Audit.AUDIT_RETENTION_VIOLATION => "Audit retention policy violation",
            Audit.AUDIT_ENCRYPTION_ERROR => "Audit encryption error",
            Audit.AUDIT_COMPLIANCE_VIOLATION => "Audit compliance violation",

            // Configuration
            Configuration.MISSING_CONFIGURATION => "Required configuration is missing",
            Configuration.INVALID_CONFIGURATION => "Invalid configuration detected",
            Configuration.ENVIRONMENT_MISMATCH => "Environment configuration mismatch",
            Configuration.FEATURE_FLAG_DISABLED => "Feature flag is disabled",
            Configuration.SERVICE_DEPENDENCY_MISSING => "Required service dependency is missing",
            Configuration.SECRET_MANAGEMENT_ERROR => "Secret management error",
            Configuration.CONFIGURATION_RELOAD_FAILED => "Configuration reload failed",
            Configuration.ENVIRONMENT_VARIABLE_MISSING => "Required environment variable is missing",

            // System
            System.INTERNAL_SERVER_ERROR => "Internal server error",
            System.SERVICE_UNAVAILABLE => "Service is temporarily unavailable",
            System.MEMORY_EXHAUSTION => "System memory exhaustion",
            System.CPU_OVERLOAD => "System CPU overload",
            System.DISK_SPACE_FULL => "System disk space is full",
            System.NETWORK_ERROR => "Network connectivity error",
            System.LOAD_BALANCER_ERROR => "Load balancer error",
            System.CACHE_ERROR => "Cache operation error",
            System.QUEUE_OVERFLOW => "Queue overflow detected",
            System.THREAD_POOL_EXHAUSTION => "Thread pool exhaustion",
            System.GARBAGE_COLLECTION_ERROR => "Garbage collection error",
            System.CLOCK_SYNC_ERROR => "Clock synchronization error",
            System.FILE_SYSTEM_ERROR => "File system error",
            System.PROCESS_CRASH => "Process crash detected",
            System.OUT_OF_MEMORY => "Out of memory error",
            System.INVALID_REQUEST => "Invalid request detected",

            // Business Logic
            Business.SURCHARGE_CALCULATION_FAILED => "Surcharge calculation failed",
            Business.SURCHARGE_NOT_SUPPORTED => "Surcharge not supported for this transaction",
            Business.SURCHARGE_AMOUNT_TOO_SMALL => "Calculated surcharge amount is too small",
            Business.SURCHARGE_AMOUNT_TOO_LARGE => "Calculated surcharge amount is too large",
            Business.SURCHARGE_COUNTRY_NOT_SUPPORTED => "Surcharge not supported for this country",
            Business.SURCHARGE_CARD_TYPE_NOT_SUPPORTED => "Surcharge not supported for this card type",
            Business.SURCHARGE_MERCHANT_NOT_SUPPORTED => "Surcharge not supported for this merchant",
            Business.SURCHARGE_PROVIDER_NOT_SUPPORTED => "Surcharge not supported for this provider",
            Business.SURCHARGE_TRANSACTION_TYPE_NOT_SUPPORTED => "Surcharge not supported for this transaction type",
            Business.SURCHARGE_ENTRY_METHOD_NOT_SUPPORTED => "Surcharge not supported for this entry method",
            Business.SURCHARGE_POSTAL_CODE_NOT_SUPPORTED => "Surcharge not supported for this postal code",
            Business.SURCHARGE_BIN_NOT_SUPPORTED => "Surcharge not supported for this BIN",
            Business.SURCHARGE_PROCESSOR_NOT_SUPPORTED => "Surcharge not supported for this processor",
            Business.SURCHARGE_CAMPAIGN_NOT_SUPPORTED => "Surcharge not supported for this campaign",
            Business.SURCHARGE_DATA_NOT_SUPPORTED => "Surcharge not supported for this data",
            Business.SURCHARGE_CARD_TOKEN_NOT_SUPPORTED => "Surcharge not supported for this card token",

            // Onboarding
            Onboarding.AUDIT_TRAIL_FAILED => "Audit trail generation failed",
            Onboarding.MERCHANT_NOT_FOUND => "Merchant not found during onboarding",
            Onboarding.AUTHENTICATION_FAILED => "Authentication failed during onboarding",
            Onboarding.INVALID_MERCHANT_ID_HEADER => "Invalid or missing Merchant-Id header",
            Onboarding.MISSING_API_KEY_HEADER => "Missing API-Key header",
            Onboarding.INVALID_OR_INACTIVE_API_KEY => "Invalid or inactive API key",
            Onboarding.API_KEY_UPDATE_FAILED => "API key update failed",
            Onboarding.MERCHANT_CREATE_FAILED => "Merchant creation failed",
            Onboarding.API_KEY_GENERATE_FAILED => "API key generation failed",
            Onboarding.API_KEY_ROTATE_FAILED => "API key rotation failed",
            Onboarding.METADATA_PARSE_FAILED => "Merchant metadata parsing failed",
            Onboarding.MERCHANT_NOT_FOUND_EXTERNAL_ID => "Merchant not found by external ID",
            Onboarding.API_KEY_GENERIC_ERROR => "Generic API key error during onboarding",
            Onboarding.API_KEY_NOT_FOUND => "API key not found",

            // Provider Validation
            ProviderValidation.PROVIDER_CODE_NULL_OR_EMPTY => "Provider code is null or empty",
            ProviderValidation.PROVIDER_CODE_NOT_SUPPORTED_OR_INACTIVE => "Provider code is not supported or inactive",

            // InterPayments
            InterPayments.SEND_REQUEST_FAILED => "Failed to send request to InterPayments",
            InterPayments.API_ERROR => "InterPayments API returned an error",
            InterPayments.MISSING_JWT_TOKEN => "JWT token is missing for InterPayments request",
            InterPayments.POSTAL_CODE_REQUIRED => "Postal code is required for InterPayments",
            InterPayments.POSTAL_CODE_INVALID_US => "Invalid US postal code format",
            InterPayments.POSTAL_CODE_INVALID_CANADA => "Invalid Canadian postal code format",

            _ => "Unknown error occurred"
        };
    }

    /// <summary>
    /// Get error category for a given error code
    /// </summary>
    public static string GetErrorCategory(string errorCode)
    {
        if (errorCode.StartsWith("SURCH_1")) return "Authentication";
        if (errorCode.StartsWith("SURCH_2")) return "Provider";
        if (errorCode.StartsWith("SURCH_3")) return "Validation";
        if (errorCode.StartsWith("SURCH_4")) return "Transaction";
        if (errorCode.StartsWith("SURCH_5")) return "ExternalProvider";
        if (errorCode.StartsWith("SURCH_6")) return "Database";
        if (errorCode.StartsWith("SURCH_7")) return "Audit";
        if (errorCode.StartsWith("SURCH_8")) return "Configuration";
        if (errorCode.StartsWith("SURCH_9")) return "System";
        if (errorCode.StartsWith("SURCH_10")) return "Business";
        if (errorCode.StartsWith("SURCH_11")) return "Onboarding";
        if (errorCode.StartsWith("SURCH_12")) return "ProviderValidation";
        if (errorCode.StartsWith("SURCH_13")) return "InterPayments";
        
        return "Unknown";
    }
} 