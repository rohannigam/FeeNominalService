using FeeNominalService.Models.Surcharge;

namespace FeeNominalService.Exceptions;

/// <summary>
/// Custom exception for surcharge-related operations
/// </summary>
public class SurchargeException : Exception
{
    /// <summary>
    /// Error code for the exception
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Error category
    /// </summary>
    public string ErrorCategory { get; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> Context { get; }

    /// <summary>
    /// Whether the error is retryable
    /// </summary>
    public bool IsRetryable { get; set; }

    /// <summary>
    /// Whether the error should be logged
    /// </summary>
    public bool ShouldLog { get; set; }

    public SurchargeException(string errorCode, string? message = null, Exception? innerException = null) 
        : base(message ?? SurchargeErrorCodes.GetErrorMessage(errorCode), innerException)
    {
        ErrorCode = errorCode;
        ErrorCategory = SurchargeErrorCodes.GetErrorCategory(errorCode);
        Context = new Dictionary<string, object>();
        IsRetryable = IsRetryableError(errorCode);
        ShouldLog = true;
    }

    public SurchargeException(string errorCode, Dictionary<string, object> context, string? message = null, Exception? innerException = null) 
        : base(message ?? SurchargeErrorCodes.GetErrorMessage(errorCode), innerException)
    {
        ErrorCode = errorCode;
        ErrorCategory = SurchargeErrorCodes.GetErrorCategory(errorCode);
        Context = context ?? new Dictionary<string, object>();
        IsRetryable = IsRetryableError(errorCode);
        ShouldLog = true;
    }

    /// <summary>
    /// Add context information to the exception
    /// </summary>
    public SurchargeException AddContext(string key, object? value)
    {
        Context[key] = value ?? "NULL";
        return this;
    }

    /// <summary>
    /// Set whether the error is retryable
    /// </summary>
    public SurchargeException SetRetryable(bool isRetryable)
    {
        IsRetryable = isRetryable;
        return this;
    }

    /// <summary>
    /// Set whether the error should be logged
    /// </summary>
    public SurchargeException SetShouldLog(bool shouldLog)
    {
        ShouldLog = shouldLog;
        return this;
    }

    /// <summary>
    /// Determine if an error code represents a retryable error
    /// </summary>
    private static bool IsRetryableError(string errorCode)
    {
        return errorCode switch
        {
            // Retryable errors
            SurchargeErrorCodes.ExternalProvider.PROVIDER_CONNECTION_FAILED => true,
            SurchargeErrorCodes.ExternalProvider.PROVIDER_TIMEOUT => true,
            SurchargeErrorCodes.ExternalProvider.PROVIDER_SERVICE_UNAVAILABLE => true,
            SurchargeErrorCodes.ExternalProvider.PROVIDER_RATE_LIMIT_HIT => true,
            SurchargeErrorCodes.ExternalProvider.PROVIDER_MAINTENANCE_MODE => true,
            SurchargeErrorCodes.ExternalProvider.PROVIDER_GATEWAY_ERROR => true,
            SurchargeErrorCodes.Database.DATABASE_CONNECTION_FAILED => true,
            SurchargeErrorCodes.Database.DATABASE_TIMEOUT => true,
            SurchargeErrorCodes.Database.DATABASE_DEADLOCK => true,
            SurchargeErrorCodes.System.SERVICE_UNAVAILABLE => true,
            SurchargeErrorCodes.System.NETWORK_ERROR => true,
            SurchargeErrorCodes.System.LOAD_BALANCER_ERROR => true,
            SurchargeErrorCodes.System.CACHE_ERROR => true,
            SurchargeErrorCodes.System.QUEUE_OVERFLOW => true,
            SurchargeErrorCodes.System.THREAD_POOL_EXHAUSTION => true,
            SurchargeErrorCodes.Auth.RATE_LIMIT_EXCEEDED => true,
            SurchargeErrorCodes.Provider.PROVIDER_RATE_LIMIT_EXCEEDED => true,
            SurchargeErrorCodes.Transaction.TRANSACTION_TIMEOUT => true,

            // Non-retryable errors
            _ => false
        };
    }

    /// <summary>
    /// Create a formatted error response
    /// </summary>
    public object ToErrorResponse()
    {
        return new
        {
            error = new
            {
                code = ErrorCode,
                category = ErrorCategory,
                message = Message,
                retryable = IsRetryable,
                context = Context.Count > 0 ? Context : null,
                timestamp = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Create a surcharge exception for provider not found
    /// </summary>
    public static SurchargeException ProviderNotFound(string providerCode, Guid merchantId)
    {
        return new SurchargeException(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND)
            .AddContext("ProviderCode", providerCode)
            .AddContext("MerchantId", merchantId);
    }

    /// <summary>
    /// Create a surcharge exception for provider inactive
    /// </summary>
    public static SurchargeException ProviderInactive(string providerCode, Guid merchantId)
    {
        return new SurchargeException(SurchargeErrorCodes.Provider.PROVIDER_INACTIVE)
            .AddContext("ProviderCode", providerCode)
            .AddContext("MerchantId", merchantId);
    }

    /// <summary>
    /// Create a surcharge exception for duplicate transaction
    /// </summary>
    public static SurchargeException DuplicateTransaction(string correlationId, Guid merchantId)
    {
        return new SurchargeException(SurchargeErrorCodes.Transaction.DUPLICATE_TRANSACTION)
            .AddContext("CorrelationId", correlationId)
            .AddContext("MerchantId", merchantId)
            .SetRetryable(false);
    }

    /// <summary>
    /// Create a surcharge exception for external provider connection failure
    /// </summary>
    public static SurchargeException ProviderConnectionFailed(string providerCode, string baseUrl, Exception? innerException = null)
    {
        return new SurchargeException(SurchargeErrorCodes.ExternalProvider.PROVIDER_CONNECTION_FAILED, innerException: innerException)
            .AddContext("ProviderCode", providerCode)
            .AddContext("BaseUrl", baseUrl)
            .SetRetryable(true);
    }

    /// <summary>
    /// Create a surcharge exception for external provider timeout
    /// </summary>
    public static SurchargeException ProviderTimeout(string providerCode, string baseUrl, int timeoutSeconds)
    {
        return new SurchargeException(SurchargeErrorCodes.ExternalProvider.PROVIDER_TIMEOUT)
            .AddContext("ProviderCode", providerCode)
            .AddContext("BaseUrl", baseUrl)
            .AddContext("TimeoutSeconds", timeoutSeconds)
            .SetRetryable(true);
    }

    /// <summary>
    /// Create a surcharge exception for invalid request validation
    /// </summary>
    public static SurchargeException InvalidRequest(string fieldName, string fieldValue, string? reason = null)
    {
        var errorCode = MapFieldNameToErrorCode(fieldName);

        return new SurchargeException(errorCode)
            .AddContext("FieldName", fieldName)
            .AddContext("FieldValue", fieldValue)
            .AddContext("Reason", reason)
            .SetRetryable(false);
    }

    /// <summary>
    /// Create a surcharge exception for surcharge calculation failure
    /// </summary>
    public static SurchargeException SurchargeCalculationFailed(string providerCode, decimal amount, Exception? innerException = null)
    {
        return new SurchargeException(SurchargeErrorCodes.Business.SURCHARGE_CALCULATION_FAILED, innerException: innerException)
            .AddContext("ProviderCode", providerCode)
            .AddContext("Amount", amount)
            .SetRetryable(true);
    }

    private static string MapFieldNameToErrorCode(string fieldName)
    {
        return fieldName.ToUpperInvariant() switch
        {
            "AMOUNT" => SurchargeErrorCodes.Validation.INVALID_AMOUNT,
            "COUNTRY" => SurchargeErrorCodes.Validation.INVALID_COUNTRY,
            "CORRELATIONID" => SurchargeErrorCodes.Validation.INVALID_CORRELATION_ID,
            "MERCHANTTRANSACTIONID" => SurchargeErrorCodes.Validation.INVALID_MERCHANT_TRANSACTION_ID,
            "PROVIDERCODE" => SurchargeErrorCodes.Validation.INVALID_PROVIDER_CODE,
            "POSTALCODE" => SurchargeErrorCodes.Validation.INVALID_POSTAL_CODE,
            "BINVALUE" => SurchargeErrorCodes.Validation.INVALID_BIN_VALUE,
            "SURCHARGEPROCESSOR" => SurchargeErrorCodes.Validation.INVALID_SURCHARGE_PROCESSOR,
            "ENTRYMETHOD" => SurchargeErrorCodes.Validation.INVALID_ENTRY_METHOD,
            "CARDTOKEN" => SurchargeErrorCodes.Validation.INVALID_CARD_TOKEN,
            "CAMPAIGN" => SurchargeErrorCodes.Validation.INVALID_CAMPAIGN,
            "DATA" => SurchargeErrorCodes.Validation.INVALID_DATA,
            "NONSURCHARGEABLEAMOUNT" => SurchargeErrorCodes.Validation.INVALID_NON_SURCHARGEABLE_AMOUNT,
            "PROVIDERTRANSACTIONID" => SurchargeErrorCodes.Validation.INVALID_PROVIDER_TRANSACTION_ID,
            "TOTALAMOUNT" => SurchargeErrorCodes.Validation.INVALID_TOTAL_AMOUNT,
            _ => SurchargeErrorCodes.Validation.INVALID_AMOUNT // Default fallback
        };
    }
} 