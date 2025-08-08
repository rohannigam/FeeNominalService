using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using FeeNominalService.Controllers.V1;
using FeeNominalService.Services;
using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Models.Common;
using FeeNominalService.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace FeeNominalService.Tests.Controllers
{
    public class SurchargeControllerTests
    {
        private readonly Mock<ILogger<SurchargeController>> _mockLogger;
        private readonly Mock<ISurchargeTransactionService> _mockSurchargeTransactionService;
        private readonly SurchargeController _controller;
        private readonly DefaultHttpContext _httpContext;

        public SurchargeControllerTests()
        {
            _mockLogger = new Mock<ILogger<SurchargeController>>();
            _mockSurchargeTransactionService = new Mock<ISurchargeTransactionService>();

            _controller = new SurchargeController(
                _mockSurchargeTransactionService.Object,
                _mockLogger.Object);

            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = _httpContext
            };
        }

        [Fact]
        public void SurchargeController_Constructor_ShouldNotThrow()
        {
            // This test just verifies our constructor setup works
            _controller.Should().NotBeNull();
            _mockLogger.Should().NotBeNull();
            _mockSurchargeTransactionService.Should().NotBeNull();
        }

        [Fact]
        public void SurchargeController_ShouldHaveCorrectRouteAttribute()
        {
            // Test that the controller has the correct route attribute
            var routeAttribute = typeof(SurchargeController).GetCustomAttributes(typeof(RouteAttribute), false)
                .FirstOrDefault() as RouteAttribute;
            
            routeAttribute.Should().NotBeNull();
            routeAttribute!.Template.Should().Be("api/v1/surcharge");
        }

        [Fact]
        public void SurchargeController_ShouldHaveApiVersionAttribute()
        {
            // Test that the controller has the correct API version attribute
            var apiVersionAttribute = typeof(SurchargeController).GetCustomAttributes(typeof(ApiVersionAttribute), false)
                .FirstOrDefault() as ApiVersionAttribute;
            
            apiVersionAttribute.Should().NotBeNull();
            apiVersionAttribute!.Versions.Should().Contain(new ApiVersion(1, 0));
        }

        [Fact]
        public void SurchargeController_ShouldHaveAuthorizeAttribute()
        {
            // Test that the controller has the correct authorize attribute
            var authorizeAttribute = typeof(SurchargeController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        private void SetupUserClaims(Guid merchantId, string? apiKey = null, bool isAdmin = false)
        {
            var claims = new List<Claim>
            {
                new Claim("MerchantId", merchantId.ToString()),
                new Claim("Scope", isAdmin ? "admin" : "merchant"),
                new Claim("AllowedEndpoints", "/api/v1/surcharge/*")
            };

            if (isAdmin)
            {
                claims.Add(new Claim("IsAdmin", "true"));
            }

            if (!string.IsNullOrEmpty(apiKey))
            {
                claims.Add(new Claim("ApiKey", apiKey));
            }

            var identity = new ClaimsIdentity(claims, "Test", "MerchantId", "Scope");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;
        }

        #region Section 1: ProcessAuth Tests

        [Fact]
        public void ProcessAuth_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the ProcessAuth method exists and is accessible
            var methodInfo = typeof(SurchargeController).GetMethod("ProcessAuth", 
                new[] { typeof(SurchargeAuthRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void SurchargeAuthRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the SurchargeAuthRequest model has the expected properties
            var request = new SurchargeAuthRequest
            {
                CorrelationId = "test-correlation-id",
                MerchantTransactionId = "merchant-tx-123",
                BinValue = "123456",
                SurchargeProcessor = "test-processor",
                Amount = 1000.00m,
                TotalAmount = 1050.00m,
                Country = "USA",
                PostalCode = "12345",
                ProviderCode = "TEST"
            };

            request.CorrelationId.Should().Be("test-correlation-id");
            request.MerchantTransactionId.Should().Be("merchant-tx-123");
            request.BinValue.Should().Be("123456");
            request.SurchargeProcessor.Should().Be("test-processor");
            request.Amount.Should().Be(1000.00m);
            request.TotalAmount.Should().Be(1050.00m);
            request.Country.Should().Be("USA");
            request.PostalCode.Should().Be("12345");
            request.ProviderCode.Should().Be("TEST");
        }

        [Fact]
        public async Task ProcessAuth_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeAuthRequest
            {
                CorrelationId = "test-correlation-id",
                BinValue = "123456",
                SurchargeProcessor = "test-processor",
                Amount = 1000.00m,
                Country = "USA",
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            var response = new SurchargeAuthResponse
            {
                SurchargeTransactionId = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                Status = "AUTHORIZED",
                OriginalAmount = request.Amount,
                SurchargeAmount = 50.00m,
                TotalAmount = 1050.00m,
                Provider = "TEST",
                ProviderType = "PAYMENT",
                ProviderCode = "TEST",
                ProcessedAt = DateTime.UtcNow
            };

            _mockSurchargeTransactionService.Setup(x => x.ProcessAuthAsync(request, merchantId, "test-api-key"))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ProcessAuth(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<SurchargeAuthResponse>().Subject;
            
            returnedResponse.SurchargeTransactionId.Should().Be(response.SurchargeTransactionId);
            returnedResponse.CorrelationId.Should().Be(request.CorrelationId);
            returnedResponse.Status.Should().Be("AUTHORIZED");
        }

        [Fact]
        public async Task ProcessAuth_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeAuthRequest
            {
                CorrelationId = "test-correlation-id",
                BinValue = "123456",
                SurchargeProcessor = "test-processor",
                Amount = 1000.00m,
                Country = "USA",
                ProviderCode = "TEST"
            };

            // Don't set up any claims

            // Act
            var result = await _controller.ProcessAuth(request);

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
        public async Task ProcessAuth_WithInvalidMerchantIdFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeAuthRequest
            {
                CorrelationId = "test-correlation-id",
                BinValue = "123456",
                SurchargeProcessor = "test-processor",
                Amount = 1000.00m,
                Country = "USA",
                ProviderCode = "TEST"
            };

            // Set up claims with invalid merchant ID format
            var claims = new List<Claim>
            {
                new Claim("MerchantId", "invalid-guid-format"),
                new Claim("Scope", "merchant")
            };

            var identity = new ClaimsIdentity(claims, "Test", "MerchantId", "Scope");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            // Act
            var result = await _controller.ProcessAuth(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid merchant ID format");
        }

        [Fact]
        public async Task ProcessAuth_WithSurchargeException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeAuthRequest
            {
                CorrelationId = "test-correlation-id",
                BinValue = "123456",
                SurchargeProcessor = "test-processor",
                Amount = 1000.00m,
                Country = "USA",
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            _mockSurchargeTransactionService.Setup(x => x.ProcessAuthAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new FeeNominalService.Exceptions.SurchargeException("Invalid BIN value"));

            // Act
            var result = await _controller.ProcessAuth(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessAuth_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeAuthRequest
            {
                CorrelationId = "test-correlation-id",
                BinValue = "123456",
                SurchargeProcessor = "test-processor",
                Amount = 1000.00m,
                Country = "USA",
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            _mockSurchargeTransactionService.Setup(x => x.ProcessAuthAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.ProcessAuth(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid operation");
        }

        [Fact]
        public async Task ProcessAuth_WithGeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeAuthRequest
            {
                CorrelationId = "test-correlation-id",
                BinValue = "123456",
                SurchargeProcessor = "test-processor",
                Amount = 1000.00m,
                Country = "USA",
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            _mockSurchargeTransactionService.Setup(x => x.ProcessAuthAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.ProcessAuth(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            json.Should().Contain("An error occurred while processing the surcharge authorization");
        }

        #endregion

        #region Section 2: ProcessSale Tests

        [Fact]
        public void ProcessSale_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the ProcessSale method exists and is accessible
            var methodInfo = typeof(SurchargeController).GetMethod("ProcessSale", 
                new[] { typeof(SurchargeSaleRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void SurchargeSaleRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the SurchargeSaleRequest model has the expected properties
            var request = new SurchargeSaleRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            request.CorrelationId.Should().Be("test-correlation-id");
            request.SurchargeTransactionId.Should().NotBe(Guid.Empty);
            request.ProviderCode.Should().Be("TEST");
        }

        [Fact]
        public async Task ProcessSale_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var surchargeTransactionId = Guid.NewGuid();
            var request = new SurchargeSaleRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = surchargeTransactionId,
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            var response = new SurchargeSaleResponse
            {
                SurchargeTransactionId = Guid.NewGuid(),
                OriginalSurchargeTransactionId = surchargeTransactionId,
                CorrelationId = request.CorrelationId,
                Status = "COMPLETED",
                Amount = 1000.00m,
                ProviderTransactionId = "provider-tx-123",
                ProviderCode = "TEST",
                ProviderType = "PAYMENT",
                ProcessedAt = DateTime.UtcNow
            };

            _mockSurchargeTransactionService.Setup(x => x.ProcessSaleAsync(request, merchantId, "test-api-key"))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ProcessSale(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<SurchargeSaleResponse>().Subject;
            
            returnedResponse.SurchargeTransactionId.Should().Be(response.SurchargeTransactionId);
            returnedResponse.CorrelationId.Should().Be(request.CorrelationId);
            returnedResponse.Status.Should().Be("COMPLETED");
        }

        [Fact]
        public async Task ProcessSale_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeSaleRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            // Setup claims without MerchantId
            var claims = new List<Claim>
            {
                new Claim("Scope", "merchant"),
                new Claim("ApiKey", "test-api-key")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            // Act
            var result = await _controller.ProcessSale(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessSale_WithInvalidMerchantIdFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeSaleRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            // Setup claims with invalid MerchantId format
            var claims = new List<Claim>
            {
                new Claim("MerchantId", "invalid-guid"),
                new Claim("Scope", "merchant"),
                new Claim("ApiKey", "test-api-key")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            // Act
            var result = await _controller.ProcessSale(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessSale_WithSurchargeException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeSaleRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            _mockSurchargeTransactionService.Setup(x => x.ProcessSaleAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new FeeNominalService.Exceptions.SurchargeException("Surcharge error"));

            // Act
            var result = await _controller.ProcessSale(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessSale_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeSaleRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            _mockSurchargeTransactionService.Setup(x => x.ProcessSaleAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.ProcessSale(request);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessSale_WithGeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeSaleRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            _mockSurchargeTransactionService.Setup(x => x.ProcessSaleAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new Exception("General error"));

            // Act
            var result = await _controller.ProcessSale(request);

            // Assert
            result.Should().NotBeNull();
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region Section 3: ProcessRefund Tests

        [Fact]
        public void ProcessRefund_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the ProcessRefund method exists and is accessible
            var methodInfo = typeof(SurchargeController).GetMethod("ProcessRefund", 
                new[] { typeof(SurchargeRefundRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void SurchargeRefundRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the SurchargeRefundRequest model has the expected properties
            var request = new SurchargeRefundRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            request.CorrelationId.Should().Be("test-correlation-id");
            request.SurchargeTransactionId.Should().NotBe(Guid.Empty);
            request.ProviderCode.Should().Be("TEST");
        }

        [Fact]
        public async Task ProcessRefund_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var surchargeTransactionId = Guid.NewGuid();
            var request = new SurchargeRefundRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = surchargeTransactionId,
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            var response = new SurchargeRefundResponse
            {
                SurchargeTransactionId = Guid.NewGuid(),
                OriginalSurchargeTransactionId = surchargeTransactionId,
                CorrelationId = request.CorrelationId,
                Status = "REFUNDED",
                RefundAmount = 25.00m,
                OriginalAmount = 100.00m,
                ProviderCode = "TEST",
                ProviderType = "PAYMENT",
                ProcessedAt = DateTime.UtcNow
            };

            _mockSurchargeTransactionService.Setup(x => x.ProcessRefundAsync(request, merchantId, "test-api-key"))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ProcessRefund(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<SurchargeRefundResponse>().Subject;
            
            returnedResponse.SurchargeTransactionId.Should().Be(response.SurchargeTransactionId);
            returnedResponse.OriginalSurchargeTransactionId.Should().Be(surchargeTransactionId);
            returnedResponse.CorrelationId.Should().Be(request.CorrelationId);
            returnedResponse.Status.Should().Be("REFUNDED");
        }

        [Fact]
        public async Task ProcessRefund_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeRefundRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            // Don't setup user claims - this will cause merchant ID to be missing

            // Act
            var result = await _controller.ProcessRefund(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Merchant ID not found in claims");
        }

        [Fact]
        public async Task ProcessRefund_WithInvalidMerchantIdFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeRefundRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            // Setup claims with invalid merchant ID format
            var claims = new List<Claim>
            {
                new Claim("MerchantId", "invalid-guid-format"),
                new Claim("Scope", "merchant"),
                new Claim("AllowedEndpoints", "/api/v1/surcharge/*")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            // Act
            var result = await _controller.ProcessRefund(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid merchant ID format");
        }

        [Fact]
        public async Task ProcessRefund_WithSurchargeException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeRefundRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");
            _mockSurchargeTransactionService.Setup(x => x.ProcessRefundAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new FeeNominalService.Exceptions.SurchargeException("Refund failed", "REFUND_ERROR"));

            // Act
            var result = await _controller.ProcessRefund(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessRefund_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeRefundRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");
            _mockSurchargeTransactionService.Setup(x => x.ProcessRefundAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.ProcessRefund(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid operation");
        }

        [Fact]
        public async Task ProcessRefund_WithGeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeRefundRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");
            _mockSurchargeTransactionService.Setup(x => x.ProcessRefundAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.ProcessRefund(request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            json.Should().Contain("An error occurred while processing the surcharge refund");
        }

        #endregion

        #region Section 4: ProcessCancel Tests

        [Fact]
        public void ProcessCancel_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the ProcessCancel method exists and is accessible
            var methodInfo = typeof(SurchargeController).GetMethod("ProcessCancel", 
                new[] { typeof(SurchargeCancelRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void SurchargeCancelRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the SurchargeCancelRequest model has the expected properties
            var request = new SurchargeCancelRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            request.CorrelationId.Should().Be("test-correlation-id");
            request.SurchargeTransactionId.Should().NotBe(Guid.Empty);
            request.ProviderCode.Should().Be("TEST");
        }

        [Fact]
        public async Task ProcessCancel_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var surchargeTransactionId = Guid.NewGuid();
            var request = new SurchargeCancelRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = surchargeTransactionId,
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");

            var response = new SurchargeCancelResponse
            {
                SurchargeTransactionId = Guid.NewGuid(),
                OriginalTransactionId = surchargeTransactionId.ToString(),
                CorrelationId = request.CorrelationId,
                Status = "CANCELLED",
                Provider = "TEST",
                ProcessedAt = DateTime.UtcNow
            };

            _mockSurchargeTransactionService.Setup(x => x.ProcessCancelAsync(request, merchantId, "test-api-key"))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.ProcessCancel(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<SurchargeCancelResponse>().Subject;
            
            returnedResponse.SurchargeTransactionId.Should().Be(response.SurchargeTransactionId);
            returnedResponse.OriginalTransactionId.Should().Be(surchargeTransactionId.ToString());
            returnedResponse.CorrelationId.Should().Be(request.CorrelationId);
            returnedResponse.Status.Should().Be("CANCELLED");
        }

        [Fact]
        public async Task ProcessCancel_WithMissingMerchantIdInClaims_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeCancelRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            // Don't setup user claims - this will cause merchant ID to be missing

            // Act
            var result = await _controller.ProcessCancel(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Merchant ID not found in claims");
        }

        [Fact]
        public async Task ProcessCancel_WithInvalidMerchantIdFormat_ReturnsBadRequest()
        {
            // Arrange
            var request = new SurchargeCancelRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            // Setup claims with invalid merchant ID format
            var claims = new List<Claim>
            {
                new Claim("MerchantId", "invalid-guid-format"),
                new Claim("Scope", "merchant"),
                new Claim("AllowedEndpoints", "/api/v1/surcharge/*")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = principal;

            // Act
            var result = await _controller.ProcessCancel(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid merchant ID format");
        }

        [Fact]
        public async Task ProcessCancel_WithSurchargeException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeCancelRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");
            _mockSurchargeTransactionService.Setup(x => x.ProcessCancelAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new FeeNominalService.Exceptions.SurchargeException("Cancel failed", "CANCEL_ERROR"));

            // Act
            var result = await _controller.ProcessCancel(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ProcessCancel_WithInvalidOperationException_ReturnsBadRequest()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeCancelRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");
            _mockSurchargeTransactionService.Setup(x => x.ProcessCancelAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new InvalidOperationException("Invalid operation"));

            // Act
            var result = await _controller.ProcessCancel(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(badRequestResult.Value);
            json.Should().Contain("Invalid operation");
        }

        [Fact]
        public async Task ProcessCancel_WithGeneralException_ReturnsInternalServerError()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new SurchargeCancelRequest
            {
                CorrelationId = "test-correlation-id",
                SurchargeTransactionId = Guid.NewGuid(),
                ProviderCode = "TEST"
            };

            SetupUserClaims(merchantId, "test-api-key");
            _mockSurchargeTransactionService.Setup(x => x.ProcessCancelAsync(request, merchantId, "test-api-key"))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.ProcessCancel(request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(500);
            objectResult.Value.Should().NotBeNull();
            // Test the error message using JSON serialization
            var json = System.Text.Json.JsonSerializer.Serialize(objectResult.Value);
            json.Should().Contain("An error occurred while processing the surcharge cancellation");
        }

        #endregion

        #region Section 5: BulkSaleComplete Tests

        [Fact]
        public void BulkSaleComplete_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the BulkSaleComplete method exists and is accessible
            var methodInfo = typeof(SurchargeController).GetMethod("BulkSaleComplete", 
                new[] { typeof(BulkSaleCompleteRequest) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public void BulkSaleComplete_HasCorrectAttributes()
        {
            // This test verifies that the BulkSaleComplete method has the correct attributes
            var methodInfo = typeof(SurchargeController).GetMethod("BulkSaleComplete", 
                new[] { typeof(BulkSaleCompleteRequest) });
            
            methodInfo.Should().NotBeNull();
            
            var authorizeAttribute = methodInfo!.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().NotBeNull();
            authorizeAttribute!.Policy.Should().Be("ApiKeyAccess");
        }

        [Fact]
        public void BulkSaleCompleteRequest_Model_ShouldHaveRequiredProperties()
        {
            // This test verifies that the BulkSaleCompleteRequest model has the expected properties
            var request = new BulkSaleCompleteRequest
            {
                Sales = new List<BulkSaleItem>
                {
                    new BulkSaleItem
                    {
                        SurchargeTransactionId = Guid.NewGuid(),
                        ProviderCode = "TEST"
                    }
                }
            };

            request.Sales.Should().HaveCount(1);
            request.Sales[0].SurchargeTransactionId.Should().NotBe(Guid.Empty);
            request.Sales[0].ProviderCode.Should().Be("TEST");
        }

        [Fact]
        public async Task BulkSaleComplete_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var request = new BulkSaleCompleteRequest
            {
                Sales = new List<BulkSaleItem>
                {
                    new BulkSaleItem
                    {
                        SurchargeTransactionId = Guid.NewGuid(),
                        ProviderCode = "TEST"
                    }
                }
            };

            SetupUserClaims(merchantId, "test-api-key", isAdmin: true);

            var response = new BulkSaleCompleteResponse
            {
                BatchId = "batch-123",
                TotalCount = 1,
                SuccessCount = 1,
                FailureCount = 0,
                Results = new List<SurchargeSaleResponse>
                {
                    new SurchargeSaleResponse
                    {
                        SurchargeTransactionId = Guid.NewGuid(),
                        OriginalSurchargeTransactionId = request.Sales[0].SurchargeTransactionId!.Value,
                        CorrelationId = "tx-1",
                        Status = "COMPLETED",
                        Amount = 1000.00m,
                        ProviderTransactionId = "provider-tx-123",
                        ProviderCode = "TEST",
                        ProviderType = "PAYMENT",
                        ProcessedAt = DateTime.UtcNow
                    }
                },
                ProcessedAt = DateTime.UtcNow
            };

            _mockSurchargeTransactionService.Setup(x => x.ProcessBulkSaleCompleteAsync(request))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.BulkSaleComplete(request);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedResponse = okResult.Value.Should().BeOfType<BulkSaleCompleteResponse>().Subject;
            
            returnedResponse.BatchId.Should().Be("batch-123");
            returnedResponse.TotalCount.Should().Be(1);
            returnedResponse.SuccessCount.Should().Be(1);
            returnedResponse.FailureCount.Should().Be(0);
        }

        #endregion

        #region Section 6: GetTransactionById Tests

        [Fact]
        public void GetTransactionById_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the GetTransactionById method exists and is accessible
            var methodInfo = typeof(SurchargeController).GetMethod("GetTransactionById", 
                new[] { typeof(Guid) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public async Task GetTransactionById_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var transactionId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            var transaction = new SurchargeTransaction
            {
                Id = transactionId,
                CorrelationId = "test-correlation-id",
                Status = SurchargeTransactionStatus.Completed,
                Amount = 1000.00m,
                RequestPayload = System.Text.Json.JsonDocument.Parse("{}"),
                ProviderConfigId = Guid.NewGuid(),
                OperationType = SurchargeOperationType.Auth
            };

            _mockSurchargeTransactionService.Setup(x => x.GetTransactionByIdAsync(transactionId, merchantId))
                .ReturnsAsync(transaction);

            // Act
            var result = await _controller.GetTransactionById(transactionId);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTransaction = okResult.Value.Should().BeOfType<SurchargeTransaction>().Subject;
            
            returnedTransaction.Id.Should().Be(transactionId);
            returnedTransaction.CorrelationId.Should().Be("test-correlation-id");
            returnedTransaction.Status.Should().Be(SurchargeTransactionStatus.Completed);
        }

        [Fact]
        public async Task GetTransactionById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            var transactionId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            _mockSurchargeTransactionService.Setup(x => x.GetTransactionByIdAsync(transactionId, merchantId))
                .ReturnsAsync((SurchargeTransaction?)null);

            // Act
            var result = await _controller.GetTransactionById(transactionId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region Section 7: GetTransactions Tests

        [Fact]
        public void GetTransactions_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the GetTransactions method exists and is accessible
            var methodInfo = typeof(SurchargeController).GetMethod("GetTransactions", 
                new[] { typeof(int), typeof(int), typeof(string), typeof(string) });
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(Task<IActionResult>));
        }

        [Fact]
        public async Task GetTransactions_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var merchantId = Guid.NewGuid();
            SetupUserClaims(merchantId);

            var transactions = new List<SurchargeTransaction>
            {
                new SurchargeTransaction
                {
                    Id = Guid.NewGuid(),
                    CorrelationId = "test-correlation-id-1",
                    Status = SurchargeTransactionStatus.Completed,
                    Amount = 1000.00m,
                    RequestPayload = System.Text.Json.JsonDocument.Parse("{}"),
                    ProviderConfigId = Guid.NewGuid(),
                    OperationType = SurchargeOperationType.Auth
                },
                new SurchargeTransaction
                {
                    Id = Guid.NewGuid(),
                    CorrelationId = "test-correlation-id-2",
                    Status = SurchargeTransactionStatus.Pending,
                    Amount = 2000.00m,
                    RequestPayload = System.Text.Json.JsonDocument.Parse("{}"),
                    ProviderConfigId = Guid.NewGuid(),
                    OperationType = SurchargeOperationType.Sale
                }
            };

            _mockSurchargeTransactionService.Setup(x => x.GetTransactionsByMerchantAsync(merchantId, 1, 20, null, null))
                .ReturnsAsync((transactions, 2));

            // Act
            var result = await _controller.GetTransactions(1, 20, null, null);

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
            
            // Verify the response has the expected structure using reflection
            var responseType = okResult.Value!.GetType();
            responseType.GetProperty("Transactions").Should().NotBeNull();
            responseType.GetProperty("Pagination").Should().NotBeNull();
        }

        #endregion
    }
} 