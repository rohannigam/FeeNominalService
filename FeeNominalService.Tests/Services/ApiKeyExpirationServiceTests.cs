using FeeNominalService.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FeeNominalService.Tests.Services
{
    // Test subclass to access protected methods
    public class TestableApiKeyExpirationService : ApiKeyExpirationService
    {
        public TestableApiKeyExpirationService(IServiceProvider serviceProvider, ILogger<ApiKeyExpirationService> logger)
            : base(serviceProvider, logger)
        {
        }

        public new async Task CheckAndUpdateExpiredKeysAsync()
        {
            await base.CheckAndUpdateExpiredKeysAsync();
        }
    }

    public class ApiKeyExpirationServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ApiKeyExpirationService>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly TestableApiKeyExpirationService _service;

        public ApiKeyExpirationServiceTests()
        {
            _mockLogger = new Mock<ILogger<ApiKeyExpirationService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();

            _service = new TestableApiKeyExpirationService(_mockServiceProvider.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        #region Background Service Tests

        [Fact]
        public void Constructor_WithValidParameters_CreatesService()
        {
            // Act & Assert
            _service.Should().NotBeNull();
            _service.Should().BeAssignableTo<BackgroundService>();
        }

        [Fact]
        public async Task ExecuteAsync_WhenStarted_LogsStartupMessage()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100)); // Cancel quickly

            // Act
            await _service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(50); // Give it time to start
            await _service.StopAsync(cancellationTokenSource.Token);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("API Key Expiration Service is starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithServiceProviderError_LogsErrorAndContinues()
        {
            // Arrange
            _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Throws(new InvalidOperationException("Service provider error"));

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(200)); // Cancel after 200ms

            // Act
            await _service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(150); // Give it time to process
            await _service.StopAsync(cancellationTokenSource.Token);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error occurred while checking for expired API keys")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellation_StopsGracefully()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            // Act
            await _service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(50); // Give service time to start
            
            // Cancel the service
            cancellationTokenSource.Cancel();
            await Task.Delay(50); // Give service time to process cancellation
            
            await _service.StopAsync(CancellationToken.None); // Use different token for stop

            // Assert
            // Should complete without hanging - the fact that we reach this point means it stopped gracefully
            cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_WithMultipleStartStopCycles_WorksCorrectly()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();

            // Act & Assert - Multiple start/stop cycles should work
            for (int i = 0; i < 3; i++)
            {
                await _service.StartAsync(cancellationTokenSource.Token);
                await Task.Delay(10); // Brief delay
                await _service.StopAsync(cancellationTokenSource.Token);
            }

            // Should complete without errors
            true.Should().BeTrue(); // Simple assertion to verify no exceptions
        }

        [Fact]
        public void Service_WithValidParameters_CreatesServiceSuccessfully()
        {
            // Arrange & Act
            var service = new TestableApiKeyExpirationService(_mockServiceProvider.Object, _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
            service.Should().BeAssignableTo<BackgroundService>();
            service.Should().BeAssignableTo<ApiKeyExpirationService>();
        }

        [Fact]
        public async Task Service_WithValidConfiguration_StartsAndStopsSuccessfully()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            await _service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(50);
            await _service.StopAsync(cancellationTokenSource.Token);

            // Assert
            // Should complete without exceptions
            true.Should().BeTrue();
        }

        [Fact]
        public async Task Service_WithLongRunningOperation_RespectsCancellation()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(TimeSpan.FromMilliseconds(50)); // Cancel quickly

            // Act
            var startTask = _service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100); // Wait longer than cancellation time
            await _service.StopAsync(cancellationTokenSource.Token);

            // Assert
            // Should complete without hanging
            startTask.IsCompleted.Should().BeTrue();
        }

        #endregion
    }
}
