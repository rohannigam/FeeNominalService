
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FeeNominalService.Controllers.V1;
using FeeNominalService.Services;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Models.Common;
using FeeNominalService.Services.AWS;
using FeeNominalService.Utils;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace FeeNominalService.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly Mock<IApiKeyService> _mockApiKeyService;
        private readonly Mock<IAwsSecretsManagerService> _mockSecretsManager;
        private readonly SecretNameFormatter _secretNameFormatter;
        private readonly AdminController _controller;
        private readonly DefaultHttpContext _httpContext;

        public AdminControllerTests()
        {
            _mockLogger = new Mock<ILogger<AdminController>>();
            _mockApiKeyService = new Mock<IApiKeyService>();
            _mockSecretsManager = new Mock<IAwsSecretsManagerService>();
            
            // Create a real SecretNameFormatter with test configuration
            var testConfig = new AwsSecretsManagerConfiguration
            {
                MerchantSecretNameFormat = "feenominal/merchants/{merchantId}/apikeys/{apiKey}",
                AdminSecretNameFormat = "feenominal/admin/{serviceName}-admin-api-key-secret"
            };
            var options = Microsoft.Extensions.Options.Options.Create(testConfig);
            _secretNameFormatter = new SecretNameFormatter(options);

            _controller = new AdminController(
                _mockLogger.Object,
                _mockApiKeyService.Object,
                _secretNameFormatter);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public void AdminController_Constructor_ShouldNotThrow()
        {
            // This test just verifies our constructor setup works
            _controller.Should().NotBeNull();
            _mockLogger.Should().NotBeNull();
            _mockApiKeyService.Should().NotBeNull();
            _mockSecretsManager.Should().NotBeNull();
            _secretNameFormatter.Should().NotBeNull();
        }

        [Fact]
        public void CreateMockSecureApiKeySecret_ShouldCreateValidObject()
        {
            // Test that our helper method works
            var secret = CreateMockSecureApiKeySecret("test-secret");
            secret.Should().NotBeNull();
            secret.GetSecret().Should().Be("test-secret");
        }

        #region GenerateAdminApiKey Tests

        [Fact]
        public async Task GenerateAdminApiKey_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "test-service",
                Description = "Test admin key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/bulk-sale-complete" }
            };

            var expectedResponse = new GenerateApiKeyResponse
            {
                ApiKey = "admin-key-123",
                Secret = "admin-secret-456",
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/bulk-sale-complete" }
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "valid-admin-secret";
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            _mockSecretsManager.Setup(x => x.GetSecureApiKeySecretAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateMockSecureApiKeySecret("valid-admin-secret"));

            _mockApiKeyService.Setup(x => x.GenerateApiKeyAsync(It.IsAny<GenerateApiKeyRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<GenerateApiKeyResponse>().Subject;
            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task GenerateAdminApiKey_WithMissingAdminSecretHeader_ReturnsForbidden()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "test-service",
                Description = "Test admin key"
            };

            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GenerateAdminApiKey_WithMissingTimestampHeader_ReturnsBadRequest()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "test-service",
                Description = "Test admin key"
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "valid-admin-secret";
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateAdminApiKey_WithMissingNonceHeader_ReturnsBadRequest()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "test-service",
                Description = "Test admin key"
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "valid-admin-secret";
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateAdminApiKey_WithInvalidServiceName_ReturnsBadRequest()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "invalid/service/name", // Invalid format
                Description = "Test admin key"
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "valid-admin-secret";
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateAdminApiKey_WithInvalidAdminSecret_ReturnsForbidden()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "test-service",
                Description = "Test admin key"
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "invalid-secret";
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            _mockSecretsManager.Setup(x => x.GetSecureApiKeySecretAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateMockSecureApiKeySecret("valid-admin-secret"));

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GenerateAdminApiKey_WithNullPurpose_UsesDefaultServiceName()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Description = "Test admin key"
            };

            var expectedResponse = new GenerateApiKeyResponse
            {
                ApiKey = "admin-key-123",
                Secret = "admin-secret-456",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "valid-admin-secret";
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            _mockSecretsManager.Setup(x => x.GetSecureApiKeySecretAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateMockSecureApiKeySecret("valid-admin-secret"));

            _mockApiKeyService.Setup(x => x.GenerateApiKeyAsync(It.Is<GenerateApiKeyRequest>(r => r.IsAdmin == true)))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<GenerateApiKeyResponse>().Subject;
            response.Should().BeEquivalentTo(expectedResponse);

            _mockApiKeyService.Verify(x => x.GenerateApiKeyAsync(It.Is<GenerateApiKeyRequest>(r => 
                r.IsAdmin == true && 
                r.AllowedEndpoints != null &&
                r.AllowedEndpoints.Contains("/api/v1/surcharge/bulk-sale-complete"))), Times.Once);
        }

        #endregion

        #region RotateAdminApiKey Tests

        [Fact]
        public async Task RotateAdminApiKey_WithValidRequestAndAdminScope_ReturnsOkResult()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = "test-service" };
            var expectedResponse = new GenerateApiKeyResponse
            {
                ApiKey = "new-admin-key-123",
                Secret = "new-admin-secret-456",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            SetupUserClaims(isAdmin: true);

            _mockApiKeyService.Setup(x => x.RotateAdminApiKeyAsync("test-service"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RotateAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<GenerateApiKeyResponse>().Subject;
            response.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task RotateAdminApiKey_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            AdminKeyServiceNameRequest? request = null;

            // Act
            var result = await _controller.RotateAdminApiKey(request!, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RotateAdminApiKey_WithInvalidServiceName_ReturnsBadRequest()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = "invalid/service/name" };

            // Act
            var result = await _controller.RotateAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RotateAdminApiKey_WithNonAdminScope_ReturnsUnauthorized()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = "test-service" };

            SetupUserClaims(isAdmin: false);

            // Act
            var result = await _controller.RotateAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            var errorResponse = unauthorizedResult!.Value.Should().BeOfType<ApiErrorResponse>().Subject;
            errorResponse.Message.Should().Contain("Only admin-scope API keys can rotate the admin key");
        }

        [Fact]
        public async Task RotateAdminApiKey_WithNullServiceName_UsesDefault()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = null! };
            var expectedResponse = new GenerateApiKeyResponse
            {
                ApiKey = "new-admin-key-123",
                Secret = "new-admin-secret-456",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            SetupUserClaims(isAdmin: true);

            _mockApiKeyService.Setup(x => x.RotateAdminApiKeyAsync("default"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RotateAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<GenerateApiKeyResponse>().Subject;
            response.Should().BeEquivalentTo(expectedResponse);
        }

        #endregion

        #region RevokeAdminApiKey Tests

        [Fact]
        public async Task RevokeAdminApiKey_WithValidRequestAndAdminScope_ReturnsOkResult()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = "test-service" };
            var expectedResponse = new ApiKeyRevokeResponse
            {
                ApiKey = "admin-key-123",
                RevokedAt = DateTime.UtcNow,
                Status = "REVOKED"
            };

            SetupUserClaims(isAdmin: true);

            _mockApiKeyService.Setup(x => x.RevokeAdminApiKeyAsync("test-service"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RevokeAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ApiResponse<ApiKeyRevokeResponse>>().Subject;
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Admin API key revoked successfully");
            response.Data.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task RevokeAdminApiKey_WithNullRequest_ReturnsBadRequest()
        {
            // Arrange
            AdminKeyServiceNameRequest? request = null;

            // Act
            var result = await _controller.RevokeAdminApiKey(request!, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RevokeAdminApiKey_WithInvalidServiceName_ReturnsBadRequest()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = "invalid/service/name" };

            // Act
            var result = await _controller.RevokeAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RevokeAdminApiKey_WithNonAdminScope_ReturnsUnauthorized()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = "test-service" };

            SetupUserClaims(isAdmin: false);

            // Act
            var result = await _controller.RevokeAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            var errorResponse = unauthorizedResult!.Value.Should().BeOfType<ApiErrorResponse>().Subject;
            errorResponse.Message.Should().Contain("Only admin-scope API keys can revoke the admin key");
        }

        [Fact]
        public async Task RevokeAdminApiKey_WithNullServiceName_UsesDefault()
        {
            // Arrange
            var request = new AdminKeyServiceNameRequest { ServiceName = null! };
            var expectedResponse = new ApiKeyRevokeResponse
            {
                ApiKey = "admin-key-123",
                RevokedAt = DateTime.UtcNow,
                Status = "REVOKED"
            };

            SetupUserClaims(isAdmin: true);

            _mockApiKeyService.Setup(x => x.RevokeAdminApiKeyAsync("default"))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.RevokeAdminApiKey(request, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ApiResponse<ApiKeyRevokeResponse>>().Subject;
            response.Success.Should().BeTrue();
            response.Message.Should().Be("Admin API key revoked successfully");
            response.Data.Should().BeEquivalentTo(expectedResponse);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public async Task GenerateAdminApiKey_WithAdminSecretNotConfigured_ReturnsForbidden()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "test-service",
                Description = "Test admin key"
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "valid-admin-secret";
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            _mockSecretsManager.Setup(x => x.GetSecureApiKeySecretAsync(It.IsAny<string>()))
                .ReturnsAsync((SecureApiKeySecret?)null);

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GenerateAdminApiKey_WithEmptyAllowedEndpoints_SetsDefaultEndpoints()
        {
            // Arrange
            var request = new GenerateApiKeyRequest
            {
                Purpose = "test-service",
                Description = "Test admin key",
                AllowedEndpoints = new string[0] // Empty array
            };

            var expectedResponse = new GenerateApiKeyResponse
            {
                ApiKey = "admin-key-123",
                Secret = "admin-secret-456",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _httpContext.Request.Headers["X-Admin-Secret"] = "valid-admin-secret";
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            _mockSecretsManager.Setup(x => x.GetSecureApiKeySecretAsync(It.IsAny<string>()))
                .ReturnsAsync(CreateMockSecureApiKeySecret("valid-admin-secret"));

            _mockApiKeyService.Setup(x => x.GenerateApiKeyAsync(It.IsAny<GenerateApiKeyRequest>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GenerateAdminApiKey(request, _mockSecretsManager.Object, _mockApiKeyService.Object);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<GenerateApiKeyResponse>().Subject;
            response.Should().BeEquivalentTo(expectedResponse);

            // Verify that the service was called with the request that has default endpoints
            _mockApiKeyService.Verify(x => x.GenerateApiKeyAsync(It.Is<GenerateApiKeyRequest>(r => 
                r.IsAdmin == true && 
                r.AllowedEndpoints != null &&
                r.AllowedEndpoints.Contains("/api/v1/surcharge/bulk-sale-complete"))), Times.Once);
        }

        [Fact]
        public void SecretNameFormatter_FormatAdminSecretName_ReturnsCorrectFormat()
        {
            // Test that our SecretNameFormatter works correctly
            var result = _secretNameFormatter.FormatAdminSecretName("test-service");
            result.Should().Be("feenominal/admin/test-service-admin-api-key-secret");
        }

        [Fact]
        public void SecretNameFormatter_IsAdminSecretName_ReturnsTrueForAdminSecrets()
        {
            // Test admin secret name detection
            var adminSecretName = "feenominal/admin/test-service-admin-api-key-secret";
            var result = _secretNameFormatter.IsAdminSecretName(adminSecretName);
            result.Should().BeTrue();
        }

        [Fact]
        public void SecretNameFormatter_IsAdminSecretName_ReturnsFalseForMerchantSecrets()
        {
            // Test that merchant secrets are not detected as admin secrets
            var merchantSecretName = "feenominal/merchants/123/apikeys/abc";
            var result = _secretNameFormatter.IsAdminSecretName(merchantSecretName);
            result.Should().BeFalse();
        }

        #endregion

        private void SetupUserClaims(bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new Claim("MerchantId", Guid.NewGuid().ToString()),
                new Claim("Scope", isAdmin ? "admin" : "merchant"),
                new Claim("IsAdmin", isAdmin.ToString()),
                new Claim("AllowedEndpoints", "/api/v1/surcharge/bulk-sale-complete")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;
        }

        private SecureApiKeySecret CreateMockSecureApiKeySecret(string secret)
        {
            var apiKeySecret = new ApiKeySecret
            {
                ApiKey = "admin-api-key",
                Secret = secret,
                MerchantId = null, // Admin secrets don't belong to a specific merchant
                CreatedAt = DateTime.UtcNow,
                LastRotated = null,
                IsRevoked = false,
                RevokedAt = null,
                Status = "ACTIVE",
                Scope = "admin",
                UpdatedAt = DateTime.UtcNow
            };

            return SecureApiKeySecret.FromApiKeySecret(apiKeySecret);
        }
    }
} 