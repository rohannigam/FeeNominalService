using System.Text.Json;

namespace FeeNominalService.Services
{
    /// <summary>
    /// Service for validating credential formats and content
    /// </summary>
    public interface ICredentialValidationService
    {
        /// <summary>
        /// Validates JWT token format
        /// </summary>
        /// <param name="jwtToken">The JWT token to validate</param>
        /// <returns>True if valid JWT format, false otherwise</returns>
        bool ValidateJwtFormat(string jwtToken);

        /// <summary>
        /// Validates API key format
        /// </summary>
        /// <param name="apiKey">The API key to validate</param>
        /// <returns>True if valid API key format, false otherwise</returns>
        bool ValidateApiKeyFormat(string apiKey);

        /// <summary>
        /// Validates email format
        /// </summary>
        /// <param name="email">The email to validate</param>
        /// <returns>True if valid email format, false otherwise</returns>
        bool ValidateEmailFormat(string email);

        /// <summary>
        /// Validates URL format
        /// </summary>
        /// <param name="url">The URL to validate</param>
        /// <returns>True if valid URL format, false otherwise</returns>
        bool ValidateUrlFormat(string url);

        /// <summary>
        /// Validates credential value based on field type
        /// </summary>
        /// <param name="fieldType">The type of credential field</param>
        /// <param name="value">The credential value to validate</param>
        /// <returns>True if valid for the field type, false otherwise</returns>
        bool ValidateCredentialValue(string fieldType, string value);

        /// <summary>
        /// Validates credentials object size and content
        /// </summary>
        /// <param name="credentials">The credentials object to validate</param>
        /// <param name="maxSize">Maximum allowed size in characters</param>
        /// <param name="maxValueLength">Maximum length for individual values</param>
        /// <param name="minValueLength">Minimum length for individual values</param>
        /// <returns>Validation result with any errors</returns>
        (bool IsValid, List<string> Errors) ValidateCredentialsObject(JsonDocument credentials, int maxSize, int maxValueLength, int minValueLength);
    }
} 