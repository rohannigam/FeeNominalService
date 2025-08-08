using FeeNominalService.Models.Configuration;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Repositories;
using FeeNominalService.Services;
using FeeNominalService.Services.AWS;
using FeeNominalService.Utils;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace FeeNominalService.Tests.Services
{
    public class RequestSigningServiceTests : IDisposable
    {
        private readonly Mock<IApiKeyRepository> _mockApiKeyRepository;
        private readonly Mock<IMerchantRepository> _mockMerchantRepository;
        private readonly Mock<IAwsSecretsManagerService> _mockSecretsManager;
        private readonly Mock<ILogger<RequestSigningService>> _mockLogger;
        private readonly Mock<IOptions<ApiKeyConfiguration>> _mockApiKeyConfig;
        private readonly SecretNameFormatter _secretNameFormatter;
        private readonly RequestSigningService _service;
        private readonly ApiKeyConfiguration _apiKeyConfig;

        public RequestSigningServiceTests()
        {
            _mockApiKeyRepository = new Mock<IApiKeyRepository>();
            _mockMerchantRepository = new Mock<IMerchantRepository>();
            _mockSecretsManager = new Mock<IAwsSecretsManagerService>();
            _mockLogger = new Mock<ILogger<RequestSigningService>>();

            // Create test configuration
            _apiKeyConfig = new ApiKeyConfiguration
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
            _mockApiKeyConfig.Setup(x => x.Value).Returns(_apiKeyConfig);

            // Create SecretNameFormatter with test configuration
            var awsOptions = Options.Create(new AwsSecretsManagerConfiguration
            {
                Region = "us-east-1",
                Profile = "default",
                MerchantSecretNameFormat = "feenominal/merchants/{merchantId}/apikeys/{apiKey}",
                AdminSecretNameFormat = "feenominal/admin/apikeys/{serviceName}-admin-api-key-secret"
            });
            _secretNameFormatter = new SecretNameFormatter(awsOptions);

            _service = new RequestSigningService(
                _mockApiKeyRepository.Object,
                _mockMerchantRepository.Object,
                _mockSecretsManager.Object,
                _mockLogger.Object,
                _mockApiKeyConfig.Object,
                _secretNameFormatter);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        #region ValidateTimestampAndNonce Tests

        [Fact]
        public void ValidateTimestampAndNonce_WithValidTimestampAndNonce_ReturnsTrue()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();

            // Act
            var result = _service.ValidateTimestampAndNonce(timestamp, nonce);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateTimestampAndNonce_WithInvalidTimestamp_ReturnsFalse()
        {
            // Arrange
            var timestamp = "invalid-timestamp";
            var nonce = Guid.NewGuid().ToString();

            // Act
            var result = _service.ValidateTimestampAndNonce(timestamp, nonce);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateTimestampAndNonce_WithExpiredTimestamp_ReturnsFalse()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();

            // Act
            var result = _service.ValidateTimestampAndNonce(timestamp, nonce);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateTimestampAndNonce_WithFutureTimestamp_ReturnsFalse()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.AddMinutes(10).ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();

            // Act
            var result = _service.ValidateTimestampAndNonce(timestamp, nonce);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateTimestampAndNonce_WithReusedNonce_ReturnsFalse()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();

            // Act
            var result1 = _service.ValidateTimestampAndNonce(timestamp, nonce);
            var result2 = _service.ValidateTimestampAndNonce(timestamp, nonce);

            // Assert
            result1.Should().BeTrue();
            result2.Should().BeFalse();
        }

        #endregion

        #region GenerateSignatureAsync Tests

        [Fact]
        public async Task GenerateSignatureAsync_WithValidParameters_ReturnsValidSignature()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var secret = "test-secret";

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = secret,
                Status = "ACTIVE"
            };

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Act
            var result = await _service.GenerateSignatureAsync(merchantId, apiKey, timestamp, nonce);

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // Verify the signature is valid by recreating it
            var data = $"{timestamp}|{nonce}|{merchantId}|{apiKey}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var expectedSignature = Convert.ToBase64String(expectedHash);
            
            result.Should().Be(expectedSignature);
        }

        [Fact]
        public async Task GenerateSignatureAsync_WithNullSecret_ThrowsKeyNotFoundException()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync((ApiKeySecret?)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _service.GenerateSignatureAsync(merchantId, apiKey, timestamp, nonce));
            exception.Message.Should().Contain("Secret not found");
        }

        [Fact]
        public async Task GenerateSignatureAsync_WithRevokedSecret_StillGeneratesSignature()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var secret = "test-secret";

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = secret,
                Status = "REVOKED",
                IsRevoked = true
            };

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Act
            var result = await _service.GenerateSignatureAsync(merchantId, apiKey, timestamp, nonce);

            // Assert
            // Note: GenerateSignatureAsync doesn't check if the secret is revoked,
            // it only checks if the secret exists. Revocation is checked in ValidateRequestAsync.
            result.Should().NotBeNullOrEmpty();
            
            // Verify the signature is valid by recreating it
            var data = $"{timestamp}|{nonce}|{merchantId}|{apiKey}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var expectedSignature = Convert.ToBase64String(expectedHash);
            
            result.Should().Be(expectedSignature);
        }

        #endregion

        #region ValidateRequestAsync Tests

        [Fact]
        public async Task ValidateRequestAsync_WithValidRequest_ReturnsTrue()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var secret = "test-secret";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "ACTIVE",
                MerchantId = Guid.Parse(merchantId),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = secret,
                Status = "ACTIVE"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Generate expected signature
            var data = $"{timestamp}|{nonce}|{merchantId}|{apiKey}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var expectedSignature = Convert.ToBase64String(expectedHash);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, expectedSignature);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithInvalidSignature_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var invalidSignature = "invalid-signature";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "ACTIVE",
                MerchantId = Guid.Parse(merchantId),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = "test-secret",
                Status = "ACTIVE"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, invalidSignature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithNonExistentMerchant_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var signature = "test-signature";

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new KeyNotFoundException("Merchant not found"));

            _mockMerchantRepository
                .Setup(x => x.GetByExternalIdAsync(merchantId))
                .ReturnsAsync((Merchant?)null);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, signature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithExternalMerchantId_FindsMerchant()
        {
            // Arrange
            var externalMerchantId = "ext-123";
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var secret = "test-secret";

            var merchant = new Merchant
            {
                MerchantId = Guid.NewGuid(),
                ExternalMerchantId = externalMerchantId,
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "ACTIVE",
                MerchantId = merchant.MerchantId,
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = secret,
                Status = "ACTIVE"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new KeyNotFoundException("Merchant not found"));

            _mockMerchantRepository
                .Setup(x => x.GetByExternalIdAsync(externalMerchantId))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Generate expected signature
            var data = $"{timestamp}|{nonce}|{merchant.MerchantId:D}|{apiKey}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expectedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var expectedSignature = Convert.ToBase64String(expectedHash);

            // Act
            var result = await _service.ValidateRequestAsync(externalMerchantId, apiKey, timestamp, nonce, requestBody, expectedSignature);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithInactiveApiKey_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var signature = "test-signature";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "INACTIVE", // Inactive status
                MerchantId = Guid.Parse(merchantId),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, signature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithNonExistentApiKey_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var signature = "test-signature";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync((ApiKey?)null);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, signature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithRevokedSecret_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var signature = "test-signature";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "ACTIVE",
                MerchantId = Guid.Parse(merchantId),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = "test-secret",
                Status = "REVOKED",
                IsRevoked = true
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, signature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithExpiredTimestamp_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.AddMinutes(-10).ToString("yyyy-MM-ddTHH:mm:ssZ"); // Expired timestamp
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var signature = "test-signature";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "ACTIVE",
                MerchantId = Guid.Parse(merchantId),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = "test-secret",
                Status = "ACTIVE"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, signature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithInvalidTimestamp_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = "invalid-timestamp";
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var signature = "test-signature";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "ACTIVE",
                MerchantId = Guid.Parse(merchantId),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = "test-secret",
                Status = "ACTIVE"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, signature);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task ValidateRequestAsync_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var signature = "test-signature";

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, signature);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GenerateSignatureAsync_WhenExceptionOccurs_ThrowsException()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ThrowsAsync(new Exception("AWS error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _service.GenerateSignatureAsync(merchantId, apiKey, timestamp, nonce));
            exception.Message.Should().Contain("Secret not found");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task ValidateRequestAsync_WithGeneratedSignature_ReturnsTrue()
        {
            // Arrange
            var merchantId = Guid.NewGuid().ToString();
            var apiKey = "test-api-key";
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var nonce = Guid.NewGuid().ToString();
            var requestBody = "{\"test\":\"data\"}";
            var secret = "test-secret";

            var merchant = new Merchant
            {
                MerchantId = Guid.Parse(merchantId),
                ExternalMerchantId = "ext-123",
                Name = "Test Merchant",
                StatusId = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeyEntity = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = apiKey,
                Name = "Test API Key",
                Status = "ACTIVE",
                MerchantId = Guid.Parse(merchantId),
                RateLimit = 1000,
                AllowedEndpoints = new[] { "/api/test" },
                ExpirationDays = 90,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = "test"
            };

            var apiKeySecret = new ApiKeySecret
            {
                Id = 1,
                ApiKey = apiKey,
                Secret = secret,
                Status = "ACTIVE"
            };

            _mockMerchantRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync(merchant);

            _mockApiKeyRepository
                .Setup(x => x.GetByKeyAsync(apiKey))
                .ReturnsAsync(apiKeyEntity);

            _mockSecretsManager
                .Setup(x => x.GetSecretAsync<ApiKeySecret>(It.IsAny<string>()))
                .ReturnsAsync(apiKeySecret);

            // Generate signature using the service
            var generatedSignature = await _service.GenerateSignatureAsync(merchantId, apiKey, timestamp, nonce);

            // Act
            var result = await _service.ValidateRequestAsync(merchantId, apiKey, timestamp, nonce, requestBody, generatedSignature);

            // Assert
            result.Should().BeTrue();
        }

        #endregion
    }
}
