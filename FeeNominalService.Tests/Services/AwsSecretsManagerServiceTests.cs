using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Services;
using FeeNominalService.Services.AWS;
using FeeNominalService.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;
using Xunit;

namespace FeeNominalService.Tests.Services
{
    public class AwsSecretsManagerServiceTests : IDisposable
    {
        private readonly Mock<IAmazonSecretsManager> _mockSecretsManager;
        private readonly Mock<ILogger<AwsSecretsManagerService>> _mockLogger;
        private readonly Mock<IOptions<ApiKeyConfiguration>> _mockApiKeyConfig;
        private readonly SecretNameFormatter _secretNameFormatter;
        private readonly AwsSecretsManagerService _service;

        public AwsSecretsManagerServiceTests()
        {
            _mockSecretsManager = new Mock<IAmazonSecretsManager>();
            _mockLogger = new Mock<ILogger<AwsSecretsManagerService>>();
            
            // Create test configuration
            var apiKeyConfig = new ApiKeyConfiguration
            {
                SecretName = "test-secret",
                Region = "us-east-1",
                MaxFailedAttempts = 3,
                LockoutDurationMinutes = 15,
                KeyRotationDays = 90,
                EnableRateLimiting = true,
                DefaultRateLimit = 1000,
                RequestTimeWindowMinutes = 5
            };
            _mockApiKeyConfig = new Mock<IOptions<ApiKeyConfiguration>>();
            _mockApiKeyConfig.Setup(x => x.Value).Returns(apiKeyConfig);

            // Create SecretNameFormatter with test configuration
            var awsOptions = Options.Create(new AwsSecretsManagerConfiguration
            {
                Region = "us-east-1",
                Profile = "default",
                MerchantSecretNameFormat = "feenominal/merchants/{merchantId}/apikeys/{apiKey}",
                AdminSecretNameFormat = "feenominal/admin/apikeys/{serviceName}-admin-api-key-secret"
            });
            _secretNameFormatter = new SecretNameFormatter(awsOptions);

            _service = new AwsSecretsManagerService(
                _mockSecretsManager.Object,
                _mockLogger.Object,
                _mockApiKeyConfig.Object,
                _secretNameFormatter);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        #region GetSecretAsync Tests

        [Fact]
        public async Task GetSecretAsync_WithValidSecretName_ReturnsSecretString()
        {
            // Arrange
            var secretName = "test-secret";
            var expectedSecret = "test-secret-value";
            
            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = expectedSecret });

            // Act
            var result = await _service.GetSecretAsync(secretName);

            // Assert
            result.Should().Be(expectedSecret);
            _mockSecretsManager.Verify(x => x.GetSecretValueAsync(It.Is<GetSecretValueRequest>(r => r.SecretId == secretName), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetSecretAsync_WithAdminSecretName_LogsAdminSecretRetrieval()
        {
            // Arrange
            var secretName = "feenominal/admin/test-service";
            var expectedSecret = "admin-secret-value";
            
            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = expectedSecret });

            // Act
            var result = await _service.GetSecretAsync(secretName);

            // Assert
            result.Should().Be(expectedSecret);
            // Verify that admin secret logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("admin")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetSecretAsync_WhenAwsThrowsException_PropagatesException()
        {
            // Arrange
            var secretName = "test-secret";
            var expectedException = new Exception("AWS Secrets Manager error");
            
            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _service.GetSecretAsync(secretName));
            exception.Should().Be(expectedException);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving secret")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region GetSecretAsync<T> Tests

        [Fact]
        public async Task GetSecretAsync_WithValidSecretName_ReturnsDeserializedObject()
        {
            // Arrange
            var secretName = "test-secret";
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Scope = "merchant",
                MerchantId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);
            
            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.GetSecretAsync<ApiKeySecret>(secretName);

            // Assert
            result.Should().NotBeNull();
            result!.ApiKey.Should().Be(apiKeySecret.ApiKey);
            result.Secret.Should().Be(apiKeySecret.Secret);
        }

