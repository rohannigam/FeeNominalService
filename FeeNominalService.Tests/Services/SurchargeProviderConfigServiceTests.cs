using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Repositories;
using FeeNominalService.Services;
using FeeNominalService.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace FeeNominalService.Tests.Services;

public class SurchargeProviderConfigServiceTests
{
    private readonly Mock<ISurchargeProviderConfigRepository> _mockRepository;
    private readonly Mock<ILogger<SurchargeProviderConfigService>> _mockLogger;
    private readonly SurchargeProviderConfigService _service;

    public SurchargeProviderConfigServiceTests()
    {
        _mockRepository = new Mock<ISurchargeProviderConfigRepository>();
        _mockLogger = new Mock<ILogger<SurchargeProviderConfigService>>();
        _service = new SurchargeProviderConfigService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnConfig()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig(configId);

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(config);

        // Act
        var result = await _service.GetByIdAsync(configId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(configId);
        _mockRepository.Verify(x => x.GetByIdAsync(configId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonexistentId_ShouldReturnNull()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act
        var result = await _service.GetByIdAsync(configId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetByIdAsync(configId));
    }

    #endregion

    #region GetPrimaryConfigAsync Tests

    [Fact]
    public async Task GetPrimaryConfigAsync_WithValidMerchantIdAndProviderId_ShouldReturnPrimaryConfig()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.IsPrimary = true;

        _mockRepository.Setup(x => x.GetPrimaryConfigAsync(Guid.Parse(merchantId), providerId))
            .ReturnsAsync(config);

        // Act
        var result = await _service.GetPrimaryConfigAsync(merchantId, providerId);

        // Assert
        result.Should().NotBeNull();
        result!.IsPrimary.Should().BeTrue();
        _mockRepository.Verify(x => x.GetPrimaryConfigAsync(Guid.Parse(merchantId), providerId), Times.Once);
    }

    [Fact]
    public async Task GetPrimaryConfigAsync_WithInvalidMerchantIdFormat_ShouldReturnNull()
    {
        // Arrange
        var invalidMerchantId = "invalid-guid";
        var providerId = Guid.NewGuid();

        // Act
        var result = await _service.GetPrimaryConfigAsync(invalidMerchantId, providerId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.GetPrimaryConfigAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetPrimaryConfigAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetPrimaryConfigAsync(Guid.Parse(merchantId), providerId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetPrimaryConfigAsync(merchantId, providerId));
    }

    #endregion

    #region GetByMerchantIdAsync Tests

    [Fact]
    public async Task GetByMerchantIdAsync_WithValidMerchantId_ShouldReturnConfigs()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var configs = new List<SurchargeProviderConfig>
        {
            TestDataBuilder.CreateValidProviderConfig(),
            TestDataBuilder.CreateValidProviderConfig()
        };

        _mockRepository.Setup(x => x.GetByMerchantIdAsync(Guid.Parse(merchantId)))
            .ReturnsAsync(configs);

        // Act
        var result = await _service.GetByMerchantIdAsync(merchantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(x => x.GetByMerchantIdAsync(Guid.Parse(merchantId)), Times.Once);
    }

    [Fact]
    public async Task GetByMerchantIdAsync_WithInvalidMerchantIdFormat_ShouldReturnEmptyCollection()
    {
        // Arrange
        var invalidMerchantId = "invalid-guid";

        // Act
        var result = await _service.GetByMerchantIdAsync(invalidMerchantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(x => x.GetByMerchantIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region GetByProviderIdAsync Tests

    [Fact]
    public async Task GetByProviderIdAsync_WithValidProviderId_ShouldReturnConfigs()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var configs = new List<SurchargeProviderConfig>
        {
            TestDataBuilder.CreateValidProviderConfig(),
            TestDataBuilder.CreateValidProviderConfig()
        };

        _mockRepository.Setup(x => x.GetByProviderIdAsync(providerId))
            .ReturnsAsync(configs);

        // Act
        var result = await _service.GetByProviderIdAsync(providerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(x => x.GetByProviderIdAsync(providerId), Times.Once);
    }

    [Fact]
    public async Task GetByProviderIdAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var providerId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetByProviderIdAsync(providerId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetByProviderIdAsync(providerId));
    }

    #endregion

    #region GetActiveConfigsAsync Tests

    [Fact]
    public async Task GetActiveConfigsAsync_WithValidMerchantId_ShouldReturnActiveConfigs()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var activeConfigs = new List<SurchargeProviderConfig>
        {
            TestDataBuilder.CreateValidProviderConfig(),
            TestDataBuilder.CreateValidProviderConfig()
        };

        _mockRepository.Setup(x => x.GetActiveConfigsAsync(Guid.Parse(merchantId)))
            .ReturnsAsync(activeConfigs);

        // Act
        var result = await _service.GetActiveConfigsAsync(merchantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(x => x.GetActiveConfigsAsync(Guid.Parse(merchantId)), Times.Once);
    }

    [Fact]
    public async Task GetActiveConfigsAsync_WithInvalidMerchantIdFormat_ShouldReturnEmptyCollection()
    {
        // Arrange
        var invalidMerchantId = "invalid-guid";

        // Act
        var result = await _service.GetActiveConfigsAsync(invalidMerchantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(x => x.GetActiveConfigsAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithNewPrimaryConfig_ShouldDemoteExistingPrimaryAndCreateNew()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.MerchantId = Guid.NewGuid();
        config.IsPrimary = true;
        var requestor = "test-user";

        var existingPrimary = TestDataBuilder.CreateValidProviderConfig();
        existingPrimary.IsPrimary = true;

        _mockRepository.Setup(x => x.GetPrimaryConfigAsync(config.MerchantId!.Value, config.ProviderId))
            .ReturnsAsync(existingPrimary);
        _mockRepository.Setup(x => x.UpdateAsync(existingPrimary))
            .ReturnsAsync(existingPrimary);
        _mockRepository.Setup(x => x.GetByProviderIdAsync(config.ProviderId))
            .ReturnsAsync(new List<SurchargeProviderConfig>());
        _mockRepository.Setup(x => x.CreateAsync(config))
            .ReturnsAsync(config);

        // Act
        var result = await _service.CreateAsync(config, requestor);

        // Assert
        result.Should().NotBeNull();
        result.IsPrimary.Should().BeTrue();
        
        _mockRepository.Verify(x => x.UpdateAsync(It.Is<SurchargeProviderConfig>(c => !c.IsPrimary)), Times.Once);
        _mockRepository.Verify(x => x.CreateAsync(config), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNonPrimaryConfigAndNoPrimary_ShouldDefaultToPrimary()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.MerchantId = Guid.NewGuid();
        config.IsPrimary = false;
        var requestor = "test-user";

        _mockRepository.Setup(x => x.HasPrimaryConfigAsync(config.MerchantId!.Value, config.ProviderId))
            .ReturnsAsync(false);
        _mockRepository.Setup(x => x.GetByProviderIdAsync(config.ProviderId))
            .ReturnsAsync(new List<SurchargeProviderConfig>());
        _mockRepository.Setup(x => x.CreateAsync(config))
            .ReturnsAsync(config);

        // Act
        var result = await _service.CreateAsync(config, requestor);

        // Assert
        result.Should().NotBeNull();
        result.IsPrimary.Should().BeTrue(); // Should be defaulted to primary
        _mockRepository.Verify(x => x.CreateAsync(config), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldArchiveExistingActiveConfigs()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.MerchantId = Guid.NewGuid();
        var requestor = "test-user";

        var existingActiveConfig = TestDataBuilder.CreateValidProviderConfig();
        existingActiveConfig.MerchantId = config.MerchantId;
        existingActiveConfig.IsActive = true;

        var existingConfigs = new List<SurchargeProviderConfig> { existingActiveConfig };

        _mockRepository.Setup(x => x.GetByProviderIdAsync(config.ProviderId))
            .ReturnsAsync(existingConfigs);
        _mockRepository.Setup(x => x.UpdateAsync(existingActiveConfig))
            .ReturnsAsync(existingActiveConfig);
        _mockRepository.Setup(x => x.HasPrimaryConfigAsync(config.MerchantId!.Value, config.ProviderId))
            .ReturnsAsync(true);
        _mockRepository.Setup(x => x.CreateAsync(config))
            .ReturnsAsync(config);

        // Act
        var result = await _service.CreateAsync(config, requestor);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.UpdateAsync(It.Is<SurchargeProviderConfig>(c => !c.IsActive)), Times.Once);
        _mockRepository.Verify(x => x.CreateAsync(config), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.MerchantId = Guid.NewGuid();
        var requestor = "test-user";

        _mockRepository.Setup(x => x.CreateAsync(config))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreateAsync(config, requestor));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidConfig_ShouldUpdateConfig()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.MerchantId = Guid.NewGuid();
        var requestor = "test-user";

        var existingConfig = TestDataBuilder.CreateValidProviderConfig();
        existingConfig.Id = config.Id;
        existingConfig.IsPrimary = false;

        _mockRepository.Setup(x => x.GetByIdAsync(config.Id))
            .ReturnsAsync(existingConfig);
        _mockRepository.Setup(x => x.UpdateAsync(config))
            .ReturnsAsync(config);

        // Act
        var result = await _service.UpdateAsync(config, requestor);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.UpdateAsync(config), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentConfig_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        var requestor = "test-user";

        _mockRepository.Setup(x => x.GetByIdAsync(config.Id))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.UpdateAsync(config, requestor));

        exception.Message.Should().Contain($"Config with ID {config.Id} not found");
    }

    [Fact]
    public async Task UpdateAsync_WhenPromotingToPrimary_ShouldDemoteExistingPrimary()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.MerchantId = Guid.NewGuid();
        config.IsPrimary = true;
        var requestor = "test-user";

        var existingConfig = TestDataBuilder.CreateValidProviderConfig();
        existingConfig.Id = config.Id;
        existingConfig.IsPrimary = false;

        var existingPrimary = TestDataBuilder.CreateValidProviderConfig();
        existingPrimary.IsPrimary = true;

        _mockRepository.Setup(x => x.GetByIdAsync(config.Id))
            .ReturnsAsync(existingConfig);
        _mockRepository.Setup(x => x.GetPrimaryConfigAsync(config.MerchantId!.Value, config.ProviderId))
            .ReturnsAsync(existingPrimary);
        _mockRepository.Setup(x => x.UpdateAsync(existingPrimary))
            .ReturnsAsync(existingPrimary);
        _mockRepository.Setup(x => x.UpdateAsync(config))
            .ReturnsAsync(config);

        // Act
        var result = await _service.UpdateAsync(config, requestor);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.UpdateAsync(It.Is<SurchargeProviderConfig>(c => 
            c.Id == existingPrimary.Id && !c.IsPrimary)), Times.Once);
        _mockRepository.Verify(x => x.UpdateAsync(config), Times.Once);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithNonPrimaryConfig_ShouldDeleteConfig()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig(configId);
        config.IsPrimary = false;

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(config);
        _mockRepository.Setup(x => x.DeleteAsync(configId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(configId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.DeleteAsync(configId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithPrimaryConfig_ShouldPromoteNextPrimaryAndDelete()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var primaryConfig = TestDataBuilder.CreateValidProviderConfig(configId);
        primaryConfig.IsPrimary = true;
        primaryConfig.MerchantId = Guid.NewGuid();

        var nextConfig = TestDataBuilder.CreateValidProviderConfig();
        nextConfig.MerchantId = primaryConfig.MerchantId;
        nextConfig.ProviderId = primaryConfig.ProviderId;
        nextConfig.IsActive = true;

        var configs = new List<SurchargeProviderConfig> { nextConfig };

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(primaryConfig);
        _mockRepository.Setup(x => x.GetByMerchantIdAsync(primaryConfig.MerchantId!.Value))
            .ReturnsAsync(configs);
        _mockRepository.Setup(x => x.UpdateAsync(nextConfig))
            .ReturnsAsync(nextConfig);
        _mockRepository.Setup(x => x.DeleteAsync(configId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(configId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(x => x.UpdateAsync(It.Is<SurchargeProviderConfig>(c => 
            c.Id == nextConfig.Id && c.IsPrimary)), Times.Once);
        _mockRepository.Verify(x => x.DeleteAsync(configId), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonexistentConfig_ShouldReturnFalse()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act
        var result = await _service.DeleteAsync(configId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_WithExistingConfig_ShouldReturnTrue()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.ExistsAsync(configId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ExistsAsync(configId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonexistentConfig_ShouldReturnFalse()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.ExistsAsync(configId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ExistsAsync(configId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region HasActiveConfigAsync Tests

    [Fact]
    public async Task HasActiveConfigAsync_WithValidMerchantId_ShouldReturnRepositoryResult()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid();

        _mockRepository.Setup(x => x.HasActiveConfigAsync(Guid.Parse(merchantId), providerId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasActiveConfigAsync(merchantId, providerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasActiveConfigAsync_WithInvalidMerchantIdFormat_ShouldReturnFalse()
    {
        // Arrange
        var invalidMerchantId = "invalid-guid";
        var providerId = Guid.NewGuid();

        // Act
        var result = await _service.HasActiveConfigAsync(invalidMerchantId, providerId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.HasActiveConfigAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region HasPrimaryConfigAsync Tests

    [Fact]
    public async Task HasPrimaryConfigAsync_WithValidMerchantId_ShouldReturnRepositoryResult()
    {
        // Arrange
        var merchantId = Guid.NewGuid().ToString();
        var providerId = Guid.NewGuid();

        _mockRepository.Setup(x => x.HasPrimaryConfigAsync(Guid.Parse(merchantId), providerId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasPrimaryConfigAsync(merchantId, providerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPrimaryConfigAsync_WithInvalidMerchantIdFormat_ShouldReturnFalse()
    {
        // Arrange
        var invalidMerchantId = "invalid-guid";
        var providerId = Guid.NewGuid();

        // Act
        var result = await _service.HasPrimaryConfigAsync(invalidMerchantId, providerId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(x => x.HasPrimaryConfigAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    #endregion

    #region UpdateLastUsedAsync Tests

    [Fact]
    public async Task UpdateLastUsedAsync_WithValidParameters_ShouldCallRepository()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var success = true;
        var errorMessage = "test error";
        var responseTime = 123.45;

        _mockRepository.Setup(x => x.UpdateLastUsedAsync(configId, success, errorMessage, responseTime))
            .Returns(Task.CompletedTask);

        // Act
        await _service.UpdateLastUsedAsync(configId, success, errorMessage, responseTime);

        // Assert
        _mockRepository.Verify(x => x.UpdateLastUsedAsync(configId, success, errorMessage, responseTime), Times.Once);
    }

    [Fact]
    public async Task UpdateLastUsedAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.UpdateLastUsedAsync(configId, true, null, null))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.UpdateLastUsedAsync(configId, true));
    }

    #endregion

    #region ValidateCredentialsAsync Tests

    [Fact]
    public async Task ValidateCredentialsAsync_WithValidConfig_ShouldReturnTrue()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig(configId);
        var credentials = JsonDocument.Parse("{\"username\":\"test\",\"password\":\"pass\"}");

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(config);

        // Act
        var result = await _service.ValidateCredentialsAsync(configId, credentials);

        // Assert
        result.Should().BeTrue(); // Current implementation returns true
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithNonexistentConfig_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var credentials = JsonDocument.Parse("{\"username\":\"test\"}");

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.ValidateCredentialsAsync(configId, credentials));

        exception.Message.Should().Contain($"Config with ID {configId} not found");
    }

    #endregion

    #region ValidateRateLimitAsync Tests

    [Fact]
    public async Task ValidateRateLimitAsync_WithNoRateLimit_ShouldReturnTrue()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig(configId);
        config.RateLimit = null;
        config.RateLimitPeriod = null;

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(config);

        // Act
        var result = await _service.ValidateRateLimitAsync(configId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRateLimitAsync_WithRateLimit_ShouldReturnTrue()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig(configId);
        config.RateLimit = 100;
        config.RateLimitPeriod = 60;

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(config);

        // Act
        var result = await _service.ValidateRateLimitAsync(configId);

        // Assert
        result.Should().BeTrue(); // Current implementation always returns true
    }

    [Fact]
    public async Task ValidateRateLimitAsync_WithNonexistentConfig_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.ValidateRateLimitAsync(configId));

        exception.Message.Should().Contain($"Config with ID {configId} not found");
    }

    #endregion

    #region ValidateTimeoutAsync Tests

    [Fact]
    public async Task ValidateTimeoutAsync_WithNoTimeout_ShouldReturnTrue()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig(configId);
        config.Timeout = null;

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(config);

        // Act
        var result = await _service.ValidateTimeoutAsync(configId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateTimeoutAsync_WithTimeout_ShouldReturnTrue()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = TestDataBuilder.CreateValidProviderConfig(configId);
        config.Timeout = 30;

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync(config);

        // Act
        var result = await _service.ValidateTimeoutAsync(configId);

        // Assert
        result.Should().BeTrue(); // Current implementation always returns true
    }

    [Fact]
    public async Task ValidateTimeoutAsync_WithNonexistentConfig_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var configId = Guid.NewGuid();

        _mockRepository.Setup(x => x.GetByIdAsync(configId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.ValidateTimeoutAsync(configId));

        exception.Message.Should().Contain($"Config with ID {configId} not found");
    }

    #endregion
}