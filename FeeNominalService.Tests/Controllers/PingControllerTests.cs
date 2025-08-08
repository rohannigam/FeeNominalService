using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Authorization;
using FluentAssertions;
using FeeNominalService.Controllers.V1;

namespace FeeNominalService.Tests.Controllers
{
    public class PingControllerTests
    {
        private readonly PingController _controller;

        public PingControllerTests()
        {
            _controller = new PingController();
        }

        #region Section 1: Constructor and Basic Setup Tests

        [Fact]
        public void PingController_Constructor_ShouldNotThrow()
        {
            // This test just verifies our constructor setup works
            _controller.Should().NotBeNull();
        }

        [Fact]
        public void PingController_ShouldHaveCorrectRouteAttribute()
        {
            // Test that the controller has the correct route attribute
            var routeAttribute = typeof(PingController).GetCustomAttributes(typeof(RouteAttribute), false)
                .FirstOrDefault() as RouteAttribute;
            
            routeAttribute.Should().NotBeNull();
            routeAttribute!.Template.Should().Be("api/v1/[controller]");
        }

        [Fact]
        public void PingController_ShouldHaveApiVersionAttribute()
        {
            // Test that the controller has the correct API version attribute
            var apiVersionAttribute = typeof(PingController).GetCustomAttributes(typeof(ApiVersionAttribute), false)
                .FirstOrDefault() as ApiVersionAttribute;
            
            apiVersionAttribute.Should().NotBeNull();
            apiVersionAttribute!.Versions.Should().Contain(new ApiVersion(1, 0));
        }

        [Fact]
        public void PingController_ShouldNotHaveAuthorizeAttribute()
        {
            // Test that the controller does NOT have an authorize attribute (it's public)
            var authorizeAttribute = typeof(PingController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .FirstOrDefault() as AuthorizeAttribute;
            
            authorizeAttribute.Should().BeNull();
        }

        #endregion

        #region Section 2: Get Method Tests

        [Fact]
        public void Get_MethodExists_ShouldBeAccessible()
        {
            // This test verifies that the Get method exists and is accessible
            var methodInfo = typeof(PingController).GetMethod("Get");
            
            methodInfo.Should().NotBeNull();
            methodInfo!.ReturnType.Should().Be(typeof(IActionResult));
        }

        [Fact]
        public void Get_ShouldHaveHttpGetAttribute()
        {
            // Test that the Get method has the correct HTTP attribute
            var methodInfo = typeof(PingController).GetMethod("Get");
            var httpGetAttribute = methodInfo!.GetCustomAttributes(typeof(HttpGetAttribute), false)
                .FirstOrDefault() as HttpGetAttribute;
            
            httpGetAttribute.Should().NotBeNull();
        }

        [Fact]
        public void Get_WithValidRequest_ReturnsOkResult()
        {
            // Arrange & Act
            var result = _controller.Get();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be("pong");
        }

        [Fact]
        public void Get_ShouldReturnStringValue()
        {
            // Arrange & Act
            var result = _controller.Get();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeOfType<string>();
        }

        [Fact]
        public void Get_ShouldReturnExactPongMessage()
        {
            // Arrange & Act
            var result = _controller.Get();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var responseValue = okResult.Value.Should().BeOfType<string>().Subject;
            responseValue.Should().Be("pong");
        }

        [Fact]
        public void Get_ShouldReturn200StatusCode()
        {
            // Arrange & Act
            var result = _controller.Get();

            // Assert
            result.Should().NotBeNull();
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        #endregion

        #region Section 3: Integration Style Tests

        [Fact]
        public void Get_ShouldBeConsistentAcrossMultipleCalls()
        {
            // Arrange & Act
            var result1 = _controller.Get();
            var result2 = _controller.Get();
            var result3 = _controller.Get();

            // Assert
            result1.Should().BeOfType<OkObjectResult>();
            result2.Should().BeOfType<OkObjectResult>();
            result3.Should().BeOfType<OkObjectResult>();
            
            var okResult1 = result1 as OkObjectResult;
            var okResult2 = result2 as OkObjectResult;
            var okResult3 = result3 as OkObjectResult;
            
            okResult1!.Value.Should().Be("pong");
            okResult2!.Value.Should().Be("pong");
            okResult3!.Value.Should().Be("pong");
        }

        [Fact]
        public void Get_ShouldNotRequireAuthentication()
        {
            // This test verifies that the endpoint is accessible without authentication
            // Since there's no [Authorize] attribute, this should work
            var result = _controller.Get();
            
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Get_ShouldBeFastAndResponsive()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var result = _controller.Get();
            stopwatch.Stop();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<OkObjectResult>();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(100); // Should be very fast
        }

        #endregion

        #region Section 4: Edge Case Tests

        [Fact]
        public async Task Get_ShouldHandleConcurrentRequests()
        {
            // Arrange
            var tasks = new List<Task<IActionResult>>();

            // Act - Simulate concurrent requests
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => _controller.Get()));
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().AllBeOfType<OkObjectResult>();
            results.Cast<OkObjectResult>().Should().AllSatisfy(r => r.Value.Should().Be("pong"));
        }

        [Fact]
        public void Get_ShouldNotModifyControllerState()
        {
            // Arrange
            var controller1 = new PingController();
            var controller2 = new PingController();

            // Act
            var result1 = controller1.Get();
            var result2 = controller2.Get();

            // Assert
            result1.Should().BeOfType<OkObjectResult>();
            result2.Should().BeOfType<OkObjectResult>();
            
            var okResult1 = result1 as OkObjectResult;
            var okResult2 = result2 as OkObjectResult;
            
            okResult1!.Value.Should().Be("pong");
            okResult2!.Value.Should().Be("pong");
        }

        #endregion
    }
} 