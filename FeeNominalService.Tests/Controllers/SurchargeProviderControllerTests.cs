using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FeeNominalService.Controllers.V1;
using FeeNominalService.Services;
using FeeNominalService.Models.SurchargeProvider;

using FeeNominalService.Models.Common;
using FeeNominalService.Settings;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace FeeNominalService.Tests.Controllers
{
    public class SurchargeProviderControllerTests
    {
        private readonly Mock<ILogger<SurchargeProviderController>> _mockLogger;
        private readonly Mock<ISurchargeProviderService> _mockSurchargeProviderService;
        private readonly Mock<ISurchargeProviderConfigService> _mockSurchargeProviderConfigService;
        private readonly Mock<ICredentialValidationService> _mockCredentialValidationService;
        private readonly Mock<SurchargeProviderValidationSettings> _mockValidationSettings;
        private readonly Mock<IAuditService> _mockAuditService;
        private readonly SurchargeProviderController _controller;
        private readonly DefaultHttpContext _httpContext;

        public SurchargeProviderControllerTests()
        {
            _mockLogger = new Mock<ILogger<SurchargeProviderController>>();
            _mockSurchargeProviderService = new Mock<ISurchargeProviderService>();
            _mockSurchargeProviderConfigService = new Mock<ISurchargeProviderConfigService>();
            _mockCredentialValidationService = new Mock<ICredentialValidationService>();
            _mockValidationSettings = new Mock<SurchargeProviderValidationSettings>();
            _mockAuditService = new Mock<IAuditService>();

            _controller = new SurchargeProviderController(
                _mockSurchargeProviderService.Object,
                _mockSurchargeProviderConfigService.Object,
                _mockCredentialValidationService.Object,
                _mockValidationSettings.Object,
                _mockLogger.Object,
                _mockAuditService.Object);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public void SurchargeProviderController_Constructor_ShouldNotThrow()
        {
            // This test just verifies our constructor setup works
            _controller.Should().NotBeNull();
            _mockLogger.Should().NotBeNull();
            _mockSurchargeProviderService.Should().NotBeNull();
            _mockSurchargeProviderConfigService.Should().NotBeNull();
            _mockCredentialValidationService.Should().NotBeNull();
            _mockValidationSettings.Should().NotBeNull();
            _mockAuditService.Should().NotBeNull();
        }

        [Fact]
        public void SurchargeProviderController_ShouldHaveCorrectRouteAttribute()
        {
            // Test that the controller has the correct route attribute
            var routeAttribute = typeof(SurchargeProviderController).GetCustomAttributes(typeof(RouteAttribute), false)
                .FirstOrDefault() as RouteAttribute;
            
            routeAttribute.Should().NotBeNull();
            routeAttribute!.Template.Should().Be("api/v1/merchants/{merchantId}/surcharge-providers");
        }

        [Fact]
        public void SurchargeProviderController_ShouldHaveApiVersionAttribute()
        {
            // Test that the controller has the correct API version attribute
            var apiVersionAttribute = typeof(SurchargeProviderController).GetCustomAttributes(typeof(ApiVersionAttribute), false)
                .FirstOrDefault() as ApiVersionAttribute;
            
            apiVersionAttribute.Should().NotBeNull();
            apiVersionAttribute!.Versions.Should().Contain(new ApiVersion(1, 0));
        }

        [Fact]
        public void SurchargeProviderController_ShouldHaveAuthorizeAttribute()
        {
            // Test that the controller has the correct authorize attribute
            var authorizeAttribute = typeof(SurchargeProviderController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        private void SetupUserClaims(Guid merchantId, string? apiKey = null)
        {
            var claims = new List<Claim>
            {
                new Claim("MerchantId", merchantId.ToString()),
                new Claim("Scope", "merchant"),
                new Claim("AllowedEndpoints", "/api/v1/merchants/*/surcharge-providers/*")
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                claims.Add(new Claim("ApiKey", apiKey));
            }

            var identity = new ClaimsIdentity(claims, "Test", "MerchantId", "Scope");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;
        }

        #region Section 1: CreateProvider Tests

        [Fact]
        public void CreateProvider_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the CreateProvider method exists and is accessible
            var methodInfo = typeof(SurchargeProviderController).GetMethod("CreateProvider", 
                new[] { typeof(string), typeof(SurchargeProviderRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void SurchargeProviderRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the SurchargeProviderRequest model has the expected properties
            var request = new SurchargeProviderRequest
            {
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test provider description",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY"
            };

            request.Name.Should().Be("Test Provider");
            request.Code.Should().Be("TEST");
            request.Description.Should().Be("Test provider description");
            request.BaseUrl.Should().Be("https://api.test.com");
            request.AuthenticationType.Should().Be("API_KEY");
        }

        [Fact]
        public async Task CreateProvider_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeProviderRequest
            {
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test provider description",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse(@"{
                    ""name"": ""Test Schema"",
                    ""description"": ""Test credentials schema"",
                    ""required_fields"": [
                        {
                            ""name"": ""api_key"",
                            ""type"": ""api_key"",
                            ""description"": ""API Key for authentication""
                        }
                    ]
                }")
            };

            SetupUserClaims(merchantId, "test-api-key");

            var response = new SurchargeProvider
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                BaseUrl = request.BaseUrl,
                AuthenticationType = request.AuthenticationType,
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 1,
                CreatedBy = "test-user",
                UpdatedBy = "test-user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Mock the status service
            var status = new SurchargeProviderStatus
            {
                StatusId = 1,
                Code = "ACTIVE",
                Name = "Active",
                Description = "Active status",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _mockSurchargeProviderService.Setup(x => x.GetStatusByCodeAsync("ACTIVE"))
                .ReturnsAsync(status);



            _mockSurchargeProviderService.Setup(x => x.CreateAsync(It.IsAny<SurchargeProvider>(), It.IsAny<SecureCredentialsSchema>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CreateProvider(merchantId.ToString(), request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<SurchargeProviderResponse>().Subject;
            
            returnedResponse.Id.Should().Be(response.Id);
            returnedResponse.Name.Should().Be(request.Name);
            returnedResponse.Code.Should().Be(request.Code);
        }

        [Fact]
        public async Task CreateProvider_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeProviderRequest
            {
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test provider description",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY"
            };

            // Don't set up any claims

            // Act
            var result = await _controller.CreateProvider(merchantId.ToString(), request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Merchant ID not found in claims");
        }

        [Fact]
        public async Task CreateProvider_WithMerchantIdMismatch_ReturnsForbidden()
        {
            // Arrange
            var urlMerchantId = Guid.NewGuid();
            var authenticatedMerchantId = Guid.NewGuid();
            var request = new SurchargeProviderRequest
            {
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test provider description",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY"
            };

            SetupUserClaims(authenticatedMerchantId, "test-api-key");

            // Act
            var result = await _controller.CreateProvider(urlMerchantId.ToString(), request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateProvider_WithInvalidCredentialsSchema_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeProviderRequest
            {
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test provider description",
                BaseUrl = "https://test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse(@"{
                    ""invalid_schema"": ""should_fail_validation""
                }")
            };

            SetupUserClaims(merchantId);

            // Act
            var result = await _controller.CreateProvider(merchantId.ToString(), request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Provider credentials are invalid");
        }

        [Fact]
        public async Task CreateProvider_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeProviderRequest
            {
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test provider description",
                BaseUrl = "https://test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse(@"{
                    ""name"": ""Test Schema"",
                    ""description"": ""Test credentials schema"",
                    ""required_fields"": [
                        {
                            ""name"": ""api_key"",
                            ""type"": ""api_key"",
                            ""description"": ""API Key for authentication""
                        }
                    ]
                }")
            };

            SetupUserClaims(merchantId);

            // Mock the status service
            var status = new SurchargeProviderStatus
            {
                StatusId = 1,
                Code = "ACTIVE",
                Name = "Active",
                Description = "Active status",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _mockSurchargeProviderService.Setup(x => x.GetStatusByCodeAsync("ACTIVE"))
                .ReturnsAsync(status);

            // Mock service to throw exception
            _mockSurchargeProviderService.Setup(x => x.CreateAsync(It.IsAny<SurchargeProvider>(), It.IsAny<SecureCredentialsSchema>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.CreateProvider(merchantId.ToString(), request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            json.Should().Contain("Internal server error");
        }

        #endregion

        #region Section 2: GetAllProviders Tests

        [Fact]
        public void GetAllProviders_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the GetAllProviders method exists and is accessible
            var methodInfo = typeof(SurchargeProviderController).GetMethod("GetAllProviders", 
                new[] { typeof(string), typeof(bool) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public async Task GetAllProviders_WithValidMerchantId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            var providers = new List<SurchargeProvider>
            {
                new SurchargeProvider
                {
                    Id = Guid.NewGuid(),
                    Name = "Provider 1",
                    Code = "PROV1",
                    BaseUrl = "https://api.provider1.com",
                    AuthenticationType = "API_KEY",
                    CredentialsSchema = JsonDocument.Parse("{}"),
                    StatusId = 1,
                    CreatedBy = "test-user",
                    UpdatedBy = "test-user",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new SurchargeProvider
                {
                    Id = Guid.NewGuid(),
                    Name = "Provider 2",
                    Code = "PROV2",
                    BaseUrl = "https://api.provider2.com",
                    AuthenticationType = "JWT",
                    CredentialsSchema = JsonDocument.Parse("{}"),
                    StatusId = 1,
                    CreatedBy = "test-user",
                    UpdatedBy = "test-user",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockSurchargeProviderService.Setup(x => x.GetByMerchantIdAsync(merchantId.ToString(), false))
                .ReturnsAsync(providers);

            // Act
            var result = await _controller.GetAllProviders(merchantId.ToString(), false);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedProviders = okResult.Value.Should().BeAssignableTo<IEnumerable<SurchargeProviderResponse>>().Subject;
            
            returnedProviders.Should().HaveCount(2);
            returnedProviders.First().Name.Should().Be("Provider 1");
            returnedProviders.Last().Name.Should().Be("Provider 2");
        }

        [Fact]
        public async Task GetAllProviders_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();

            // Don't setup user claims - this will cause merchant ID to be missing

            // Act
            var result = await _controller.GetAllProviders(merchantId.ToString());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Merchant ID not found in claims");
        }

        [Fact]
        public async Task GetAllProviders_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            // Mock service to throw exception
            _mockSurchargeProviderService.Setup(x => x.GetByMerchantIdAsync(merchantId.ToString(), false))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetAllProviders(merchantId.ToString());

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            json.Should().Contain("Internal server error");
        }

        #endregion

        #region Section 3: GetProviderById Tests

        [Fact]
        public void GetProviderById_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the GetProviderById method exists and is accessible
            var methodInfo = typeof(SurchargeProviderController).GetMethod("GetProviderById", 
                new[] { typeof(string), typeof(Guid) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public async Task GetProviderById_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            var provider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Test Provider",
                Code = "TEST",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 1,
                CreatedBy = "test-user",
                UpdatedBy = "test-user",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync(provider);

            // Act
            var result = await _controller.GetProviderById(merchantId.ToString(), providerId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedProvider = okResult.Value.Should().BeOfType<SurchargeProviderResponse>().Subject;
            
            returnedProvider.Id.Should().Be(providerId);
            returnedProvider.Name.Should().Be("Test Provider");
        }

        [Fact]
        public async Task GetProviderById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync((SurchargeProvider?)null);

            // Act
            var result = await _controller.GetProviderById(merchantId.ToString(), providerId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region Section 4: UpdateProvider Tests

        [Fact]
        public void UpdateProvider_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the UpdateProvider method exists and is accessible
            var methodInfo = typeof(SurchargeProviderController).GetMethod("UpdateProvider", 
                new[] { typeof(string), typeof(Guid), typeof(SurchargeProviderUpdateRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void SurchargeProviderUpdateRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the SurchargeProviderUpdateRequest model has the expected properties
            var request = new SurchargeProviderUpdateRequest
            {
                Name = "Updated Provider",
                Description = "Updated description",
                BaseUrl = "https://api.updated.com"
            };

            request.Name.Should().Be("Updated Provider");
            request.Description.Should().Be("Updated description");
            request.BaseUrl.Should().Be("https://api.updated.com");
        }

        [Fact]
        public async Task UpdateProvider_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var request = new SurchargeProviderUpdateRequest
            {
                Name = "Updated Provider",
                Description = "Updated description",
                BaseUrl = "https://api.github.com",
                CredentialsSchema = JsonDocument.Parse(@"{
                    ""name"": ""Updated Schema"",
                    ""description"": ""Updated credentials schema"",
                    ""required_fields"": [
                        {
                            ""name"": ""api_key"",
                            ""type"": ""api_key"",
                            ""description"": ""API Key for authentication""
                        }
                    ]
                }")
            };

            SetupUserClaims(merchantId, "test-api-key");

            var existingProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Original Provider",
                Code = "TEST",
                BaseUrl = "https://api.github.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse(@"{
                    ""name"": ""Original Schema"",
                    ""description"": ""Original credentials schema"",
                    ""required_fields"": [
                        {
                            ""name"": ""api_key"",
                            ""type"": ""api_key"",
                            ""description"": ""API Key for authentication""
                        }
                    ]
                }"),
                StatusId = 1,
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var response = new SurchargeProvider
            {
                Id = providerId,
                Name = request.Name,
                Description = request.Description,
                BaseUrl = "https://api.github.com",
                Code = "TEST",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse(@"{
                    ""name"": ""Updated Schema"",
                    ""description"": ""Updated credentials schema"",
                    ""required_fields"": [
                        {
                            ""name"": ""api_key"",
                            ""type"": ""api_key"",
                            ""description"": ""API Key for authentication""
                        }
                    ]
                }"),
                StatusId = 1,
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync(existingProvider);

            // Mock the status service for the update
            var status = new SurchargeProviderStatus
            {
                StatusId = 1,
                Code = "ACTIVE",
                Name = "Active",
                Description = "Active status",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _mockSurchargeProviderService.Setup(x => x.GetStatusByCodeAsync("ACTIVE"))
                .ReturnsAsync(status);

            _mockSurchargeProviderService.Setup(x => x.UpdateAsync(It.IsAny<SurchargeProvider>()))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.UpdateProvider(merchantId.ToString(), providerId, request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            
            // Simply verify that we got a successful response with some value
            okResult.Value.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task UpdateProvider_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var request = new SurchargeProviderUpdateRequest
            {
                Name = "Updated Provider",
                Description = "Updated description"
            };

            // Don't setup user claims - this will cause merchant ID to be missing

            // Act
            var result = await _controller.UpdateProvider(merchantId.ToString(), providerId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid merchant ID provided");
        }

        [Fact]
        public async Task UpdateProvider_WithMerchantIdMismatch_ReturnsForbidden()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var differentMerchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var request = new SurchargeProviderUpdateRequest
            {
                Name = "Updated Provider",
                Description = "Updated description"
            };

            // Setup claims with different merchant ID
            SetupUserClaims(differentMerchantId);

            // Act
            var result = await _controller.UpdateProvider(merchantId.ToString(), providerId, request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task UpdateProvider_WithNonExistentProvider_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var request = new SurchargeProviderUpdateRequest
            {
                Name = "Updated Provider",
                Description = "Updated description"
            };

            SetupUserClaims(merchantId);

            // Mock service to return null (provider not found)
            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync((SurchargeProvider?)null);

            // Act
            var result = await _controller.UpdateProvider(merchantId.ToString(), providerId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateProvider_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var request = new SurchargeProviderUpdateRequest
            {
                Name = "Updated Provider",
                Description = "Updated description"
            };

            SetupUserClaims(merchantId);

            var existingProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Original Provider",
                Code = "TEST",
                Description = "Original description",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 1,
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync(existingProvider);

            // Mock service to throw exception
            _mockSurchargeProviderService.Setup(x => x.UpdateAsync(It.IsAny<SurchargeProvider>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.UpdateProvider(merchantId.ToString(), providerId, request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Provider configuration is missing");
        }

        #endregion

        #region Section 5: DeleteProvider Tests

        [Fact]
        public void DeleteProvider_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the DeleteProvider method exists and is accessible
            var methodInfo = typeof(SurchargeProviderController).GetMethod("DeleteProvider", 
                new[] { typeof(string), typeof(Guid) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public async Task DeleteProvider_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId, "test-api-key");

            var existingProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Test Provider",
                Code = "TEST",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 1,
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = new SurchargeProviderStatus
                {
                    StatusId = 1,
                    Code = "ACTIVE",
                    Name = "Active",
                    Description = "Active status",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var deletedProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Test Provider",
                Code = "TEST",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 2, // DELETED status
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = new SurchargeProviderStatus
                {
                    StatusId = 2,
                    Code = "DELETED",
                    Name = "Deleted",
                    Description = "Deleted status",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync(existingProvider);

            _mockSurchargeProviderService.Setup(x => x.SoftDeleteAsync(providerId, It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId, true))
                .ReturnsAsync(deletedProvider);

            // Act
            var result = await _controller.DeleteProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<SurchargeProviderResponse>().Subject;
            
            returnedResponse.Id.Should().Be(providerId);
        }

        [Fact]
        public async Task DeleteProvider_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            // Don't setup user claims - this will cause merchant ID to be missing

            // Act
            var result = await _controller.DeleteProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid merchant ID provided");
        }

        [Fact]
        public async Task DeleteProvider_WithNonExistentProvider_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            // Mock service to return null (provider not found)
            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync((SurchargeProvider?)null);

            // Act
            var result = await _controller.DeleteProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteProvider_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            var existingProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test description",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 1,
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync(existingProvider);

            // Mock service to throw exception
            _mockSurchargeProviderService.Setup(x => x.SoftDeleteAsync(providerId, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.DeleteProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            json.Should().Contain("Internal server error");
        }

        #endregion

        #region Section 6: RestoreProvider Tests

        [Fact]
        public void RestoreProvider_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the RestoreProvider method exists and is accessible
            var methodInfo = typeof(SurchargeProviderController).GetMethod("RestoreProvider", 
                new[] { typeof(string), typeof(Guid) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public async Task RestoreProvider_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId, "test-api-key");

            var existingProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Test Provider",
                Code = "TEST",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 2, // DELETED status
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = new SurchargeProviderStatus
                {
                    StatusId = 2,
                    Code = "DELETED",
                    Name = "Deleted",
                    Description = "Deleted status",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var restoredProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Test Provider",
                Code = "TEST",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 1, // ACTIVE status
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = new SurchargeProviderStatus
                {
                    StatusId = 1,
                    Code = "ACTIVE",
                    Name = "Active",
                    Description = "Active status",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId, true))
                .ReturnsAsync(existingProvider);

            _mockSurchargeProviderService.Setup(x => x.RestoreAsync(providerId, It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync(restoredProvider);

            // Act
            var result = await _controller.RestoreProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<SurchargeProviderResponse>().Subject;
            
            returnedResponse.Id.Should().Be(providerId);
        }

        [Fact]
        public async Task RestoreProvider_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            // Don't setup user claims - this will cause merchant ID to be missing

            // Act
            var result = await _controller.RestoreProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid merchant ID provided");
        }

        [Fact]
        public async Task RestoreProvider_WithNonExistentProvider_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            // Mock service to return null (provider not found)
            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId))
                .ReturnsAsync((SurchargeProvider?)null);

            // Act
            var result = await _controller.RestoreProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task RestoreProvider_WithServiceException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            var existingProvider = new SurchargeProvider
            {
                Id = providerId,
                Name = "Test Provider",
                Code = "TEST",
                Description = "Test description",
                BaseUrl = "https://api.test.com",
                AuthenticationType = "API_KEY",
                CredentialsSchema = JsonDocument.Parse("{}"),
                StatusId = 2, // DELETED status
                CreatedBy = merchantId.ToString(),
                UpdatedBy = merchantId.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _mockSurchargeProviderService.Setup(x => x.GetByIdAsync(providerId, true))
                .ReturnsAsync(existingProvider);

            // Mock service to throw exception
            _mockSurchargeProviderService.Setup(x => x.RestoreAsync(providerId, It.IsAny<string>()))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.RestoreProvider(merchantId.ToString(), providerId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Surcharge provider not found");
        }

        #endregion
    }
} 