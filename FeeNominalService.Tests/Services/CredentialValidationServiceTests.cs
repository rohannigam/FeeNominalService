using FeeNominalService.Services;
using FeeNominalService.Settings;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace FeeNominalService.Tests.Services
{
    public class CredentialValidationServiceTests
    {
        private readonly Mock<ILogger<CredentialValidationService>> _mockLogger;
        private readonly SurchargeProviderValidationSettings _settings;
        private readonly CredentialValidationService _service;

        public CredentialValidationServiceTests()
        {
            _mockLogger = new Mock<ILogger<CredentialValidationService>>();
            
            _settings = new SurchargeProviderValidationSettings
            {
                ValidateJwtFormat = true,
                ValidateApiKeyFormat = true,
                ValidateEmailFormat = true,
                ValidateUrlFormat = true,
                MinCredentialValueLength = 1,
                MaxCredentialValueLength = 10000
            };

            _service = new CredentialValidationService(_mockLogger.Object, _settings);
        }

        #region ValidateJwtFormat Tests

        [Fact]
        public void ValidateJwtFormat_WithValidJwt_ReturnsTrue()
        {
            // Arrange
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var result = _service.ValidateJwtFormat(jwt);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateJwtFormat_WithInvalidJwtParts_ReturnsFalse()
        {
            // Arrange
            var jwt = "invalid.jwt"; // Only 2 parts

            // Act
            var result = _service.ValidateJwtFormat(jwt);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateJwtFormat_WithInvalidCharacters_ReturnsFalse()
        {
            // Arrange
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c!";

            // Act
            var result = _service.ValidateJwtFormat(jwt);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateJwtFormat_WithEmptyParts_ReturnsFalse()
        {
            // Arrange
            var jwt = ".."; // Empty parts

            // Act
            var result = _service.ValidateJwtFormat(jwt);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateJwtFormat_WithNullJwt_ReturnsFalse()
        {
            // Arrange
            string? jwt = null;

            // Act
            var result = _service.ValidateJwtFormat(jwt!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateJwtFormat_WithWhitespaceJwt_ReturnsFalse()
        {
            // Arrange
            var jwt = "   ";

            // Act
            var result = _service.ValidateJwtFormat(jwt);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateJwtFormat_WhenValidationDisabled_ReturnsTrue()
        {
            // Arrange
            var settings = new SurchargeProviderValidationSettings { ValidateJwtFormat = false };
            var service = new CredentialValidationService(_mockLogger.Object, settings);
            var jwt = "invalid.jwt";

            // Act
            var result = service.ValidateJwtFormat(jwt);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region ValidateApiKeyFormat Tests

        [Fact]
        public void ValidateApiKeyFormat_WithValidApiKey_ReturnsTrue()
        {
            // Arrange
            var apiKey = "sk_test_1234567890abcdef1234567890abcdef";

            // Act
            var result = _service.ValidateApiKeyFormat(apiKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateApiKeyFormat_WithShortApiKey_ReturnsFalse()
        {
            // Arrange
            var apiKey = "short"; // Less than 16 characters

            // Act
            var result = _service.ValidateApiKeyFormat(apiKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateApiKeyFormat_WithInvalidCharacters_ReturnsFalse()
        {
            // Arrange
            var apiKey = "sk_test_1234567890abcdef1234567890abcdef!";

            // Act
            var result = _service.ValidateApiKeyFormat(apiKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateApiKeyFormat_WithNullApiKey_ReturnsFalse()
        {
            // Arrange
            string? apiKey = null;

            // Act
            var result = _service.ValidateApiKeyFormat(apiKey!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateApiKeyFormat_WithWhitespaceApiKey_ReturnsFalse()
        {
            // Arrange
            var apiKey = "   ";

            // Act
            var result = _service.ValidateApiKeyFormat(apiKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateApiKeyFormat_WhenValidationDisabled_ReturnsTrue()
        {
            // Arrange
            var settings = new SurchargeProviderValidationSettings { ValidateApiKeyFormat = false };
            var service = new CredentialValidationService(_mockLogger.Object, settings);
            var apiKey = "invalid!";

            // Act
            var result = service.ValidateApiKeyFormat(apiKey);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region ValidateEmailFormat Tests

        [Fact]
        public void ValidateEmailFormat_WithValidEmail_ReturnsTrue()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = _service.ValidateEmailFormat(email);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateEmailFormat_WithInvalidEmail_ReturnsFalse()
        {
            // Arrange
            var email = "invalid-email";

            // Act
            var result = _service.ValidateEmailFormat(email);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateEmailFormat_WithEmailMissingDomain_ReturnsFalse()
        {
            // Arrange
            var email = "test@";

            // Act
            var result = _service.ValidateEmailFormat(email);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateEmailFormat_WithEmailMissingAtSymbol_ReturnsFalse()
        {
            // Arrange
            var email = "testexample.com";

            // Act
            var result = _service.ValidateEmailFormat(email);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateEmailFormat_WithNullEmail_ReturnsFalse()
        {
            // Arrange
            string? email = null;

            // Act
            var result = _service.ValidateEmailFormat(email!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateEmailFormat_WithWhitespaceEmail_ReturnsFalse()
        {
            // Arrange
            var email = "   ";

            // Act
            var result = _service.ValidateEmailFormat(email);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateEmailFormat_WhenValidationDisabled_ReturnsTrue()
        {
            // Arrange
            var settings = new SurchargeProviderValidationSettings { ValidateEmailFormat = false };
            var service = new CredentialValidationService(_mockLogger.Object, settings);
            var email = "invalid-email";

            // Act
            var result = service.ValidateEmailFormat(email);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region ValidateUrlFormat Tests

        [Fact]
        public void ValidateUrlFormat_WithValidHttpUrl_ReturnsTrue()
        {
            // Arrange
            var url = "http://example.com";

            // Act
            var result = _service.ValidateUrlFormat(url);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateUrlFormat_WithValidHttpsUrl_ReturnsTrue()
        {
            // Arrange
            var url = "https://example.com/api/v1";

            // Act
            var result = _service.ValidateUrlFormat(url);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateUrlFormat_WithInvalidUrl_ReturnsFalse()
        {
            // Arrange
            var url = "ftp://example.com";

            // Act
            var result = _service.ValidateUrlFormat(url);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateUrlFormat_WithUrlMissingProtocol_ReturnsFalse()
        {
            // Arrange
            var url = "example.com";

            // Act
            var result = _service.ValidateUrlFormat(url);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateUrlFormat_WithNullUrl_ReturnsFalse()
        {
            // Arrange
            string? url = null;

            // Act
            var result = _service.ValidateUrlFormat(url!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateUrlFormat_WithWhitespaceUrl_ReturnsFalse()
        {
            // Arrange
            var url = "   ";

            // Act
            var result = _service.ValidateUrlFormat(url);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateUrlFormat_WhenValidationDisabled_ReturnsTrue()
        {
            // Arrange
            var settings = new SurchargeProviderValidationSettings { ValidateUrlFormat = false };
            var service = new CredentialValidationService(_mockLogger.Object, settings);
            var url = "invalid-url";

            // Act
            var result = service.ValidateUrlFormat(url);

            // Assert
            result.Should().BeTrue();
        }

        #endregion

        #region ValidateCredentialValue Tests

        [Fact]
        public void ValidateCredentialValue_WithValidJwt_ReturnsTrue()
        {
            // Arrange
            var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var result = _service.ValidateCredentialValue("jwt", jwt);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidApiKey_ReturnsTrue()
        {
            // Arrange
            var apiKey = "sk_test_1234567890abcdef1234567890abcdef";

            // Act
            var result = _service.ValidateCredentialValue("api_key", apiKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidEmail_ReturnsTrue()
        {
            // Arrange
            var email = "test@example.com";

            // Act
            var result = _service.ValidateCredentialValue("email", email);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidUrl_ReturnsTrue()
        {
            // Arrange
            var url = "https://example.com/api";

            // Act
            var result = _service.ValidateCredentialValue("url", url);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidUsername_ReturnsTrue()
        {
            // Arrange
            var username = "testuser";

            // Act
            var result = _service.ValidateCredentialValue("username", username);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidPassword_ReturnsTrue()
        {
            // Arrange
            var password = "password123";

            // Act
            var result = _service.ValidateCredentialValue("password", password);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithShortPassword_ReturnsFalse()
        {
            // Arrange
            var password = "123"; // Less than 8 characters

            // Act
            var result = _service.ValidateCredentialValue("password", password);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidCertificate_ReturnsTrue()
        {
            // Arrange
            var certificate = "-----BEGIN CERTIFICATE-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END CERTIFICATE-----";

            // Act
            var result = _service.ValidateCredentialValue("certificate", certificate);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithInvalidCertificate_ReturnsFalse()
        {
            // Arrange
            var certificate = "invalid certificate";

            // Act
            var result = _service.ValidateCredentialValue("certificate", certificate);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidPrivateKey_ReturnsTrue()
        {
            // Arrange
            var privateKey = "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC...\n-----END PRIVATE KEY-----";

            // Act
            var result = _service.ValidateCredentialValue("private_key", privateKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidPublicKey_ReturnsTrue()
        {
            // Arrange
            var publicKey = "-----BEGIN PUBLIC KEY-----\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA...\n-----END PUBLIC KEY-----";

            // Act
            var result = _service.ValidateCredentialValue("public_key", publicKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidBase64_ReturnsTrue()
        {
            // Arrange
            var base64 = "SGVsbG8gV29ybGQ=";

            // Act
            var result = _service.ValidateCredentialValue("base64", base64);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithInvalidBase64_ReturnsFalse()
        {
            // Arrange
            var base64 = "invalid-base64!";

            // Act
            var result = _service.ValidateCredentialValue("base64", base64);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithValidJson_ReturnsTrue()
        {
            // Arrange
            var json = "{\"key\": \"value\", \"number\": 123}";

            // Act
            var result = _service.ValidateCredentialValue("json", json);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithInvalidJson_ReturnsFalse()
        {
            // Arrange
            var json = "{\"key\": \"value\", \"number\": 123"; // Missing closing brace

            // Act
            var result = _service.ValidateCredentialValue("json", json);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithNullValue_ReturnsFalse()
        {
            // Arrange
            string? value = null;

            // Act
            var result = _service.ValidateCredentialValue("string", value!);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithWhitespaceValue_ReturnsFalse()
        {
            // Arrange
            var value = "   ";

            // Act
            var result = _service.ValidateCredentialValue("string", value);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithValueTooShort_ReturnsFalse()
        {
            // Arrange
            var settings = new SurchargeProviderValidationSettings { MinCredentialValueLength = 5 };
            var service = new CredentialValidationService(_mockLogger.Object, settings);
            var value = "abc"; // Less than minimum length

            // Act
            var result = service.ValidateCredentialValue("string", value);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithValueTooLong_ReturnsFalse()
        {
            // Arrange
            var settings = new SurchargeProviderValidationSettings { MaxCredentialValueLength = 10 };
            var service = new CredentialValidationService(_mockLogger.Object, settings);
            var value = "this is too long"; // More than maximum length

            // Act
            var result = service.ValidateCredentialValue("string", value);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCredentialValue_WithUnknownType_ReturnsTrue()
        {
            // Arrange
            var value = "some value";

            // Act
            var result = _service.ValidateCredentialValue("unknown_type", value);

            // Assert
            result.Should().BeTrue(); // Unknown types just check length
        }

        #endregion

        #region ValidateCredentialsObject Tests

        [Fact]
        public void ValidateCredentialsObject_WithValidCredentials_ReturnsValid()
        {
            // Arrange
            var credentialsJson = "{\"api_key\": \"sk_test_1234567890abcdef\", \"url\": \"https://api.example.com\"}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 100, 1);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateCredentialsObject_WithTooLargeObject_ReturnsInvalid()
        {
            // Arrange
            var credentialsJson = "{\"api_key\": \"sk_test_1234567890abcdef\", \"url\": \"https://api.example.com\"}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 10, 100, 1); // Max size 10

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors[0].Should().Contain("exceeds maximum allowed size");
        }

        [Fact]
        public void ValidateCredentialsObject_WithValueTooShort_ReturnsInvalid()
        {
            // Arrange
            var credentialsJson = "{\"api_key\": \"short\"}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 100, 10); // Min length 10

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors[0].Should().Contain("is below minimum");
        }

        [Fact]
        public void ValidateCredentialsObject_WithValueTooLong_ReturnsInvalid()
        {
            // Arrange
            var credentialsJson = "{\"api_key\": \"this_is_a_very_long_api_key_that_exceeds_the_maximum_length_allowed\"}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 20, 1); // Max length 20

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors[0].Should().Contain("exceeds maximum");
        }

        [Fact]
        public void ValidateCredentialsObject_WithComplexObject_ValidatesCorrectly()
        {
            // Arrange
            var credentialsJson = "{\"config\": {\"timeout\": 30, \"retries\": 3}}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 50, 1);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateCredentialsObject_WithComplexObjectTooLarge_ReturnsInvalid()
        {
            // Arrange
            var credentialsJson = "{\"config\": {\"timeout\": 30, \"retries\": 3}}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 10, 1); // Max value length 10

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors[0].Should().Contain("exceeds maximum");
        }

        [Fact]
        public void ValidateCredentialsObject_WithArray_ValidatesCorrectly()
        {
            // Arrange
            var credentialsJson = "{\"endpoints\": [\"https://api1.example.com\", \"https://api2.example.com\"]}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 100, 1);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateCredentialsObject_WithMultipleErrors_ReturnsAllErrors()
        {
            // Arrange
            var credentialsJson = "{\"api_key\": \"short\", \"url\": \"this_is_a_very_long_url_that_exceeds_the_maximum_length_allowed\"}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 20, 10); // Min 10, Max 20

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(e => e.Contains("is below minimum"));
            result.Errors.Should().Contain(e => e.Contains("exceeds maximum"));
        }

        [Fact]
        public void ValidateCredentialsObject_WithNonStringValues_ValidatesCorrectly()
        {
            // Arrange
            var credentialsJson = "{\"timeout\": 30, \"enabled\": true, \"count\": 5}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 100, 1);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void ValidateCredentialValue_WithCaseInsensitiveFieldType_WorksCorrectly()
        {
            // Arrange
            var value = "test@example.com";

            // Act
            var result1 = _service.ValidateCredentialValue("EMAIL", value);
            var result2 = _service.ValidateCredentialValue("Email", value);
            var result3 = _service.ValidateCredentialValue("email", value);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialValue_WithSpecialFieldTypes_WorksCorrectly()
        {
            // Arrange
            var clientId = "client_1234567890abcdef";
            var clientSecret = "secret_1234567890abcdef";
            var accessToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
            var refreshToken = "refresh_1234567890abcdef";

            // Act
            var clientIdResult = _service.ValidateCredentialValue("client_id", clientId);
            var clientSecretResult = _service.ValidateCredentialValue("client_secret", clientSecret);
            var accessTokenResult = _service.ValidateCredentialValue("access_token", accessToken);
            var refreshTokenResult = _service.ValidateCredentialValue("refresh_token", refreshToken);

            // Assert
            clientIdResult.Should().BeTrue();
            clientSecretResult.Should().BeTrue();
            accessTokenResult.Should().BeTrue();
            refreshTokenResult.Should().BeTrue();
        }

        [Fact]
        public void ValidateCredentialsObject_WithEmptyObject_ReturnsValid()
        {
            // Arrange
            var credentialsJson = "{}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 100, 1);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateCredentialsObject_WithNullValues_HandlesCorrectly()
        {
            // Arrange
            var credentialsJson = "{\"api_key\": null, \"url\": \"https://example.com\"}";
            var credentials = JsonDocument.Parse(credentialsJson);

            // Act
            var result = _service.ValidateCredentialsObject(credentials, 1000, 100, 1);

            // Assert
            result.IsValid.Should().BeTrue(); // Null values are not strings, so they're not validated
            result.Errors.Should().BeEmpty();
        }

        #endregion
    }
}
