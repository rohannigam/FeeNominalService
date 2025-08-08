using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FeeNominalService.Controllers.V1;
using FeeNominalService.Services;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.Merchant.Requests;
using FeeNominalService.Models.Merchant.Responses;
using FeeNominalService.Models.Common;
using FeeNominalService.Services.AWS;
using FeeNominalService.Models.ApiKey;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace FeeNominalService.Tests.Controllers
{
    public class OnboardingControllerTests
    {
        private readonly Mock<ILogger<OnboardingController>> _mockLogger;
        private readonly Mock<IMerchantService> _mockMerchantService;
        private readonly Mock<IApiKeyService> _mockApiKeyService;
        private readonly Mock<IAwsSecretsManagerService> _mockSecretsManager;
        private readonly OnboardingController _controller;
        private readonly DefaultHttpContext _httpContext;

        public OnboardingControllerTests()
        {
            _mockLogger = new Mock<ILogger<OnboardingController>>();
            _mockMerchantService = new Mock<IMerchantService>();
            _mockApiKeyService = new Mock<IApiKeyService>();
            _mockSecretsManager = new Mock<IAwsSecretsManagerService>();

            _controller = new OnboardingController(
                _mockLogger.Object,
                _mockMerchantService.Object,
                _mockApiKeyService.Object,
                _mockSecretsManager.Object);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public void OnboardingController_Constructor_ShouldNotThrow()
        {
            // This test just verifies our constructor setup works
            _controller.Should().NotBeNull();
            _mockLogger.Should().NotBeNull();
            _mockMerchantService.Should().NotBeNull();
            _mockApiKeyService.Should().NotBeNull();
            _mockSecretsManager.Should().NotBeNull();
        }

        [Fact]
        public void OnboardingController_ShouldHaveCorrectRouteAttribute()
        {
            // Test that the controller has the correct route attribute
            var routeAttribute = typeof(OnboardingController).GetCustomAttributes(typeof(RouteAttribute), false)
                .FirstOrDefault() as RouteAttribute;
            
            routeAttribute.Should().NotBeNull();
            routeAttribute!.Template.Should().Be("api/v1/onboarding");
        }

        [Fact]
        public void OnboardingController_ShouldHaveApiVersionAttribute()
        {
            // Test that the controller has the correct API version attribute
            var apiVersionAttribute = typeof(OnboardingController).GetCustomAttributes(typeof(ApiVersionAttribute), false)
                .FirstOrDefault() as ApiVersionAttribute;
            
            apiVersionAttribute.Should().NotBeNull();
            apiVersionAttribute!.Versions.Should().Contain(new ApiVersion(1, 0));
        }

        private void SetupUserClaims(Guid merchantId, bool isAdmin = false)
        {
            var claims = new List<Claim>
            {
                new Claim("MerchantId", merchantId.ToString()),
                new Claim("Scope", isAdmin ? "admin" : "merchant"),
                new Claim("IsAdmin", isAdmin.ToString()),
                new Claim("AllowedEndpoints", "/api/v1/onboarding/*")
            };

            var identity = new ClaimsIdentity(claims, "Test", "MerchantId", "Scope");
            identity.AddClaim(new Claim(ClaimTypes.Name, "test-user"));
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;
        }

        private void SetupRequiredHeaders()
        {
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();
        }

        private void SetupApiKeyHeaders(Guid merchantId, string apiKey = "test-api-key")
        {
            _httpContext.Request.Headers["X-Merchant-ID"] = merchantId.ToString();
            _httpContext.Request.Headers["X-API-Key"] = apiKey;
        }

        #region GenerateInitialApiKeyAsync Tests

        [Fact]
        public async Task GenerateInitialApiKeyAsync_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "Test Merchant",
                ExternalMerchantId = "EXT123",
                ExternalMerchantGuid = Guid.NewGuid(),
                Description = "Test API key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" }
            };

            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                Name = "Test Merchant",
                ExternalMerchantId = "EXT123",
                ExternalMerchantGuid = request.ExternalMerchantGuid,
                StatusId = 1 // Active status ID
            };

            var apiKeyResponse = new GenerateInitialApiKeyResponse
            {
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE",
                ApiKeyId = Guid.NewGuid()
            };

            SetupRequiredHeaders();
            _httpContext.Request.Headers["X-Onboarding-Metadata"] = "{\"AdminUserId\":\"admin123\"}";

            var merchantResponse = new MerchantResponse
            {
                MerchantId = merchant.MerchantId,
                Name = merchant.Name,
                ExternalMerchantId = merchant.ExternalMerchantId,
                ExternalMerchantGuid = merchant.ExternalMerchantGuid,
                StatusId = 1,
                StatusCode = "ACTIVE",
                StatusName = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM"
            };

            _mockMerchantService.Setup(x => x.CreateMerchantAsync(request, "SYSTEM"))
                .ReturnsAsync(merchantResponse);

            _mockApiKeyService.Setup(x => x.GenerateInitialApiKeyAsync(merchant.MerchantId, request))
                .ReturnsAsync(apiKeyResponse);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GenerateInitialApiKeyAsync(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<GenerateInitialApiKeyResponse>().Subject;
            
            response.MerchantId.Should().Be(merchant.MerchantId);
            response.ExternalMerchantId.Should().Be(merchant.ExternalMerchantId);
            response.ExternalMerchantGuid.Should().Be(merchant.ExternalMerchantGuid);
            response.MerchantName.Should().Be(merchant.Name);
            response.ApiKey.Should().Be(apiKeyResponse.ApiKey);
            response.Secret.Should().Be(apiKeyResponse.Secret);
            response.Status.Should().Be(apiKeyResponse.Status);
            response.ApiKeyId.Should().Be(apiKeyResponse.ApiKeyId);
        }

        [Fact]
        public async Task GenerateInitialApiKeyAsync_WithMissingTimestampHeader_ReturnsBadRequest()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "Test Merchant"
            };

            // Only set nonce, not timestamp
            _httpContext.Request.Headers["X-Nonce"] = Guid.NewGuid().ToString();

            // Act
            var result = await _controller.GenerateInitialApiKeyAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateInitialApiKeyAsync_WithMissingNonceHeader_ReturnsBadRequest()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "Test Merchant"
            };

            // Only set timestamp, not nonce
            _httpContext.Request.Headers["X-Timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Act
            var result = await _controller.GenerateInitialApiKeyAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GenerateInitialApiKeyAsync_WithNullOnboardingMetadata_UsesSystemAsPerformedBy()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "Test Merchant",
                ExternalMerchantId = "EXT123"
            };

            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                Name = "Test Merchant",
                ExternalMerchantId = "EXT123",
                StatusId = 1 // Active status ID
            };

            var apiKeyResponse = new GenerateInitialApiKeyResponse
            {
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE",
                ApiKeyId = Guid.NewGuid()
            };

            SetupRequiredHeaders();
            // Don't set X-Onboarding-Metadata header

            var merchantResponse = new MerchantResponse
            {
                MerchantId = merchant.MerchantId,
                Name = merchant.Name,
                ExternalMerchantId = merchant.ExternalMerchantId,
                ExternalMerchantGuid = merchant.ExternalMerchantGuid,
                StatusId = 1,
                StatusCode = "ACTIVE",
                StatusName = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM"
            };

            _mockMerchantService.Setup(x => x.CreateMerchantAsync(request, "SYSTEM"))
                .ReturnsAsync(merchantResponse);

            _mockApiKeyService.Setup(x => x.GenerateInitialApiKeyAsync(merchant.MerchantId, request))
                .ReturnsAsync(apiKeyResponse);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                "SYSTEM")) // Should use "SYSTEM" as performedBy
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GenerateInitialApiKeyAsync(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            
            // Verify that CreateAuditTrailAsync was called with "SYSTEM" as performedBy
            _mockMerchantService.Verify(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                "SYSTEM"), Times.Once);
        }

        [Fact]
        public async Task GenerateInitialApiKeyAsync_WithValidOnboardingMetadata_UsesAdminUserIdAsPerformedBy()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "Test Merchant",
                ExternalMerchantId = "EXT123"
            };

            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                Name = "Test Merchant",
                ExternalMerchantId = "EXT123",
                StatusId = 1 // Active status ID
            };

            var apiKeyResponse = new GenerateInitialApiKeyResponse
            {
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE",
                ApiKeyId = Guid.NewGuid()
            };

            SetupRequiredHeaders();
            _httpContext.Request.Headers["X-Onboarding-Metadata"] = "{\"AdminUserId\":\"admin456\"}";

            var merchantResponse = new MerchantResponse
            {
                MerchantId = merchant.MerchantId,
                Name = merchant.Name,
                ExternalMerchantId = merchant.ExternalMerchantId,
                ExternalMerchantGuid = merchant.ExternalMerchantGuid,
                StatusId = 1,
                StatusCode = "ACTIVE",
                StatusName = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM"
            };

            _mockMerchantService.Setup(x => x.CreateMerchantAsync(request, "SYSTEM"))
                .ReturnsAsync(merchantResponse);

            _mockApiKeyService.Setup(x => x.GenerateInitialApiKeyAsync(merchant.MerchantId, request))
                .ReturnsAsync(apiKeyResponse);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                "admin456")) // Should use "admin456" as performedBy
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GenerateInitialApiKeyAsync(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            
            // Verify that CreateAuditTrailAsync was called with "admin456" as performedBy
            _mockMerchantService.Verify(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                "admin456"), Times.Once);
        }

        #endregion

        #region Merchant Lookup Tests

        [Fact]
        public async Task GetMerchantAuditTrail_WithValidMerchantId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var auditTrail = new List<MerchantAuditTrail>
            {
                new MerchantAuditTrail
                {
                    MerchantAuditTrailId = Guid.NewGuid(),
                    MerchantId = merchantId,
                    Action = "CREATE",
                    EntityType = "MERCHANT",
                    PropertyName = "Name",
                    NewValue = "Test Merchant",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = "admin123"
                }
            };

            SetupUserClaims(merchantId, isAdmin: true);

            _mockMerchantService.Setup(x => x.GetMerchantAuditTrailAsync(merchantId))
                .ReturnsAsync(auditTrail);

            // Act
            var result = await _controller.GetMerchantAuditTrail(merchantId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<MerchantAuditTrail>>>().Subject;
            
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().HaveCount(1);
            apiResponse.Data.First().MerchantId.Should().Be(merchantId);
        }

        [Fact]
        public async Task GetMerchantAuditTrail_WithNoAuditTrail_ReturnsEmptyCollection()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            SetupUserClaims(merchantId, isAdmin: true);

            _mockMerchantService.Setup(x => x.GetMerchantAuditTrailAsync(merchantId))
                .ReturnsAsync(new List<MerchantAuditTrail>());

            // Act
            var result = await _controller.GetMerchantAuditTrail(merchantId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<MerchantAuditTrail>>>().Subject;
            
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task GetMerchantByExternalId_WithValidExternalId_ReturnsOkResult()
        {
            // Arrange
            var externalMerchantId = "EXT123";
            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                Name = "Test Merchant",
                ExternalMerchantId = externalMerchantId,
                StatusId = 1
            };

            SetupUserClaims(merchant.MerchantId, isAdmin: true);

            _mockMerchantService.Setup(x => x.GetByExternalMerchantIdAsync(externalMerchantId))
                .ReturnsAsync(merchant);

            // Act
            var result = await _controller.GetMerchantByExternalId(externalMerchantId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<Merchant>>().Subject;
            
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.ExternalMerchantId.Should().Be(externalMerchantId);
        }

        [Fact]
        public async Task GetMerchantByExternalId_WithNonExistentExternalId_ReturnsNotFound()
        {
            // Arrange
            var externalMerchantId = "NONEXISTENT";
            SetupUserClaims(Guid.NewGuid(), isAdmin: true);

            _mockMerchantService.Setup(x => x.GetByExternalMerchantIdAsync(externalMerchantId))
                .ReturnsAsync((Merchant)null!);

            // Act
            var result = await _controller.GetMerchantByExternalId(externalMerchantId);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMerchantByExternalGuid_WithValidExternalGuid_ReturnsOkResult()
        {
            // Arrange
            var externalMerchantGuid = Guid.NewGuid();
            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                Name = "Test Merchant",
                ExternalMerchantGuid = externalMerchantGuid,
                StatusId = 1
            };

            SetupUserClaims(merchant.MerchantId, isAdmin: true);

            _mockMerchantService.Setup(x => x.GetByExternalMerchantGuidAsync(externalMerchantGuid))
                .ReturnsAsync(merchant);

            // Act
            var result = await _controller.GetMerchantByExternalGuid(externalMerchantGuid);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<Merchant>>().Subject;
            
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.ExternalMerchantGuid.Should().Be(externalMerchantGuid);
        }

        [Fact]
        public async Task GetMerchantByExternalGuid_WithNonExistentExternalGuid_ReturnsNotFound()
        {
            // Arrange
            var externalMerchantGuid = Guid.NewGuid();
            SetupUserClaims(Guid.NewGuid(), isAdmin: true);

            _mockMerchantService.Setup(x => x.GetByExternalMerchantGuidAsync(externalMerchantGuid))
                .ReturnsAsync((Merchant)null!);

            // Act
            var result = await _controller.GetMerchantByExternalGuid(externalMerchantGuid);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetMerchant_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var merchant = new Merchant
            {
                MerchantId = merchantId,
                Name = "Test Merchant",
                ExternalMerchantId = "EXT123",
                StatusId = 1
            };

            SetupUserClaims(merchantId, isAdmin: true);

            _mockMerchantService.Setup(x => x.GetMerchantAsync(merchantId))
                .ReturnsAsync(merchant);

            // Act
            var result = await _controller.GetMerchant(merchantId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(merchant);
        }

        [Fact]
        public async Task GetMerchant_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            SetupUserClaims(merchantId, isAdmin: true);

            _mockMerchantService.Setup(x => x.GetMerchantAsync(merchantId))
                .ReturnsAsync((Merchant)null!);

            // Act
            var result = await _controller.GetMerchant(merchantId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region API Key Management Tests

        [Fact]
        public void GenerateApiKey_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the GenerateApiKey method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("GenerateApiKey", 
                new[] { typeof(GenerateApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void GenerateApiKey_HasCorrectAttributes()
        {
            // This test verifies that the GenerateApiKey method has the correct attributes
            var methodInfo = typeof(OnboardingController).GetMethod("GenerateApiKey", 
                new[] { typeof(GenerateApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            
            var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        [Fact]
        public void GenerateApiKeyRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the GenerateApiKeyRequest model has the expected properties
            var request = new GenerateApiKeyRequest
            {
                MerchantId = Guid.NewGuid(),
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                IsAdmin = false
            };

            request.MerchantId.Should().NotBe(Guid.Empty);
            request.Description.Should().Be("Test API Key");
            request.RateLimit.Should().Be(1000);
            request.AllowedEndpoints.Should().HaveCount(1);
            request.IsAdmin.Should().BeFalse();
        }

        [Fact]
        public void GenerateApiKeyResponse_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the GenerateApiKeyResponse model has the expected properties
            var response = new GenerateApiKeyResponse
            {
                ApiKey = "test-api-key",
                Secret = "test-secret",
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Description = "Test API Key"
            };

            response.ApiKey.Should().Be("test-api-key");
            response.Secret.Should().Be("test-secret");
            response.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
            response.RateLimit.Should().Be(1000);
            response.AllowedEndpoints.Should().HaveCount(1);
            response.Description.Should().Be("Test API Key");
        }

        #endregion

        #region Section 3.2: UpdateApiKey Tests

        [Fact]
        public void UpdateApiKey_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the UpdateApiKey method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("UpdateApiKey", 
                new[] { typeof(UpdateApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void UpdateApiKey_HasCorrectAttributes()
        {
            // This test verifies that the UpdateApiKey method has the correct attributes
            var methodInfo = typeof(OnboardingController).GetMethod("UpdateApiKey", 
                new[] { typeof(UpdateApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            
            var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        [Fact]
        public void UpdateApiKeyRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the UpdateApiKeyRequest model has the expected properties
            var request = new UpdateApiKeyRequest
            {
                MerchantId = Guid.NewGuid().ToString(),
                ApiKey = "test-api-key",
                Description = "Updated API Key",
                RateLimit = 2000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*", "/api/v1/onboarding/*" },
                OnboardingMetadata = new OnboardingMetadata { AdminUserId = "admin123" }
            };

            request.MerchantId.Should().NotBeEmpty();
            request.ApiKey.Should().Be("test-api-key");
            request.Description.Should().Be("Updated API Key");
            request.RateLimit.Should().Be(2000);
            request.AllowedEndpoints.Should().HaveCount(2);
            request.OnboardingMetadata.Should().NotBeNull();
            request.OnboardingMetadata.AdminUserId.Should().Be("admin123");
        }

        [Fact]
        public async Task UpdateApiKey_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key",
                Description = "Updated API Key",
                RateLimit = 2000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                OnboardingMetadata = new OnboardingMetadata { AdminUserId = "admin123" }
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Original API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            var updatedApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Updated API Key",
                RateLimit = 2000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                LastRotatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.UpdateApiKeyAsync(request, request.OnboardingMetadata))
                .ReturnsAsync(updatedApiKeyInfo);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiKeyInfo>>().Subject;
            
            apiResponse.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("API key updated successfully");
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Description.Should().Be("Updated API Key");
            apiResponse.Data.RateLimit.Should().Be(2000);
        }

        [Fact]
        public async Task UpdateApiKey_WithNonExistentApiKey_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "non-existent-api-key",
                Description = "Updated API Key",
                OnboardingMetadata = new OnboardingMetadata { AdminUserId = "admin123" }
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("non-existent-api-key"))
                .ReturnsAsync(default(ApiKeyInfo?));

            // Act
            var result = await _controller.UpdateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("API key not found");
        }

        [Fact]
        public async Task UpdateApiKey_WithInsufficientPermissions_ReturnsUnauthorized()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "admin-api-key",
                Description = "Updated API Key",
                OnboardingMetadata = new OnboardingMetadata { AdminUserId = "admin123" }
            };

            // Set up merchant scope (not admin)
            SetupUserClaims(merchantId, isAdmin: false);
            SetupRequiredHeaders();

            var adminApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "admin-api-key",
                MerchantId = null, // Admin key has no merchant
                Description = "Admin API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/*" },
                Status = "admin", // This triggers admin check
                CreatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("admin-api-key"))
                .ReturnsAsync(adminApiKeyInfo);

            // Act
            var result = await _controller.UpdateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            var errorResponse = unauthorizedResult!.Value.Should().BeOfType<ApiErrorResponse>().Subject;
            errorResponse.Message.Should().Be("Only admin-scope keys can update admin keys.");
        }

        [Fact]
        public async Task UpdateApiKey_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key",
                Description = "Updated API Key",
                OnboardingMetadata = new OnboardingMetadata { AdminUserId = "admin123" }
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Original API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.UpdateApiKeyAsync(request, request.OnboardingMetadata))
                .ThrowsAsync(new InvalidOperationException("Service error"));

            // Act
            var result = await _controller.UpdateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().Be("An error occurred while updating the API key");
        }

        [Fact]
        public async Task UpdateApiKey_WithNullUpdateResponse_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key",
                Description = "Updated API Key",
                OnboardingMetadata = new OnboardingMetadata { AdminUserId = "admin123" }
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Original API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.UpdateApiKeyAsync(request, request.OnboardingMetadata))
                .ReturnsAsync((ApiKeyInfo)null!);

            // Act
            var result = await _controller.UpdateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            var errorResponse = badRequestResult!.Value.Should().BeOfType<ApiErrorResponse>().Subject;
            errorResponse.Message.Should().Contain("API key update failed");
        }

        [Fact]
        public async Task UpdateApiKey_WithOnboardingMetadata_UsesAdminUserIdAsPerformedBy()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key",
                Description = "Updated API Key",
                OnboardingMetadata = new OnboardingMetadata { AdminUserId = "admin456" }
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();
            _httpContext.Request.Headers["X-Onboarding-Metadata"] = "{\"AdminUserId\":\"admin456\"}";

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Original API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            var updatedApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Updated API Key",
                RateLimit = 2000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                LastRotatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.UpdateApiKeyAsync(request, request.OnboardingMetadata))
                .ReturnsAsync(updatedApiKeyInfo);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                "admin456")) // Should use "admin456" as performedBy
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            
            // Verify that CreateAuditTrailAsync was called with "admin456" as performedBy
            _mockMerchantService.Verify(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                "admin456"), Times.Once);
        }

        #endregion

        #region Section 3.3: RevokeApiKey Tests

        [Fact]
        public void RevokeApiKey_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the RevokeApiKey method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("RevokeApiKey", 
                new[] { typeof(RevokeApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void RevokeApiKey_HasCorrectAttributes()
        {
            // This test verifies that the RevokeApiKey method has the correct attributes
            var methodInfo = typeof(OnboardingController).GetMethod("RevokeApiKey", 
                new[] { typeof(RevokeApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            
            var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        [Fact]
        public void RevokeApiKeyRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the RevokeApiKeyRequest model has the expected properties
            var request = new RevokeApiKeyRequest
            {
                MerchantId = Guid.NewGuid().ToString(),
                ApiKey = "test-api-key-to-revoke"
            };

            request.MerchantId.Should().NotBeEmpty();
            request.ApiKey.Should().Be("test-api-key-to-revoke");
        }

        [Fact]
        public void ApiKeyRevokeResponse_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the ApiKeyRevokeResponse model has the expected properties
            var response = new ApiKeyRevokeResponse
            {
                ApiKey = "test-api-key",
                RevokedAt = DateTime.UtcNow,
                Status = "REVOKED"
            };

            response.ApiKey.Should().Be("test-api-key");
            response.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            response.Status.Should().Be("REVOKED");
        }

        [Fact]
        public async Task RevokeApiKey_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RevokeApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key"
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            var revokedApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "REVOKED",
                CreatedAt = DateTime.UtcNow,
                RevokedAt = DateTime.UtcNow,
                IsRevoked = true
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.RevokeApiKeyAsync(request))
                .ReturnsAsync(true);

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(revokedApiKeyInfo);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RevokeApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<ApiKeyRevokeResponse>>().Subject;
            
            apiResponse.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("API key revoked successfully");
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.ApiKey.Should().Be("test-api-key");
            apiResponse.Data.Status.Should().Be("REVOKED");
            apiResponse.Data.RevokedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task RevokeApiKey_WithNonExistentApiKey_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RevokeApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "non-existent-api-key"
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("non-existent-api-key"))
                .ReturnsAsync(default(ApiKeyInfo?));

            // Act
            var result = await _controller.RevokeApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("API key not found");
        }

        [Fact]
        public async Task RevokeApiKey_WithInsufficientPermissions_ReturnsUnauthorized()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RevokeApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "admin-api-key"
            };

            // Set up merchant scope (not admin)
            SetupUserClaims(merchantId, isAdmin: false);
            SetupRequiredHeaders();

            var adminApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "admin-api-key",
                MerchantId = null, // Admin key has no merchant
                Description = "Admin API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/*" },
                Status = "admin", // This triggers admin check
                CreatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("admin-api-key"))
                .ReturnsAsync(adminApiKeyInfo);

            // Act
            var result = await _controller.RevokeApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            var errorResponse = unauthorizedResult!.Value.Should().BeOfType<ApiErrorResponse>().Subject;
            errorResponse.Message.Should().Be("Only admin-scope keys can revoke admin keys.");
        }

        [Fact]
        public async Task RevokeApiKey_WithInvalidMerchantIdFormat_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RevokeApiKeyRequest
            {
                MerchantId = "invalid-guid-format",
                ApiKey = "test-api-key"
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId, // Match the user's merchant ID
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            // Act
            var result = await _controller.RevokeApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Invalid merchant ID format");
        }

        [Fact]
        public async Task RevokeApiKey_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RevokeApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key"
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.RevokeApiKeyAsync(request))
                .ThrowsAsync(new InvalidOperationException("Service error"));

            // Act
            var result = await _controller.RevokeApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().Be("An error occurred while revoking the API key");
        }

        [Fact]
        public async Task RevokeApiKey_WithMerchantNotFound_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RevokeApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key"
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.RevokeApiKeyAsync(request))
                .ThrowsAsync(new KeyNotFoundException("Merchant not found"));

            // Act
            var result = await _controller.RevokeApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be($"Merchant not found: {merchantId}");
        }

        [Fact]
        public async Task RevokeApiKey_WithMerchantId_CreatesAuditTrail()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RevokeApiKeyRequest
            {
                MerchantId = merchantId.ToString(),
                ApiKey = "test-api-key"
            };

            SetupUserClaims(merchantId);
            SetupRequiredHeaders();

            var existingApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            var revokedApiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Description = "Test API Key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                Status = "REVOKED",
                CreatedAt = DateTime.UtcNow,
                RevokedAt = DateTime.UtcNow,
                IsRevoked = true
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(existingApiKeyInfo);

            _mockApiKeyService.Setup(x => x.RevokeApiKeyAsync(request))
                .ReturnsAsync(true);

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ReturnsAsync(revokedApiKeyInfo);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                merchantId,
                "API_KEY_REVOKED",
                "api_key",
                null,
                "test-api-key_Revoked",
                "SYSTEM"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RevokeApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            
            // Verify that CreateAuditTrailAsync was called with correct parameters
            _mockMerchantService.Verify(x => x.CreateAuditTrailAsync(
                merchantId,
                "API_KEY_REVOKED",
                "api_key",
                null,
                "test-api-key_Revoked",
                "SYSTEM"), Times.Once);
        }

        #endregion

        #region Section 4: Additional API Key and Merchant Management Tests

        #region Section 4.1: GetApiKeys Tests

        [Fact]
        public void GetApiKeys_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the GetApiKeys method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("GetApiKeys", 
                new[] { typeof(string) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void GetApiKeys_HasCorrectAttributes()
        {
            // This test verifies that the GetApiKeys method has the correct attributes
            var methodInfo = typeof(OnboardingController).GetMethod("GetApiKeys", 
                new[] { typeof(string) });
            
            methodInfo.Should().NotBeNull();
            
            var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        [Fact]
        public async Task GetApiKeys_WithValidMerchantId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKeys = new List<ApiKeyInfo>
            {
                new ApiKeyInfo
                {
                    ApiKey = "test-api-key-1",
                    MerchantId = Guid.Parse(merchantId),
                    Description = "Test API Key 1",
                    RateLimit = 1000,
                    AllowedEndpoints = new[] { "/api/v1/surcharge/*" },
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                },
                new ApiKeyInfo
                {
                    ApiKey = "test-api-key-2",
                    MerchantId = Guid.Parse(merchantId),
                    Description = "Test API Key 2",
                    RateLimit = 2000,
                    AllowedEndpoints = new[] { "/api/v1/onboarding/*" },
                    Status = "ACTIVE",
                    CreatedAt = DateTime.UtcNow
                }
            };

            SetupUserClaims(Guid.Parse(merchantId));

            _mockApiKeyService.Setup(x => x.GetMerchantApiKeysAsync(merchantId))
                .ReturnsAsync(apiKeys);

            // Act
            var result = await _controller.GetApiKeys(merchantId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedApiKeys = okResult.Value.Should().BeOfType<List<ApiKeyInfo>>().Subject;
            
            returnedApiKeys.Should().HaveCount(2);
            returnedApiKeys[0].ApiKey.Should().Be("test-api-key-1");
            returnedApiKeys[1].ApiKey.Should().Be("test-api-key-2");
        }

        [Fact]
        public async Task GetApiKeys_WithInvalidMerchantIdFormat_ReturnsBadRequest()
        {
            // Arrange
            var invalidMerchantId = "invalid-guid-format";
            SetupUserClaims(Guid.NewGuid());

            // Act
            var result = await _controller.GetApiKeys(invalidMerchantId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Invalid merchant ID format");
        }

        [Fact]
        public async Task GetApiKeys_WithNonExistentMerchant_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            SetupUserClaims(Guid.Parse(merchantId));

            _mockApiKeyService.Setup(x => x.GetMerchantApiKeysAsync(merchantId))
                .ThrowsAsync(new KeyNotFoundException("Merchant not found"));

            // Act
            var result = await _controller.GetApiKeys(merchantId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
            var notFoundResult = result as NotFoundObjectResult;
            notFoundResult!.Value.Should().Be("Merchant not found: " + merchantId);
        }

        [Fact]
        public async Task GetApiKeys_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            SetupUserClaims(Guid.Parse(merchantId));

            _mockApiKeyService.Setup(x => x.GetMerchantApiKeysAsync(merchantId))
                .ThrowsAsync(new InvalidOperationException("Service error"));

            // Act
            var result = await _controller.GetApiKeys(merchantId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().Be("An error occurred while retrieving API keys");
        }

        #endregion

        #region Section 4.2: CreateMerchant Tests

        [Fact]
        public void CreateMerchant_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the CreateMerchant method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("CreateMerchant", 
                new[] { typeof(GenerateInitialApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<ActionResult<ApiResponse<MerchantResponse>>>));
        }

        [Fact]
        public void CreateMerchant_HasCorrectAttributes()
        {
            // This test verifies that the CreateMerchant method has the correct attributes
            var methodInfo = typeof(OnboardingController).GetMethod("CreateMerchant", 
                new[] { typeof(GenerateInitialApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            
            var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        [Fact]
        public async Task CreateMerchant_WithValidRequest_ReturnsCreatedResult()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "New Test Merchant",
                ExternalMerchantId = "EXT456",
                ExternalMerchantGuid = Guid.NewGuid(),
                Description = "Test API key",
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/v1/surcharge/*" }
            };

            SetupUserClaims(Guid.NewGuid(), isAdmin: true);

            var merchantResponse = new MerchantResponse
            {
                MerchantId = Guid.NewGuid(),
                Name = "New Test Merchant",
                ExternalMerchantId = "EXT456",
                ExternalMerchantGuid = request.ExternalMerchantGuid,
                StatusId = 1,
                StatusCode = "ACTIVE",
                StatusName = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM"
            };

            _mockMerchantService.Setup(x => x.CreateMerchantAsync(request, "SYSTEM"))
                .ReturnsAsync(merchantResponse);

            // Act
            var result = await _controller.CreateMerchant(request);

            // Assert
            result.Should().NotBeNull();
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var apiResponse = createdResult.Value.Should().BeOfType<ApiResponse<MerchantResponse>>().Subject;
            
            apiResponse.Success.Should().BeTrue();
            apiResponse.Message.Should().Be("Merchant created successfully");
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.Name.Should().Be("New Test Merchant");
            apiResponse.Data.ExternalMerchantId.Should().Be("EXT456");
        }

        [Fact]
        public async Task CreateMerchant_WithInvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "Duplicate Merchant",
                ExternalMerchantId = "EXT456"
            };

            SetupUserClaims(Guid.NewGuid(), isAdmin: true);

            _mockMerchantService.Setup(x => x.CreateMerchantAsync(request, "SYSTEM"))
                .ThrowsAsync(new InvalidOperationException("Merchant already exists"));

            // Act
            var result = await _controller.CreateMerchant(request);

            // Assert
            result.Should().NotBeNull();
            var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errorResponse = badRequestResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
            errorResponse.Message.Should().Contain("Merchant creation failed");
        }

        [Fact]
        public async Task CreateMerchant_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var request = new GenerateInitialApiKeyRequest
            {
                MerchantName = "Test Merchant",
                ExternalMerchantId = "EXT456"
            };

            SetupUserClaims(Guid.NewGuid(), isAdmin: true);

            _mockMerchantService.Setup(x => x.CreateMerchantAsync(request, "SYSTEM"))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateMerchant(request);

            // Assert
            result.Should().NotBeNull();
            var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            var errorResponse = objectResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
            errorResponse.Message.Should().Contain("Merchant creation failed");
        }

        #endregion

        #region UpdateMerchantStatus Tests

        [Fact]
        public void UpdateMerchantStatus_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the UpdateMerchantStatus method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("UpdateMerchantStatus", 
                new[] { typeof(Guid), typeof(UpdateMerchantStatusRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void UpdateMerchantStatusRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the UpdateMerchantStatusRequest model has the expected properties
            var request = new UpdateMerchantStatusRequest
            {
                StatusId = 1
            };

            request.StatusId.Should().Be(1);
        }

        [Fact]
        public async Task UpdateMerchantStatus_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateMerchantStatusRequest
            {
                StatusId = 1
            };

            var merchant = new Merchant
            {
                MerchantId = merchantId,
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            SetupUserClaims(merchantId, isAdmin: false);

            _mockMerchantService.Setup(x => x.UpdateMerchantStatusAsync(merchantId, request.StatusId))
                .ReturnsAsync(merchant);

            // Act
            var result = await _controller.UpdateMerchantStatus(merchantId, request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedMerchant = okResult.Value.Should().BeOfType<Merchant>().Subject;
            returnedMerchant.MerchantId.Should().Be(merchantId);
        }

        [Fact]
        public async Task UpdateMerchantStatus_WithNonExistentMerchant_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateMerchantStatusRequest
            {
                StatusId = 1
            };

            SetupUserClaims(merchantId, isAdmin: false);

            _mockMerchantService.Setup(x => x.UpdateMerchantStatusAsync(merchantId, request.StatusId))
                .ThrowsAsync(new KeyNotFoundException("Merchant not found"));

            // Act
            var result = await _controller.UpdateMerchantStatus(merchantId, request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateMerchantStatus_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateMerchantStatusRequest
            {
                StatusId = 1
            };

            SetupUserClaims(merchantId, isAdmin: false);

            _mockMerchantService.Setup(x => x.UpdateMerchantStatusAsync(merchantId, request.StatusId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.UpdateMerchantStatus(merchantId, request);

            // Assert
            result.Should().NotBeNull();
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region UpdateMerchant Tests

        [Fact]
        public void UpdateMerchant_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the UpdateMerchant method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("UpdateMerchant", 
                new[] { typeof(Guid), typeof(UpdateMerchantRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<ActionResult<MerchantResponse>>));
        }

        [Fact]
        public void UpdateMerchantRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the UpdateMerchantRequest model has the expected properties
            var request = new UpdateMerchantRequest
            {
                Name = "Updated Merchant Name"
            };

            request.Name.Should().Be("Updated Merchant Name");
        }

        [Fact]
        public async Task UpdateMerchant_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateMerchantRequest
            {
                Name = "Updated Merchant Name"
            };

            var updatedMerchant = new Merchant
            {
                MerchantId = merchantId,
                Name = request.Name,
                StatusId = 1,
                Status = new MerchantStatus
                {
                    Code = "ACTIVE",
                    Name = "Active"
                },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM"
            };

            _mockMerchantService.Setup(x => x.UpdateMerchantAsync(merchantId, request.Name, "SYSTEM"))
                .ReturnsAsync(updatedMerchant);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                merchantId, 
                "MERCHANT_UPDATED", 
                "merchant", 
                null, 
                It.IsAny<string>(), 
                "SYSTEM"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.UpdateMerchant(merchantId, request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<MerchantResponse>().Subject;
            returnedResponse.MerchantId.Should().Be(merchantId);
            returnedResponse.Name.Should().Be(request.Name);
        }

        [Fact]
        public async Task UpdateMerchant_WithNonExistentMerchant_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateMerchantRequest
            {
                Name = "Updated Merchant Name"
            };

            _mockMerchantService.Setup(x => x.UpdateMerchantAsync(merchantId, request.Name, "SYSTEM"))
                .ThrowsAsync(new KeyNotFoundException("Merchant not found"));

            // Act
            var result = await _controller.UpdateMerchant(merchantId, request);

            // Assert
            result.Should().NotBeNull();
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateMerchant_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new UpdateMerchantRequest
            {
                Name = "Updated Merchant Name"
            };

            _mockMerchantService.Setup(x => x.UpdateMerchantAsync(merchantId, request.Name, "SYSTEM"))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.UpdateMerchant(merchantId, request);

            // Assert
            result.Should().NotBeNull();
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GenerateApiKey (Complex Method) Tests

        [Fact]
        public async Task GenerateApiKey_WithValidMerchantRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new GenerateApiKeyRequest
            {
                MerchantId = merchantId,
                IsAdmin = false,
                OnboardingMetadata = new OnboardingMetadata
                {
                    AdminUserId = "admin-user",
                    OnboardingReference = "ref-123"
                }
            };

            SetupUserClaims(merchantId, isAdmin: false);
            SetupRequiredHeaders();
            SetupApiKeyHeaders(merchantId, "header-api-key");

            var merchant = new Merchant
            {
                MerchantId = merchantId,
                Name = "Test Merchant",
                StatusId = 1
            };

            var apiKeyResponse = new GenerateApiKeyResponse
            {
                ApiKey = "test-api-key",
                Secret = "test-secret",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "header-api-key",
                MerchantId = merchantId,
                Status = "ACTIVE"
            };

            _mockMerchantService.Setup(x => x.GetMerchantAsync(merchantId))
                .ReturnsAsync(merchant);

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync(It.IsAny<string>()))
                .ReturnsAsync(apiKeyInfo);

            _mockApiKeyService.Setup(x => x.GenerateApiKeyAsync(request))
                .ReturnsAsync(apiKeyResponse);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                merchantId,
                "API_KEY_GENERATED",
                "api_key",
                null,
                apiKeyResponse.ApiKey,
                "admin-user"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GenerateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GenerateApiKeyResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.ApiKey.Should().Be("test-api-key");
        }

        [Fact]
        public async Task GenerateApiKey_WithValidAdminRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new GenerateApiKeyRequest
            {
                MerchantId = merchantId,
                IsAdmin = true
            };

            SetupUserClaims(merchantId, isAdmin: true);
            SetupRequiredHeaders();
            SetupApiKeyHeaders(merchantId, "header-api-key");

            var apiKeyResponse = new GenerateApiKeyResponse
            {
                ApiKey = "admin-api-key",
                Secret = "admin-secret",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "header-api-key",
                MerchantId = merchantId,
                Status = "ACTIVE"
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("header-api-key"))
                .ReturnsAsync(apiKeyInfo);

            _mockApiKeyService.Setup(x => x.GenerateApiKeyAsync(request))
                .ReturnsAsync(apiKeyResponse);

            // Act
            var result = await _controller.GenerateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GenerateApiKeyResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.ApiKey.Should().Be("admin-api-key");
        }

        [Fact]
        public async Task GenerateApiKey_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new GenerateApiKeyRequest
            {
                MerchantId = merchantId,
                IsAdmin = false
            };

            SetupUserClaims(merchantId, isAdmin: false);
            SetupRequiredHeaders();
            SetupApiKeyHeaders(merchantId, "header-api-key");

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("header-api-key"))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GenerateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GenerateApiKey (Simple Method) Tests

        [Fact]
        public void GenerateApiKey_SimpleMethod_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the simple GenerateApiKey method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("GenerateApiKey", 
                new[] { typeof(Guid), typeof(GenerateApiKeyRequest), typeof(string) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<ActionResult<ApiKeyInfo>>));
        }

        [Fact]
        public async Task GenerateApiKey_SimpleMethod_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new GenerateApiKeyRequest
            {
                MerchantId = merchantId,
                IsAdmin = false
            };

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Status = "ACTIVE"
            };

            _mockMerchantService.Setup(x => x.GenerateApiKeyAsync(merchantId, request, null))
                .ReturnsAsync(apiKeyInfo);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                merchantId,
                "API_KEY_GENERATED",
                "api_key",
                null,
                apiKeyInfo.ApiKey,
                "SYSTEM"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GenerateApiKey(merchantId, request, null);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedApiKeyInfo = okResult.Value.Should().BeOfType<ApiKeyInfo>().Subject;
            returnedApiKeyInfo.ApiKey.Should().Be("test-api-key");
        }

        [Fact]
        public async Task GenerateApiKey_SimpleMethod_WithOnboardingMetadata_UsesAdminUserId()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new GenerateApiKeyRequest
            {
                MerchantId = merchantId,
                IsAdmin = false
            };

            var onboardingMetadataHeader = "{\"AdminUserId\":\"admin-user\",\"OnboardingReference\":\"ref-123\"}";

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId,
                Status = "ACTIVE"
            };

            _mockMerchantService.Setup(x => x.GenerateApiKeyAsync(merchantId, request, It.IsAny<OnboardingMetadata>()))
                .ReturnsAsync(apiKeyInfo);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                merchantId,
                "API_KEY_GENERATED",
                "api_key",
                null,
                apiKeyInfo.ApiKey,
                "admin-user"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.GenerateApiKey(merchantId, request, onboardingMetadataHeader);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedApiKeyInfo = okResult.Value.Should().BeOfType<ApiKeyInfo>().Subject;
            returnedApiKeyInfo.ApiKey.Should().Be("test-api-key");
        }

        [Fact]
        public async Task GenerateApiKey_SimpleMethod_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new GenerateApiKeyRequest
            {
                MerchantId = merchantId,
                IsAdmin = false
            };

            _mockMerchantService.Setup(x => x.GenerateApiKeyAsync(merchantId, request, null))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GenerateApiKey(merchantId, request, null);

            // Assert
            result.Should().NotBeNull();
            var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region RotateApiKey Tests

        [Fact]
        public void RotateApiKey_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the RotateApiKey method exists and is accessible
            var methodInfo = typeof(OnboardingController).GetMethod("RotateApiKey", 
                new[] { typeof(RotateApiKeyRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void RotateApiKeyRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the RotateApiKeyRequest model has the expected properties
            var request = new RotateApiKeyRequest
            {
                ApiKey = "test-api-key",
                MerchantId = Guid.NewGuid().ToString(),
                OnboardingMetadata = new OnboardingMetadata
                {
                    AdminUserId = "admin-user",
                    OnboardingReference = "ref-123"
                }
            };

            request.ApiKey.Should().Be("test-api-key");
            request.MerchantId.Should().NotBeNullOrEmpty();
            request.OnboardingMetadata.Should().NotBeNull();
        }

        [Fact]
        public async Task RotateApiKey_WithValidMerchantRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RotateApiKeyRequest
            {
                ApiKey = "old-api-key",
                MerchantId = merchantId.ToString(),
                OnboardingMetadata = new OnboardingMetadata
                {
                    AdminUserId = "admin-user",
                    OnboardingReference = "ref-123"
                }
            };

            SetupUserClaims(merchantId, isAdmin: false);

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "old-api-key",
                MerchantId = merchantId,
                Status = "ACTIVE",
                IsRevoked = false
            };

            var rotatedResponse = new GenerateApiKeyResponse
            {
                ApiKey = "new-api-key",
                Secret = "new-secret",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("old-api-key"))
                .ReturnsAsync(apiKeyInfo);

            _mockApiKeyService.Setup(x => x.RotateApiKeyAsync(merchantId.ToString(), request.OnboardingMetadata, "old-api-key"))
                .ReturnsAsync(rotatedResponse);

            _mockMerchantService.Setup(x => x.CreateAuditTrailAsync(
                merchantId,
                "API_KEY_ROTATED",
                "api_key",
                It.IsAny<string>(),
                It.IsAny<string>(),
                "admin-user"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RotateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GenerateApiKeyResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.ApiKey.Should().Be("new-api-key");
        }

        [Fact]
        public async Task RotateApiKey_WithValidAdminRequest_ReturnsOkResult()
        {
            // Arrange
            var request = new RotateApiKeyRequest
            {
                ApiKey = "admin-api-key",
                MerchantId = Guid.NewGuid().ToString(),
                OnboardingMetadata = new OnboardingMetadata
                {
                    AdminUserId = "admin-user",
                    OnboardingReference = "ref-123"
                }
            };

            SetupUserClaims(Guid.NewGuid(), isAdmin: true);

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "admin-api-key",
                Status = "admin",
                IsRevoked = false
            };

            var rotatedResponse = new GenerateApiKeyResponse
            {
                ApiKey = "new-admin-api-key",
                Secret = "new-admin-secret",
                ExpiresAt = DateTime.UtcNow.AddDays(30)
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("admin-api-key"))
                .ReturnsAsync(apiKeyInfo);

            _mockApiKeyService.Setup(x => x.RotateApiKeyAsync(request.MerchantId, request.OnboardingMetadata, "admin-api-key"))
                .ReturnsAsync(rotatedResponse);

            // Act
            var result = await _controller.RotateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<GenerateApiKeyResponse>>().Subject;
            apiResponse.Success.Should().BeTrue();
            apiResponse.Data.Should().NotBeNull();
            apiResponse.Data.ApiKey.Should().Be("new-admin-api-key");
        }

        [Fact]
        public async Task RotateApiKey_WithNonExistentApiKey_ReturnsNotFound()
        {
            // Arrange
            var request = new RotateApiKeyRequest
            {
                ApiKey = "non-existent-key",
                MerchantId = Guid.NewGuid().ToString()
            };

            SetupUserClaims(Guid.NewGuid(), isAdmin: false);

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("non-existent-key"))
                .ReturnsAsync(default(ApiKeyInfo?));

            // Act
            var result = await _controller.RotateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task RotateApiKey_WithRevokedApiKey_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RotateApiKeyRequest
            {
                ApiKey = "revoked-api-key",
                MerchantId = merchantId.ToString()
            };

            SetupUserClaims(merchantId, isAdmin: false);

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "revoked-api-key",
                MerchantId = merchantId,
                Status = "REVOKED",
                IsRevoked = true
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("revoked-api-key"))
                .ReturnsAsync(apiKeyInfo);

            // Act
            var result = await _controller.RotateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task RotateApiKey_WithInsufficientPermissions_ReturnsUnauthorized()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RotateApiKeyRequest
            {
                ApiKey = "admin-api-key",
                MerchantId = merchantId.ToString()
            };

            // Setup merchant claims but try to rotate admin key
            SetupUserClaims(merchantId, isAdmin: false);

            var apiKeyInfo = new ApiKeyInfo
            {
                ApiKey = "admin-api-key",
                Status = "admin",
                IsRevoked = false
            };

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("admin-api-key"))
                .ReturnsAsync(apiKeyInfo);

            // Act
            var result = await _controller.RotateApiKey(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task RotateApiKey_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new RotateApiKeyRequest
            {
                ApiKey = "test-api-key",
                MerchantId = merchantId.ToString()
            };

            SetupUserClaims(merchantId, isAdmin: false);

            _mockApiKeyService.Setup(x => x.GetApiKeyInfoAsync("test-api-key"))
                .ThrowsAsync(new Exception("Service error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _controller.RotateApiKey(request));
            exception.Message.Should().Be("Service error");
        }

        #endregion

        #endregion
    }
}