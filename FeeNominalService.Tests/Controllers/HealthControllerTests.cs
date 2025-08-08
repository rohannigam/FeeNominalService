using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using FeeNominalService.Controllers.Common;
using FeeNominalService.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;

namespace FeeNominalService.Tests.Controllers
{
    public class HealthControllerTests : IDisposable
    {
        private readonly HealthController _controller;
        private readonly ApplicationDbContext _dbContext;

        public HealthControllerTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            // Create configuration with the required value
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:Schema"] = "public"
                })
                .Build();
            
            _dbContext = new ApplicationDbContext(options, configuration);
            _controller = new HealthController(_dbContext);
        }

        [Fact]
        public void HealthController_Constructor_ShouldNotThrow()
        {
            // This test just verifies our constructor setup works
            _controller.Should().NotBeNull();
        }

        [Fact]
        public void Liveness_ShouldReturnOkWithCorrectData()
        {
            // Act
            var result = _controller.Liveness();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Readiness_ShouldReturnObjectResult()
        {
            // Act
            var result = await _controller.Readiness();

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().BeOneOf(200, 503); // Can be either success or failure with in-memory DB
            objectResult.Value.Should().NotBeNull();
        }

        [Fact]
        public void Health_ShouldReturnOkWithCorrectData()
        {
            // Act
            var result = _controller.Health();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public void Liveness_ShouldHaveAllowAnonymousAttribute()
        {
            // Act & Assert
            var methodInfo = typeof(HealthController).GetMethod("Liveness");
            var attributes = methodInfo!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false);
            attributes.Should().HaveCount(1);
        }

        [Fact]
        public void Readiness_ShouldHaveAllowAnonymousAttribute()
        {
            // Act & Assert
            var methodInfo = typeof(HealthController).GetMethod("Readiness");
            var attributes = methodInfo!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false);
            attributes.Should().HaveCount(1);
        }

        [Fact]
        public void Health_ShouldHaveAllowAnonymousAttribute()
        {
            // Act & Assert
            var methodInfo = typeof(HealthController).GetMethod("Health");
            var attributes = methodInfo!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false);
            attributes.Should().HaveCount(1);
        }

        [Fact]
        public void Liveness_ShouldHaveHttpGetAttribute()
        {
            // Act & Assert
            var methodInfo = typeof(HealthController).GetMethod("Liveness");
            var attributes = methodInfo!.GetCustomAttributes(typeof(HttpGetAttribute), false);
            attributes.Should().HaveCount(1);
            
            var httpGetAttribute = attributes[0] as HttpGetAttribute;
            httpGetAttribute!.Template.Should().Be("live");
        }

        [Fact]
        public void Readiness_ShouldHaveHttpGetAttribute()
        {
            // Act & Assert
            var methodInfo = typeof(HealthController).GetMethod("Readiness");
            var attributes = methodInfo!.GetCustomAttributes(typeof(HttpGetAttribute), false);
            attributes.Should().HaveCount(1);
            
            var httpGetAttribute = attributes[0] as HttpGetAttribute;
            httpGetAttribute!.Template.Should().Be("ready");
        }

        [Fact]
        public void Health_ShouldHaveHttpGetAttribute()
        {
            // Act & Assert
            var methodInfo = typeof(HealthController).GetMethod("Health");
            var attributes = methodInfo!.GetCustomAttributes(typeof(HttpGetAttribute), false);
            attributes.Should().HaveCount(1);
            
            var httpGetAttribute = attributes[0] as HttpGetAttribute;
            httpGetAttribute!.Template.Should().BeNullOrEmpty(); // No template for root endpoint
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
        }
    }
} 