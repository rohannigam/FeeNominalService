using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Http;
using Moq;
using FluentAssertions;
using FeeNominalService.Services;
using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Repositories;
using FeeNominalService.Models;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Services.Adapters;
using FeeNominalService.Services.Adapters.InterPayments;
using FeeNominalService.Settings;
using System.Text.Json;

namespace FeeNominalService.Tests.Services;

// Test adapter that inherits from InterPaymentsAdapter for testing
public class TestInterPaymentsAdapter : InterPaymentsAdapter
{
    private readonly (bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage) _mockResponse;

    public TestInterPaymentsAdapter(
        IHttpClientFactory httpClientFactory, 
        ILogger<InterPaymentsAdapter> logger,
        (bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage) mockResponse) 
        : base(httpClientFactory, logger)
    {
        _mockResponse = mockResponse;
    }

    public override async Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessRefundAsync(
        string sTxId,
        SurchargeProviderConfig providerConfig,
        decimal amount,
        string? mTxId = null,
        string? cardToken = null,
        List<string>? data = null)
    {
        return await Task.FromResult(_mockResponse);
    }
}

public class SurchargeTransactionServiceTests
{
    private readonly Mock<ILogger<SurchargeTransactionService>> _mockLogger;
    private readonly Mock<ISurchargeTransactionRepository> _mockTransactionRepository;
    private readonly Mock<ISurchargeProviderConfigRepository> _mockProviderConfigRepository;
    private readonly Mock<ISurchargeProviderAdapterFactory> _mockProviderAdapterFactory;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ISurchargeProviderRepository> _mockProviderRepository;
    private readonly SurchargeTransactionService _service;
    private readonly Guid _testMerchantId = Guid.NewGuid();

    public SurchargeTransactionServiceTests()
    {
        _mockLogger = new Mock<ILogger<SurchargeTransactionService>>();
        _mockTransactionRepository = new Mock<ISurchargeTransactionRepository>();
        _mockProviderConfigRepository = new Mock<ISurchargeProviderConfigRepository>();
        _mockProviderAdapterFactory = new Mock<ISurchargeProviderAdapterFactory>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockProviderRepository = new Mock<ISurchargeProviderRepository>();

        var bulkSaleSettings = new BulkSaleSettings { MaxConcurrency = 10 };
        var options = Options.Create(bulkSaleSettings);

        _service = new SurchargeTransactionService(
            _mockTransactionRepository.Object,
            _mockProviderConfigRepository.Object,
            _mockLogger.Object,
            _mockHttpClientFactory.Object,
            _mockProviderAdapterFactory.Object,
            options,
            _mockProviderRepository.Object);
    }

    [Fact]
    public async Task ProcessRefundAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var refundRequest = new SurchargeRefundRequest
        {
            SurchargeTransactionId = Guid.NewGuid(),
            Amount = 100.00m
        };

