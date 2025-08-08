using System;
using System.Net.Http;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Services;
using FeeNominalService.Services.Adapters.InterPayments;
using FeeNominalService.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace FeeNominalService.Tests.Services;

public class SurchargeProviderAdapterFactoryTests : IDisposable
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<InterPaymentsAdapter>> _mockLogger;
    private readonly InterPaymentsAdapter _interPaymentsAdapter;
    private readonly SurchargeProviderAdapterFactory _factory;

    public SurchargeProviderAdapterFactoryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<InterPaymentsAdapter>>();
        
        // Create a real instance of InterPaymentsAdapter with mocked dependencies
        _interPaymentsAdapter = new InterPaymentsAdapter(_mockHttpClientFactory.Object, _mockLogger.Object);
        
        _factory = new SurchargeProviderAdapterFactory(_mockServiceProvider.Object, _interPaymentsAdapter);
    }

    public void Dispose()
    {
    }

    #region GetAdapter Tests

    [Fact]
    public void GetAdapter_WithInterPaymentsProviderType_ShouldReturnInterPaymentsAdapter()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "INTERPAYMENTS";
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act
        var result = _factory.GetAdapter(config);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_interPaymentsAdapter);
    }

    [Fact]
    public void GetAdapter_WithInterPaymentsTestProviderType_ShouldReturnInterPaymentsAdapter()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "INTERPAYMENTS_TEST_001";
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act
        var result = _factory.GetAdapter(config);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_interPaymentsAdapter);
    }

    [Fact]
    public void GetAdapter_WithInterPaymentsProdProviderType_ShouldReturnInterPaymentsAdapter()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "INTERPAYMENTS_PROD_001";
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act
        var result = _factory.GetAdapter(config);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_interPaymentsAdapter);
    }

    [Fact]
    public void GetAdapter_WithCaseInsensitiveProviderType_ShouldReturnInterPaymentsAdapter()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "interpayments"; // lowercase
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act
        var result = _factory.GetAdapter(config);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_interPaymentsAdapter);
    }

    [Fact]
    public void GetAdapter_WithMixedCaseProviderType_ShouldReturnInterPaymentsAdapter()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "InterPayments"; // mixed case
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act
        var result = _factory.GetAdapter(config);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_interPaymentsAdapter);
    }

    [Fact]
    public void GetAdapter_WithUnsupportedProviderType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "UNSUPPORTED_PROVIDER";
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetAdapter(config));
        
        exception.Message.Should().Contain("No adapter registered for provider type: UNSUPPORTED_PROVIDER");
    }

    [Fact]
    public void GetAdapter_WithNullProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _factory.GetAdapter(config));
        
        exception.ParamName.Should().Be("Provider");
        exception.Message.Should().Contain("Provider cannot be null in provider config");
    }

    [Fact]
    public void GetAdapter_WithNullProviderType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = null!;
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetAdapter(config));
        
        exception.Message.Should().Contain("No adapter registered for provider type:");
    }

    [Fact]
    public void GetAdapter_WithEmptyProviderType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "";
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetAdapter(config));
        
        exception.Message.Should().Contain("No adapter registered for provider type:");
    }

    [Fact]
    public void GetAdapter_WithWhitespaceProviderType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "   ";
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(() => _factory.GetAdapter(config));
        
        exception.Message.Should().Contain("No adapter registered for provider type:    ");
    }

    #endregion

    #region Factory Construction Tests

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateFactory()
    {
        // Arrange & Act
        var factory = new SurchargeProviderAdapterFactory(_mockServiceProvider.Object, _interPaymentsAdapter);

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldStillCreateFactory()
    {
        // Arrange & Act
        var factory = new SurchargeProviderAdapterFactory(null!, _interPaymentsAdapter);

        // Assert - Constructor should still work since _serviceProvider is stored but not used in current implementation
        factory.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullInterPaymentsAdapter_ShouldCreateFactoryButFailOnAccess()
    {
        // Arrange & Act
        var factory = new SurchargeProviderAdapterFactory(_mockServiceProvider.Object, null!);

        // Assert - Constructor should work, but GetAdapter should fail
        factory.Should().NotBeNull();
        
        // Test that accessing InterPayments adapter fails
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.ProviderType = "INTERPAYMENTS";
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config.Provider = provider;

        var result = factory.GetAdapter(config);
        result.Should().BeNull(); // The dictionary will contain null
    }

    #endregion

    #region Integration Tests with Real Config Objects

    [Fact]
    public void GetAdapter_WithComplexProviderConfig_ShouldReturnCorrectAdapter()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var provider = TestDataBuilder.CreateValidSurchargeProvider(providerId);
        provider.ProviderType = "INTERPAYMENTS";
        provider.Code = "INTERPAYMENTS_PROD";
        provider.Name = "InterPayments Production";
        
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, providerId);
        config.Provider = provider;
        config.ConfigName = "Production Configuration";
        config.IsPrimary = true;
        config.IsActive = true;

        // Act
        var result = _factory.GetAdapter(config);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(_interPaymentsAdapter);
    }

    [Fact]
    public void GetAdapter_WithMultipleCallsForSameProviderType_ShouldReturnSameInstance()
    {
        // Arrange
        var provider1 = TestDataBuilder.CreateValidSurchargeProvider();
        provider1.ProviderType = "INTERPAYMENTS";
        var config1 = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config1.Provider = provider1;

        var provider2 = TestDataBuilder.CreateValidSurchargeProvider();
        provider2.ProviderType = "INTERPAYMENTS";
        var config2 = TestDataBuilder.CreateValidProviderConfigWithMerchant();
        config2.Provider = provider2;

        // Act
        var result1 = _factory.GetAdapter(config1);
        var result2 = _factory.GetAdapter(config2);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        result1.Should().BeSameAs(result2); // Should return the same instance
        result1.Should().BeSameAs(_interPaymentsAdapter);
    }

    [Fact]
    public void GetAdapter_WithDifferentInterPaymentsVariants_ShouldReturnSameAdapter()
    {
        // Arrange
        var providerTypes = new[] { "INTERPAYMENTS", "INTERPAYMENTS_TEST_001", "INTERPAYMENTS_PROD_001" };
        var results = new ISurchargeProviderAdapter[providerTypes.Length];

        // Act
        for (int i = 0; i < providerTypes.Length; i++)
        {
            var provider = TestDataBuilder.CreateValidSurchargeProvider();
            provider.ProviderType = providerTypes[i];
            var config = TestDataBuilder.CreateValidProviderConfigWithMerchant();
            config.Provider = provider;
            
            results[i] = _factory.GetAdapter(config);
        }

        // Assert
        foreach (var result in results)
        {
            result.Should().NotBeNull();
            result.Should().BeSameAs(_interPaymentsAdapter);
        }

        // All results should be the same instance
        for (int i = 1; i < results.Length; i++)
        {
            results[i].Should().BeSameAs(results[0]);
        }
    }

    #endregion
}