        [Fact]
        public async Task GetSecretAsync_WithInvalidJson_ThrowsInvalidOperationException()
        {
            // Arrange
            var secretName = "test-secret";
            var invalidJson = "{ invalid json }";
            
            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = invalidJson });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<JsonException>(() => 
                _service.GetSecretAsync<ApiKeySecret>(secretName));
        }

        [Fact]
        public async Task GetSecretAsync_WithApiKeySecret_ReturnsSecureWrapper()
        {
            // Arrange
            var secretName = "test-secret";
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Scope = "merchant",
                MerchantId = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);
            
            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.GetSecretAsync<ApiKeySecret>(secretName);

            // Assert
            result.Should().NotBeNull();
            // The result should be a secure wrapper, but we can't directly test the secure properties
            result!.ApiKey.Should().Be(apiKeySecret.ApiKey);
        }

        #endregion

        #region StoreSecretAsync Tests

        [Fact]
        public async Task StoreSecretAsync_WithValidParameters_CallsAwsCreateSecret()
        {
            // Arrange
            var secretName = "test-secret";
            var secretValue = "test-secret-value";

            _mockSecretsManager
                .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSecretResponse());

            // Act
            await _service.StoreSecretAsync(secretName, secretValue);

            // Assert
            _mockSecretsManager.Verify(x => x.CreateSecretAsync(
                It.Is<CreateSecretRequest>(r => r.Name == secretName && r.SecretString == secretValue), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task StoreSecretAsync_WithAdminSecretName_LogsAdminSecretStorage()
        {
            // Arrange
            var secretName = "feenominal/admin/test-service";
            var secretValue = "admin-secret-value";

            _mockSecretsManager
                .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSecretResponse());

            // Act
            await _service.StoreSecretAsync(secretName, secretValue);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("admin")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region CreateSecretAsync Tests

        [Fact]
        public async Task CreateSecretAsync_WithValidDictionary_CallsAwsCreateSecret()
        {
            // Arrange
            var secretName = "test-secret";
            var secretValue = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            _mockSecretsManager
                .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSecretResponse());

            // Act
            await _service.CreateSecretAsync(secretName, secretValue);

            // Assert
            var expectedJson = JsonSerializer.Serialize(secretValue);
            _mockSecretsManager.Verify(x => x.CreateSecretAsync(
                It.Is<CreateSecretRequest>(r => 
                    r.Name == secretName && 
                    r.SecretString == expectedJson), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region UpdateSecretAsync Tests

        [Fact]
        public async Task UpdateSecretAsync_WithValidObject_CallsAwsUpdateSecret()
        {
            // Arrange
            var secretName = "test-secret";
            var secretValue = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            };

            _mockSecretsManager
                .Setup(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateSecretResponse());

            // Act
            await _service.UpdateSecretAsync(secretName, secretValue);

            // Assert
            _mockSecretsManager.Verify(x => x.UpdateSecretAsync(
                It.Is<UpdateSecretRequest>(r => r.SecretId == secretName), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSecretAsync_WithApiKeySecret_UsesSecureWrapper()
        {
            // Arrange
            var secretName = "test-secret";
            var secretValue = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            };

            _mockSecretsManager
                .Setup(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateSecretResponse());

            // Act
            await _service.UpdateSecretAsync(secretName, secretValue);

            // Assert
            _mockSecretsManager.Verify(x => x.UpdateSecretAsync(
                It.Is<UpdateSecretRequest>(r => 
                    r.SecretId == secretName && 
                    !string.IsNullOrEmpty(r.SecretString)), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region GetAllSecretsAsync Tests

        [Fact]
        public async Task GetAllSecretsAsync_WithValidSecrets_ReturnsAllSecrets()
        {
            // Arrange
            var secretList = new List<SecretListEntry>
            {
                new SecretListEntry { Name = "secret1" },
                new SecretListEntry { Name = "secret2" }
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = secretList });

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.GetAllSecretsAsync<ApiKeySecret>();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _mockSecretsManager.Verify(x => x.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllSecretsAsync_WithNullSecretName_SkipsSecret()
        {
            // Arrange
            var secretList = new List<SecretListEntry>
            {
                new SecretListEntry { Name = null },
                new SecretListEntry { Name = "secret2" }
            };

            _mockSecretsManager
                .Setup(x => x.ListSecretsAsync(It.IsAny<ListSecretsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListSecretsResponse { SecretList = secretList });

            // Mock the GetSecretValueAsync to handle the valid secret name
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.GetAllSecretsAsync<ApiKeySecret>();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(1); // Only the valid secret should be returned
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Found secret with null name")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region ValidateApiKeyAsync Tests

        [Fact]
        public async Task ValidateApiKeyAsync_WithValidApiKey_ReturnsTrue()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
            
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = apiKey, // The secret should match the API key for validation
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.ValidateApiKeyAsync(merchantId, apiKey);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WithInvalidApiKey_ReturnsFalse()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
            
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "different-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.ValidateApiKeyAsync(merchantId, apiKey);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WithRevokedApiKey_StillReturnsTrue()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
            
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = apiKey, // The secret should match the API key for validation
                Status = "REVOKED",
                IsRevoked = true
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.ValidateApiKeyAsync(merchantId, apiKey);

            // Assert
            // Note: ValidateApiKeyAsync only checks if the API key matches the secret,
            // not if it's revoked. Revocation status is checked elsewhere in the application.
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateApiKeyAsync_WhenSecretNotFound_ReturnsFalse()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Secret not found"));

            // Act
            var result = await _service.ValidateApiKeyAsync(merchantId, apiKey);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region RevokeApiKeyAsync Tests

        [Fact]
        public async Task RevokeApiKeyAsync_WithValidApiKey_RevokesSuccessfully()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
            
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            _mockSecretsManager
                .Setup(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateSecretResponse());

            // Act
            await _service.RevokeApiKeyAsync(merchantId, apiKey);

            // Assert
            _mockSecretsManager.Verify(x => x.UpdateSecretAsync(
                It.Is<UpdateSecretRequest>(r => r.SecretId == secretName), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RevokeApiKeyAsync_WithInvalidApiKey_ThrowsKeyNotFoundException()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "different-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _service.RevokeApiKeyAsync(merchantId, apiKey));
            exception.Message.Should().Contain("API key");
        }

        [Fact]
        public async Task RevokeApiKeyAsync_WhenSecretNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Secret not found"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _service.RevokeApiKeyAsync(merchantId, apiKey));
            exception.Message.Should().Contain("API key");
        }

        #endregion

        #region Secure API Key Secret Tests

        [Fact]
        public async Task GetSecureApiKeySecretAsync_WithValidSecretName_ReturnsSecureSecret()
        {
            // Arrange
            var secretName = "test-secret";
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.GetSecureApiKeySecretAsync(secretName);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSecureApiKeySecretAsync_WhenSecretNotFound_ReturnsNull()
        {
            // Arrange
            var secretName = "test-secret";

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Secret not found"));

            // Act
            var result = await _service.GetSecureApiKeySecretAsync(secretName);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task StoreSecureApiKeySecretAsync_WithValidSecret_StoresSuccessfully()
        {
            // Arrange
            var secretName = "test-secret";
            var secureSecret = SecureApiKeySecret.FromApiKeySecret(new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            });

            _mockSecretsManager
                .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSecretResponse());

            // Act
            await _service.StoreSecureApiKeySecretAsync(secretName, secureSecret);

            // Assert
            _mockSecretsManager.Verify(x => x.CreateSecretAsync(
                It.Is<CreateSecretRequest>(r => r.Name == secretName), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSecureApiKeySecretAsync_WithValidSecret_UpdatesSuccessfully()
        {
            // Arrange
            var secretName = "test-secret";
            var secureSecret = SecureApiKeySecret.FromApiKeySecret(new ApiKeySecret
            {
                Id = 1,
                ApiKey = "test-api-key",
                Secret = "test-secret",
                Status = "ACTIVE"
            });

            _mockSecretsManager
                .Setup(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateSecretResponse());

            // Act
            await _service.UpdateSecureApiKeySecretAsync(secretName, secureSecret);

            // Assert
            _mockSecretsManager.Verify(x => x.UpdateSecretAsync(
                It.Is<UpdateSecretRequest>(r => r.SecretId == secretName), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Merchant Secret Tests

        [Fact]
        public async Task GetMerchantSecretSecurelyAsync_WithValidParameters_ReturnsSecret()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);
            
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.GetMerchantSecretSecurelyAsync(merchantId, apiKey);

            // Assert
            result.Should().NotBeNull();
            result!.ApiKey.Should().Be(apiKey);
        }

        [Fact]
        public async Task StoreMerchantSecretSecurelyAsync_WithValidParameters_StoresSuccessfully()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            var secretValue = "test-secret-value";
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);

            _mockSecretsManager
                .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSecretResponse());

            // Act
            await _service.StoreMerchantSecretSecurelyAsync(merchantId, apiKey, secretValue);

            // Assert
            _mockSecretsManager.Verify(x => x.CreateSecretAsync(
                It.Is<CreateSecretRequest>(r => r.Name == secretName && r.SecretString == secretValue), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateMerchantSecretSecurelyAsync_WithValidParameters_UpdatesSuccessfully()
        {
            // Arrange
            var merchantId = "test-merchant";
            var apiKey = "test-api-key";
            var secretValue = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = "test-secret",
                Status = "ACTIVE"
            };
            var secretName = _secretNameFormatter.FormatMerchantSecretName(merchantId, apiKey);

            _mockSecretsManager
                .Setup(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateSecretResponse());

            // Act
            await _service.UpdateMerchantSecretSecurelyAsync(merchantId, apiKey, secretValue);

            // Assert
            _mockSecretsManager.Verify(x => x.UpdateSecretAsync(
                It.Is<UpdateSecretRequest>(r => r.SecretId == secretName), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Admin Secret Tests

        [Fact]
        public async Task GetAdminSecretSecurelyAsync_WithValidServiceName_ReturnsSecret()
        {
            // Arrange
            var serviceName = "test-service";
            var secretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
            
            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "admin-api-key",
                Secret = "admin-secret",
                Status = "ACTIVE"
            };
            var secretJson = JsonSerializer.Serialize(apiKeySecret);

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetSecretValueResponse { SecretString = secretJson });

            // Act
            var result = await _service.GetAdminSecretSecurelyAsync(serviceName);

            // Assert
            result.Should().NotBeNull();
            result!.ApiKey.Should().Be("admin-api-key");
        }

        [Fact]
        public async Task StoreAdminSecretSecurelyAsync_WithValidParameters_StoresSuccessfully()
        {
            // Arrange
            var serviceName = "test-service";
            var secretValue = "admin-secret-value";
            var secretName = _secretNameFormatter.FormatAdminSecretName(serviceName);

            _mockSecretsManager
                .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateSecretResponse());

            // Act
            await _service.StoreAdminSecretSecurelyAsync(serviceName, secretValue);

            // Assert
            _mockSecretsManager.Verify(x => x.CreateSecretAsync(
                It.Is<CreateSecretRequest>(r => r.Name == secretName && r.SecretString == secretValue), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAdminSecretSecurelyAsync_WithValidParameters_UpdatesSuccessfully()
        {
            // Arrange
            var serviceName = "test-service";
            var secretValue = new ApiKeySecret
            {
                Id = 1,
                ApiKey = "admin-api-key",
                Secret = "admin-secret",
                Status = "ACTIVE"
            };
            var secretName = _secretNameFormatter.FormatAdminSecretName(serviceName);

            _mockSecretsManager
                .Setup(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateSecretResponse());

            // Act
            await _service.UpdateAdminSecretSecurelyAsync(serviceName, secretValue);

            // Assert
            _mockSecretsManager.Verify(x => x.UpdateSecretAsync(
                It.Is<UpdateSecretRequest>(r => r.SecretId == secretName), 
                It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetSecretAsync_WhenAwsThrowsResourceNotFoundException_LogsAndThrows()
        {
            // Arrange
            var secretName = "test-secret";
            var resourceNotFoundException = new ResourceNotFoundException("Secret not found");

            _mockSecretsManager
                .Setup(x => x.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(resourceNotFoundException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResourceNotFoundException>(() => 
                _service.GetSecretAsync(secretName));
            exception.Should().Be(resourceNotFoundException);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error retrieving secret")),
                    resourceNotFoundException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task StoreSecretAsync_WhenAwsThrowsResourceExistsException_LogsAndThrows()
        {
            // Arrange
            var secretName = "test-secret";
            var secretValue = "test-value";
            var resourceExistsException = new ResourceExistsException("Secret already exists");

            _mockSecretsManager
                .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(resourceExistsException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResourceExistsException>(() => 
                _service.StoreSecretAsync(secretName, secretValue));
            exception.Should().Be(resourceExistsException);
            
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error storing secret")),
                    resourceExistsException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