        var originalTransaction = new SurchargeTransaction
        {
            Id = refundRequest.SurchargeTransactionId.Value,
            Amount = 200.00m,
            CorrelationId = "test-correlation-id",
            ProviderConfigId = Guid.NewGuid(),
            ProviderTransactionId = "test-provider-tx-id",
            MerchantTransactionId = "test-merchant-tx-id",
            MerchantId = _testMerchantId,
            RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        var providerConfig = new SurchargeProviderConfig
        {
            Id = Guid.NewGuid(),
            ConfigName = "Test Config",
            Credentials = JsonSerializer.SerializeToDocument(new { jwt_token = "test-token", token_type = "Bearer" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            IsPrimary = true
        };

        var successfulResponse = new
        {
            refundId = Guid.NewGuid().ToString(),
            refund = 5.00m,
            previouslyRefundedTransactionFees = 0.00m,
            originalTransactionFee = 10.00m,
            message = "Refund processed successfully"
        };

        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(refundRequest.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        // Create a test adapter that can be cast to InterPaymentsAdapter
        var mockLogger = new Mock<ILogger<InterPaymentsAdapter>>();
        var testAdapter = new TestInterPaymentsAdapter(
            _mockHttpClientFactory.Object, 
            mockLogger.Object,
            (true, JsonDocument.Parse(JsonSerializer.Serialize(successfulResponse)), (string?)null));

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(testAdapter);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = "test-correlation-id",
                RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" }),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Mock the FindOriginalSaleTransactionAsync method
        _mockTransactionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(originalTransaction);

        // Mock the GetRootSurchargeTransactionId method
        _mockTransactionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(originalTransaction);

        // Act
        var result = await _service.ProcessRefundAsync(refundRequest, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.OriginalSurchargeTransactionId.Should().Be(refundRequest.SurchargeTransactionId.Value);
        result.RefundAmount.Should().Be(refundRequest.Amount);
        result.OriginalAmount.Should().Be(originalTransaction.Amount);
        result.CorrelationId.Should().Be(originalTransaction.CorrelationId);
        result.OriginalProviderTransactionId.Should().Be(originalTransaction.ProviderTransactionId);
    }

    [Fact]
    public async Task ProcessRefundAsync_WithNonExistentTransaction_ReturnsErrorResponse()
    {
        // Arrange
        var refundRequest = new SurchargeRefundRequest
        {
            SurchargeTransactionId = Guid.NewGuid(),
            Amount = 100.00m
        };

        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(refundRequest.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync((SurchargeTransaction?)null);

        // Act
        var result = await _service.ProcessRefundAsync(refundRequest, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().Be(Guid.Empty);
        result.Status.Should().Be("NotFound");
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("TRANSACTION_NOT_FOUND");
        result.Error.Message.Should().Be("Transaction not found.");
    }

    [Fact]
    public async Task ProcessRefundAsync_WithNonExistentProviderConfig_ReturnsErrorResponse()
    {
        // Arrange
        var refundRequest = new SurchargeRefundRequest
        {
            SurchargeTransactionId = Guid.NewGuid(),
            Amount = 100.00m
        };

        var originalTransaction = new SurchargeTransaction
        {
            Id = refundRequest.SurchargeTransactionId.Value,
            Amount = 200.00m,
            CorrelationId = "test-correlation-id",
            ProviderConfigId = Guid.NewGuid(),
            MerchantId = _testMerchantId,
            RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" })
        };

        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(refundRequest.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act
        var result = await _service.ProcessRefundAsync(refundRequest, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().Be(Guid.Empty);
        result.Status.Should().Be("ConfigNotFound");
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("PROVIDER_CONFIG_NOT_FOUND");
        result.Error.Message.Should().Be("Provider config not found.");
    }

    [Fact]
    public async Task ProcessRefundAsync_WithProviderError_ReturnsErrorResponse()
    {
        // Arrange
        var refundRequest = new SurchargeRefundRequest
        {
            SurchargeTransactionId = Guid.NewGuid(),
            Amount = 100.00m
        };

        var originalTransaction = new SurchargeTransaction
        {
            Id = refundRequest.SurchargeTransactionId.Value,
            Amount = 200.00m,
            CorrelationId = "test-correlation-id",
            ProviderConfigId = Guid.NewGuid(),
            MerchantId = _testMerchantId,
            RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" })
        };

        var providerConfig = new SurchargeProviderConfig
        {
            Id = Guid.NewGuid(),
            ConfigName = "Test Config",
            Credentials = JsonSerializer.SerializeToDocument(new { jwt_token = "test-token", token_type = "Bearer" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            IsPrimary = true
        };

        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(refundRequest.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        // Create a test adapter that returns an error with a response payload containing the error message
        var mockLogger = new Mock<ILogger<InterPaymentsAdapter>>();
        var errorResponse = new
        {
            message = "Refund amount exceeds original transaction amount",
            code = "REFUND_ERROR"
        };

        var testAdapter = new TestInterPaymentsAdapter(
            _mockHttpClientFactory.Object, 
            mockLogger.Object,
            (false, JsonDocument.Parse(JsonSerializer.Serialize(errorResponse)), "Refund amount exceeds original transaction amount"));

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(testAdapter);

        // Act
        var result = await _service.ProcessRefundAsync(refundRequest, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("INTERPAYMENTS_MESSAGE");
        result.Error.Message.Should().Be("Refund amount exceeds original transaction amount");
    }

    [Fact]
    public async Task ProcessRefundAsync_WithProviderWarning_ReturnsWarningInError()
    {
        // Arrange
        var refundRequest = new SurchargeRefundRequest
        {
            SurchargeTransactionId = Guid.NewGuid(),
            Amount = 100.00m
        };

        var originalTransaction = new SurchargeTransaction
        {
            Id = refundRequest.SurchargeTransactionId.Value,
            Amount = 200.00m,
            CorrelationId = "test-correlation-id",
            ProviderConfigId = Guid.NewGuid(),
            MerchantId = _testMerchantId,
            RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" })
        };

        var providerConfig = new SurchargeProviderConfig
        {
            Id = Guid.NewGuid(),
            ConfigName = "Test Config",
            Credentials = JsonSerializer.SerializeToDocument(new { jwt_token = "test-token", token_type = "Bearer" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            IsPrimary = true
        };

        // Create a test adapter that returns a warning
        var mockLogger = new Mock<ILogger<InterPaymentsAdapter>>();
        var warningResponse = new
        {
            refundId = Guid.NewGuid().ToString(),
            refund = 0.00m,
            previouslyRefundedTransactionFees = 10.00m,
            originalTransactionFee = 10.00m,
            message = "Asking for too much refund"
        };

        var testAdapter = new TestInterPaymentsAdapter(
            _mockHttpClientFactory.Object, 
            mockLogger.Object,
            (true, JsonDocument.Parse(JsonSerializer.Serialize(warningResponse)), (string?)null));

        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(refundRequest.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(testAdapter);

        // Act
        var result = await _service.ProcessRefundAsync(refundRequest, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("INTERPAYMENTS_MESSAGE");
        result.Error.Message.Should().Be("Asking for too much refund");
    }
} 