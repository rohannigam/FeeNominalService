using System.Collections.Generic;

namespace FeeNominalService.Models.Common
{
    /// <summary>
    /// Standardized API error response model
    /// </summary>
    public class ApiErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string? Details { get; set; }
        public List<string>? Errors { get; set; }
        public Dictionary<string, object>? Context { get; set; }

        public ApiErrorResponse(string message, string errorCode, string? details = null)
        {
            Message = message;
            ErrorCode = errorCode;
            Details = details;
        }

        public ApiErrorResponse(string message, string errorCode, List<string> errors)
        {
            Message = message;
            ErrorCode = errorCode;
            Errors = errors;
        }

        public static ApiErrorResponse ProviderLimitExceeded(int maxProviders, int currentCount)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_LIMIT_EXCEEDED),
                SurchargeErrorCodes.Provider.PROVIDER_LIMIT_EXCEEDED,
                $"Current count: {currentCount}, Max allowed: {maxProviders}"
            );
        }

        public static ApiErrorResponse ProviderCodeExists(string code)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_CODE_INVALID),
                SurchargeErrorCodes.Provider.PROVIDER_CODE_INVALID,
                $"Provider code: {code}"
            );
        }

        public static ApiErrorResponse ProviderNotFound(string id)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND),
                SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND,
                $"Provider ID: {id}"
            );
        }

        public static ApiErrorResponse InvalidCredentialsSchema(List<string> errors)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_CREDENTIALS_INVALID),
                SurchargeErrorCodes.Provider.PROVIDER_CREDENTIALS_INVALID,
                errors
            );
        }

        public static ApiErrorResponse InvalidConfiguration(List<string> errors)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Configuration.INVALID_CONFIGURATION),
                SurchargeErrorCodes.Configuration.INVALID_CONFIGURATION,
                errors
            );
        }

        public static ApiErrorResponse InvalidCredentials(List<string> errors)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_CREDENTIALS_INVALID),
                SurchargeErrorCodes.Provider.PROVIDER_CREDENTIALS_INVALID,
                errors
            );
        }

        public static ApiErrorResponse InvalidStatusCode(string statusCode)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Provider.PROVIDER_CONFIG_MISSING),
                SurchargeErrorCodes.Provider.PROVIDER_CONFIG_MISSING,
                $"Status code: {statusCode}"
            );
        }

        public static ApiErrorResponse SystemConfigurationError(string details)
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Configuration.MISSING_CONFIGURATION),
                SurchargeErrorCodes.Configuration.MISSING_CONFIGURATION,
                details
            );
        }

        public static ApiErrorResponse InternalServerError()
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.System.INTERNAL_SERVER_ERROR),
                SurchargeErrorCodes.System.INTERNAL_SERVER_ERROR
            );
        }

        public static ApiErrorResponse UnauthorizedAccess()
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Auth.INSUFFICIENT_PERMISSIONS),
                SurchargeErrorCodes.Auth.INSUFFICIENT_PERMISSIONS
            );
        }

        public static ApiErrorResponse MerchantIdMismatch()
        {
            return new ApiErrorResponse(
                SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.Auth.INVALID_MERCHANT_ID),
                SurchargeErrorCodes.Auth.INVALID_MERCHANT_ID
            );
        }
    }
} 