using System;
using System.Collections.Generic;
using System.Net.Http;
using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Services.Adapters.InterPayments;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace FeeNominalService.Tests.Services;

public class InterPaymentsAdapterTests : IDisposable
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<InterPaymentsAdapter>> _mockLogger;
    private readonly InterPaymentsAdapter _adapter;

    public InterPaymentsAdapterTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<InterPaymentsAdapter>>();

        _adapter = new InterPaymentsAdapter(_mockHttpClientFactory.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
    }

    #region ValidateRequest Tests

    [Fact]
    public void ValidateRequest_WithValidUSRequest_ShouldReturnValid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "USA",
            PostalCode = "12345",
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateRequest_WithValidUSRequestZipPlusFour_ShouldReturnValid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "US",
            PostalCode = "12345-6789",
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateRequest_WithValidCanadianRequest_ShouldReturnValid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "CANADA",
            PostalCode = "K1A 0A6",
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateRequest_WithValidCanadianRequestNoSpace_ShouldReturnValid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "CAN",
            PostalCode = "K1A0A6",
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateRequest_WithInvalidUSPostalCode_ShouldReturnInvalid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "USA",
            PostalCode = "1234", // Too short
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("postal code");
    }

    [Fact]
    public void ValidateRequest_WithInvalidCanadianPostalCode_ShouldReturnInvalid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "CANADA",
            PostalCode = "123456", // Wrong format
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("postal code");
    }

    [Fact]
    public void ValidateRequest_WithMissingCountry_ShouldReturnInvalid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = null!,
            PostalCode = "12345",
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void ValidateRequest_WithEmptyCountry_ShouldReturnInvalid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "",
            PostalCode = "12345",
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNull();
    }

    [Fact]
    public void ValidateRequest_WithOtherCountry_ShouldReturnValid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "GBR",
            PostalCode = "SW1A 1AA", // UK postal code format
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateRequest_WithNumericCountryCode_ShouldReturnValid()
    {
        // Arrange
        var request = new SurchargeAuthRequest
        {
            Country = "840", // USA
            PostalCode = "12345",
            CorrelationId = "test-correlation",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            ProviderCode = "test-provider",
            Amount = 100.00m
        };

        // Act
        var result = _adapter.ValidateRequest(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    #endregion
}