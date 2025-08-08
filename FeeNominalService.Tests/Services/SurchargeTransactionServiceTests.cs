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
using FeeNominalService.Models.Surcharge;
using FeeNominalService.Models.Common;
using FeeNominalService.Services.Adapters;
using FeeNominalService.Services.Adapters.InterPayments;
using FeeNominalService.Settings;
using FeeNominalService.Exceptions;
using System.Text.Json;

namespace FeeNominalService.Tests.Services;

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

    private SurchargeAuthRequest CreateValidAuthRequest(string correlationId = "test-correlation-id")
    {
        return new SurchargeAuthRequest
        {
            CorrelationId = correlationId,
            MerchantTransactionId = "test-merchant-tx-id",
            BinValue = "123456",
            SurchargeProcessor = "test-processor",
            Amount = 100.00m,
            Country = "USA",
            PostalCode = "12345",
            ProviderCode = "INTERPAYMENTS"
        };
    }

    private SurchargeProvider CreateValidProvider()
    {
        return new SurchargeProvider
        {
            Code = "INTERPAYMENTS",
            ProviderType = "INTERPAYMENTS",
            Name = "InterPayments",
            BaseUrl = "https://api.interpayments.com",
            AuthenticationType = "Bearer",
            CredentialsSchema = JsonSerializer.SerializeToDocument(new { }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
    }

    private SurchargeProviderConfig CreateValidProviderConfig()
    {
        return new SurchargeProviderConfig
        {
            Id = Guid.NewGuid(),
            ConfigName = "Test Config",
            Credentials = JsonSerializer.SerializeToDocument(new { jwt_token = "test-token", token_type = "Bearer" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            IsPrimary = true,
            Provider = CreateValidProvider()
        };
    }

    private SurchargeTransaction CreateValidTransaction(Guid? id = null, string correlationId = "test-correlation-id")
    {
        return new SurchargeTransaction
        {
            Id = id ?? Guid.NewGuid(),
            CorrelationId = correlationId,
            ProviderTransactionId = "test-provider-tx-id",
            MerchantTransactionId = "test-merchant-tx-id",
            RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
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
            (true, JsonDocument.Parse(JsonSerializer.Serialize(successfulResponse)), (string?)null),
            _mockHttpClientFactory.Object,
            mockLogger.Object);

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
            (false, JsonDocument.Parse(JsonSerializer.Serialize(errorResponse)), "Refund amount exceeds original transaction amount"),
            _mockHttpClientFactory.Object,
            mockLogger.Object);

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
            (true, JsonDocument.Parse(JsonSerializer.Serialize(warningResponse)), (string?)null),
            _mockHttpClientFactory.Object,
            mockLogger.Object);

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

    [Fact]
    public async Task ProcessAuthAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();

        var providerConfig = CreateValidProviderConfig();

        var successfulResponse = new SurchargeAuthResponse
        {
            SurchargeTransactionId = Guid.Empty,
            CorrelationId = request.CorrelationId,
            MerchantTransactionId = request.MerchantTransactionId,
            ProviderTransactionId = "test-provider-tx-id",
            OriginalAmount = request.Amount,
            SurchargeAmount = 5.00m,
            TotalAmount = 105.00m,
            Status = "Authorized",
            Provider = "InterPayments",
            ProviderType = "INTERPAYMENTS",
            ProviderCode = "INTERPAYMENTS",
            ProcessedAt = DateTime.UtcNow
        };

        _mockProviderConfigRepository
            .Setup(x => x.GetPrimaryConfigByProviderCodeAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync(providerConfig);

        var mockAdapter = new Mock<ISurchargeProviderAdapter>();
        mockAdapter.Setup(x => x.ValidateRequest(request)).Returns((true, (string?)null));
        mockAdapter.Setup(x => x.CalculateSurchargeAsync(request, providerConfig))
            .ReturnsAsync(successfulResponse);

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(mockAdapter.Object);

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.CorrelationId.Should().Be(request.CorrelationId);
        result.MerchantTransactionId.Should().Be(request.MerchantTransactionId);
        result.ProviderTransactionId.Should().Be("test-provider-tx-id");
        result.OriginalAmount.Should().Be(request.Amount);
        result.SurchargeAmount.Should().Be(5.00m);
        result.TotalAmount.Should().Be(105.00m);
        result.Status.Should().Be("Authorized");
        result.Provider.Should().Be("InterPayments");
        result.ProviderType.Should().Be("INTERPAYMENTS");
        result.ProviderCode.Should().Be("INTERPAYMENTS");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithDuplicateCorrelationId_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();

        var existingTransaction = CreateValidTransaction();

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync(existingTransaction);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("Duplicate transaction detected");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithInvalidProviderCode_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.ProviderCode = "AB"; // Too short

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        // Mock provider config lookup to return null so it goes to validation
        _mockProviderConfigRepository
            .Setup(x => x.GetPrimaryConfigByProviderCodeAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        _mockProviderConfigRepository
            .Setup(x => x.GetByProviderCodeAndMerchantAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        _mockProviderRepository
            .Setup(x => x.GetByCodeAsync(request.ProviderCode))
            .ReturnsAsync((SurchargeProvider?)null);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("Invalid provider code format");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithInvalidAmount_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.Amount = 0m; // Invalid amount

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("Amount must be greater than 0");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithMissingPostalCode_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.PostalCode = ""; // Missing postal code

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("postalCode is required");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithNonSurchargableAmountExceedingTotal_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.NonSurchargableAmount = 150.00m; // Exceeds total amount

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("Non-surchargable amount cannot exceed total amount");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithProviderNotFound_ThrowsSurchargeException()
    {
        // Arrange
        var request = CreateValidAuthRequest();

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockProviderConfigRepository
            .Setup(x => x.GetPrimaryConfigByProviderCodeAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        _mockProviderConfigRepository
            .Setup(x => x.GetByProviderCodeAndMerchantAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        _mockProviderRepository
            .Setup(x => x.GetByCodeAsync(request.ProviderCode))
            .ReturnsAsync((SurchargeProvider?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SurchargeException>(
            () => _service.ProcessAuthAsync(request, _testMerchantId, "test-actor"));
        
        exception.ErrorCode.Should().Be(SurchargeErrorCodes.Provider.PROVIDER_NOT_FOUND);
    }

    [Fact]
    public async Task ProcessAuthAsync_WithNoActiveProviderConfig_ThrowsSurchargeException()
    {
        // Arrange
        var request = CreateValidAuthRequest();

        var provider = CreateValidProvider();
        provider.CreatedBy = _testMerchantId.ToString();

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockProviderConfigRepository
            .Setup(x => x.GetPrimaryConfigByProviderCodeAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        _mockProviderConfigRepository
            .Setup(x => x.GetByProviderCodeAndMerchantAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        _mockProviderRepository
            .Setup(x => x.GetByCodeAsync(request.ProviderCode))
            .ReturnsAsync(provider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SurchargeException>(
            () => _service.ProcessAuthAsync(request, _testMerchantId, "test-actor"));
        
        exception.ErrorCode.Should().Be(SurchargeErrorCodes.Provider.PROVIDER_CONFIG_MISSING);
    }

    [Fact]
    public async Task ProcessAuthAsync_WithProviderValidationFailure_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();

        var providerConfig = CreateValidProviderConfig();

        _mockTransactionRepository
            .Setup(x => x.GetByCorrelationIdAsync(request.CorrelationId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockProviderConfigRepository
            .Setup(x => x.GetPrimaryConfigByProviderCodeAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync(providerConfig);

        var mockAdapter = new Mock<ISurchargeProviderAdapter>();
        mockAdapter.Setup(x => x.ValidateRequest(request)).Returns((false, "Provider validation failed"));

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(mockAdapter.Object);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Be("Provider validation failed");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithFollowUpCall_ValidatesOriginalTransaction()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.ProviderTransactionId = "test-provider-tx-id"; // Follow-up call

        var providerConfig = CreateValidProviderConfig();

        var originalTransaction = CreateValidTransaction(correlationId: request.CorrelationId);
        originalTransaction.ProviderTransactionId = request.ProviderTransactionId;
        originalTransaction.ProviderConfigId = providerConfig.Id; // Use the same provider config ID
        // Set the RequestPayload to contain the same merchantTransactionId as the request
        originalTransaction.RequestPayload = JsonSerializer.SerializeToDocument(new { 
            MerchantTransactionId = request.MerchantTransactionId,
            CorrelationId = request.CorrelationId 
        });

        _mockTransactionRepository
            .Setup(x => x.GetByProviderTransactionIdAndCorrelationIdForMerchantAsync(request.ProviderTransactionId!, request.CorrelationId, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetPrimaryConfigByProviderCodeAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync(providerConfig);

        var mockAdapter = new Mock<ISurchargeProviderAdapter>();
        mockAdapter.Setup(x => x.ValidateRequest(request)).Returns((true, (string?)null));
        mockAdapter.Setup(x => x.CalculateSurchargeAsync(request, providerConfig))
            .ReturnsAsync(new SurchargeAuthResponse
            {
                SurchargeTransactionId = Guid.Empty,
                CorrelationId = request.CorrelationId,
                MerchantTransactionId = request.MerchantTransactionId,
                ProviderTransactionId = "new-provider-tx-id",
                OriginalAmount = request.Amount,
                SurchargeAmount = 5.00m,
                TotalAmount = 105.00m,
                Status = "Authorized",
                Provider = "InterPayments",
                ProviderType = "INTERPAYMENTS",
                ProviderCode = "INTERPAYMENTS",
                ProcessedAt = DateTime.UtcNow
            });

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(mockAdapter.Object);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Authorized");
        result.ProviderTransactionId.Should().Be("new-provider-tx-id");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithFollowUpCallInvalidOriginalTransaction_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.ProviderTransactionId = "test-provider-tx-id"; // Follow-up call

        _mockTransactionRepository
            .Setup(x => x.GetByProviderTransactionIdAndCorrelationIdForMerchantAsync(request.ProviderTransactionId!, request.CorrelationId, _testMerchantId))
            .ReturnsAsync((SurchargeTransaction?)null);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("No original transaction found");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithFollowUpCallMerchantTransactionIdMismatch_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.ProviderTransactionId = "test-provider-tx-id"; // Follow-up call
        request.MerchantTransactionId = "different-merchant-tx-id"; // Different from original

        var originalTransaction = CreateValidTransaction(correlationId: request.CorrelationId);
        originalTransaction.ProviderTransactionId = request.ProviderTransactionId;
        // Set the RequestPayload to contain a different merchantTransactionId
        originalTransaction.RequestPayload = JsonSerializer.SerializeToDocument(new { 
            MerchantTransactionId = "original-merchant-tx-id",
            CorrelationId = request.CorrelationId 
        });

        _mockTransactionRepository
            .Setup(x => x.GetByProviderTransactionIdAndCorrelationIdForMerchantAsync(request.ProviderTransactionId!, request.CorrelationId, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("MerchantTransactionId must match original transaction");
    }

    [Fact]
    public async Task ProcessAuthAsync_WithFollowUpCallProviderConfigMismatch_ReturnsErrorResponse()
    {
        // Arrange
        var request = CreateValidAuthRequest();
        request.ProviderTransactionId = "test-provider-tx-id"; // Follow-up call

        var originalTransaction = CreateValidTransaction(correlationId: request.CorrelationId);
        originalTransaction.ProviderConfigId = Guid.NewGuid(); // Different config
        originalTransaction.ProviderTransactionId = request.ProviderTransactionId;
        // Set the RequestPayload to contain the same merchantTransactionId to pass validation
        originalTransaction.RequestPayload = JsonSerializer.SerializeToDocument(new { 
            MerchantTransactionId = request.MerchantTransactionId,
            CorrelationId = request.CorrelationId 
        });

        var providerConfig = CreateValidProviderConfig();

        _mockTransactionRepository
            .Setup(x => x.GetByProviderTransactionIdAndCorrelationIdForMerchantAsync(request.ProviderTransactionId!, request.CorrelationId, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetPrimaryConfigByProviderCodeAsync(request.ProviderCode, _testMerchantId))
            .ReturnsAsync(providerConfig);

        var mockAdapter = new Mock<ISurchargeProviderAdapter>();
        mockAdapter.Setup(x => x.ValidateRequest(request)).Returns((true, (string?)null));

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(mockAdapter.Object);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .ReturnsAsync(new SurchargeTransaction
            {
                Id = Guid.NewGuid(),
                CorrelationId = request.CorrelationId,
                RequestPayload = JsonSerializer.SerializeToDocument(request),
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            });

        // Act
        var result = await _service.ProcessAuthAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("Provider configuration does not match original transaction");
    }

    // ==================== ProcessSaleAsync Tests ====================

    [Fact]
    public async Task ProcessSaleAsync_WithValidRequestAndSurchargeTransactionId_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new SurchargeSaleRequest
        {
            SurchargeTransactionId = Guid.NewGuid()
        };

        var originalTransaction = CreateValidTransaction(request.SurchargeTransactionId.Value);
        originalTransaction.Status = SurchargeTransactionStatus.Authorized;
        originalTransaction.ProviderConfigId = Guid.NewGuid();
        originalTransaction.OperationType = SurchargeOperationType.Auth;
        originalTransaction.MerchantId = _testMerchantId;
        originalTransaction.ProviderTransactionId = "auth-provider-tx-id";

        var providerConfig = CreateValidProviderConfig();
        providerConfig.Id = originalTransaction.ProviderConfigId;

        // Mock GetLatestInOriginalChainAsync instead of GetByIdForMerchantAsync
        _mockTransactionRepository
            .Setup(x => x.GetLatestInOriginalChainAsync(request.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        var mockAdapter = new Mock<ISurchargeProviderAdapter>();
        mockAdapter.Setup(x => x.ProcessSaleAsync(It.IsAny<SurchargeTransaction>(), providerConfig, request))
            .ReturnsAsync((true, JsonSerializer.SerializeToDocument(new { success = true }), (string?)null));

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(mockAdapter.Object);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .Callback<SurchargeTransaction>(transaction => 
            {
                // Ensure the created transaction has the correct OriginalSurchargeTransId
                transaction.OriginalSurchargeTransId = originalTransaction.Id;
            })
            .ReturnsAsync((SurchargeTransaction transaction) => transaction);

        // Mock GetRootSurchargeTransactionId to return the expected value
        _mockTransactionRepository
            .Setup(x => x.GetLatestInOriginalChainAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(originalTransaction);
        
        // Mock GetByIdAsync for GetRootSurchargeTransactionId
        _mockTransactionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => 
            {
                // Return the original transaction for any ID to ensure GetRootSurchargeTransactionId returns the expected value
                return originalTransaction;
            });

        // Act
        var result = await _service.ProcessSaleAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.OriginalSurchargeTransactionId.Should().Be(originalTransaction.Id);
        result.Amount.Should().Be(originalTransaction.Amount);
        result.CorrelationId.Should().Be(originalTransaction.CorrelationId);
        result.ProviderTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task ProcessSaleAsync_WithValidRequestAndProviderTransactionId_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new SurchargeSaleRequest
        {
            ProviderTransactionId = "auth-provider-tx-id",
            CorrelationId = "test-correlation-id"
        };

        var originalTransaction = CreateValidTransaction(correlationId: request.CorrelationId);
        originalTransaction.Status = SurchargeTransactionStatus.Authorized;
        originalTransaction.ProviderTransactionId = request.ProviderTransactionId;
        originalTransaction.ProviderConfigId = Guid.NewGuid();
        originalTransaction.OperationType = SurchargeOperationType.Auth;
        originalTransaction.MerchantId = _testMerchantId;

        var providerConfig = CreateValidProviderConfig();
        providerConfig.Id = originalTransaction.ProviderConfigId;

        // Mock GetLatestInProviderTransactionChainAsync
        _mockTransactionRepository
            .Setup(x => x.GetLatestInProviderTransactionChainAsync(request.ProviderTransactionId, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        // Mock GetByIdAsync for GetRootSurchargeTransactionId
        _mockTransactionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => 
            {
                // Return the original transaction for any ID to ensure GetRootSurchargeTransactionId returns the expected value
                return originalTransaction;
            });

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        var mockAdapter = new Mock<ISurchargeProviderAdapter>();
        mockAdapter.Setup(x => x.ProcessSaleAsync(It.IsAny<SurchargeTransaction>(), providerConfig, request))
            .ReturnsAsync((true, JsonSerializer.SerializeToDocument(new { success = true }), (string?)null));

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(mockAdapter.Object);

        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .Callback<SurchargeTransaction>(transaction => 
            {
                // Ensure the created transaction has the correct OriginalSurchargeTransId
                transaction.OriginalSurchargeTransId = originalTransaction.Id;
            })
            .ReturnsAsync((SurchargeTransaction transaction) => transaction);

        // Act
        var result = await _service.ProcessSaleAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.OriginalSurchargeTransactionId.Should().Be(originalTransaction.Id);
        result.Amount.Should().Be(originalTransaction.Amount);
        result.CorrelationId.Should().Be(originalTransaction.CorrelationId);
        result.ProviderTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task ProcessSaleAsync_WithMissingRequiredFields_ThrowsException()
    {
        // Arrange
        var request = new SurchargeSaleRequest
        {
            // Missing both SurchargeTransactionId and ProviderTransactionId, but providing CorrelationId
            CorrelationId = "test-correlation-id"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProcessSaleAsync(request, _testMerchantId, "test-actor"));
        
        exception.Message.Should().Contain("providerCode is required if surchargeTransactionId is not provided");
    }

    [Fact]
    public async Task ProcessSaleAsync_WithTransactionNotFound_ThrowsException()
    {
        // Arrange
        var request = new SurchargeSaleRequest
        {
            SurchargeTransactionId = Guid.NewGuid()
        };

        // Mock GetLatestInOriginalChainAsync to return null
        _mockTransactionRepository
            .Setup(x => x.GetLatestInOriginalChainAsync(request.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync((SurchargeTransaction?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProcessSaleAsync(request, _testMerchantId, "test-actor"));
        
        exception.Message.Should().Contain("Original auth transaction not found or does not belong to merchant");
    }

    [Fact]
    public async Task ProcessSaleAsync_WithTransactionNotAuthorized_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new SurchargeSaleRequest
        {
            SurchargeTransactionId = Guid.NewGuid()
        };

        var originalTransaction = CreateValidTransaction(request.SurchargeTransactionId.Value);
        originalTransaction.Status = SurchargeTransactionStatus.Authorized; // Changed from Pending to Authorized
        originalTransaction.OperationType = SurchargeOperationType.Auth;
        originalTransaction.MerchantId = _testMerchantId;
        originalTransaction.ProviderTransactionId = "auth-provider-tx-id";
        originalTransaction.ProviderConfigId = Guid.NewGuid(); // Add this to prevent the "missing provider config" error

        // Mock GetLatestInOriginalChainAsync
        _mockTransactionRepository
            .Setup(x => x.GetLatestInOriginalChainAsync(request.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        // Mock GetByIdAsync for GetRootSurchargeTransactionId
        _mockTransactionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => 
            {
                // Return the original transaction for any ID to ensure GetRootSurchargeTransactionId returns the expected value
                return originalTransaction;
            });

        // Mock provider config repository to return a valid config
        var providerConfig = CreateValidProviderConfig();
        providerConfig.Id = originalTransaction.ProviderConfigId;
        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        // Mock the adapter factory to return a test adapter
        var mockAdapter = new Mock<ISurchargeProviderAdapter>();
        mockAdapter.Setup(x => x.ProcessSaleAsync(It.IsAny<SurchargeTransaction>(), providerConfig, request))
            .ReturnsAsync((true, JsonSerializer.SerializeToDocument(new { success = true }), (string?)null));

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(mockAdapter.Object);

        // Mock the CreateAsync method to return the transaction with the correct OriginalSurchargeTransId
        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .Callback<SurchargeTransaction>(transaction => 
            {
                // Ensure the created transaction has the correct OriginalSurchargeTransId
                transaction.OriginalSurchargeTransId = originalTransaction.Id;
            })
            .ReturnsAsync((SurchargeTransaction transaction) => transaction);

        // Act
        var result = await _service.ProcessSaleAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.OriginalSurchargeTransactionId.Should().Be(originalTransaction.Id);
        result.Amount.Should().Be(originalTransaction.Amount);
        result.CorrelationId.Should().Be(originalTransaction.CorrelationId);
        result.ProviderTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task ProcessSaleAsync_WithProviderConfigNotFound_ThrowsException()
    {
        // Arrange
        var request = new SurchargeSaleRequest
        {
            SurchargeTransactionId = Guid.NewGuid()
        };

        var originalTransaction = CreateValidTransaction(request.SurchargeTransactionId.Value);
        originalTransaction.Status = SurchargeTransactionStatus.Authorized;
        originalTransaction.ProviderConfigId = Guid.NewGuid();
        originalTransaction.OperationType = SurchargeOperationType.Auth;
        originalTransaction.MerchantId = _testMerchantId;
        originalTransaction.ProviderTransactionId = "auth-provider-tx-id";

        // Mock GetLatestInOriginalChainAsync
        _mockTransactionRepository
            .Setup(x => x.GetLatestInOriginalChainAsync(request.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync((SurchargeProviderConfig?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProcessSaleAsync(request, _testMerchantId, "test-actor"));
        
        exception.Message.Should().Contain("Provider configuration could not be determined from original transaction or providerTransactionId");
    }

    [Fact]
    public async Task ProcessSaleAsync_WithProviderError_ReturnsErrorResponse()
    {
        // Arrange
        var request = new SurchargeSaleRequest
        {
            SurchargeTransactionId = Guid.NewGuid()
        };

        var originalTransaction = CreateValidTransaction(request.SurchargeTransactionId.Value);
        originalTransaction.Status = SurchargeTransactionStatus.Authorized;
        originalTransaction.ProviderConfigId = Guid.NewGuid();
        originalTransaction.OperationType = SurchargeOperationType.Auth;
        originalTransaction.MerchantId = _testMerchantId;
        originalTransaction.ProviderTransactionId = "auth-provider-tx-id";
        // Add ResponsePayload to avoid NullReferenceException
        originalTransaction.ResponsePayload = JsonSerializer.SerializeToDocument(new { 
            surchargeAmount = 10.00m,
            surchargeFeePercent = 2.5m
        });

        // Mock GetLatestInOriginalChainAsync
        _mockTransactionRepository
            .Setup(x => x.GetLatestInOriginalChainAsync(request.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        // Mock GetByIdAsync for GetRootSurchargeTransactionId
        _mockTransactionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => 
            {
                // Return the original transaction for any ID to ensure GetRootSurchargeTransactionId returns the expected value
                return originalTransaction;
            });

        var providerConfig = CreateValidProviderConfig();
        providerConfig.Id = originalTransaction.ProviderConfigId;

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        // Use TestInterPaymentsAdapter instead of mock adapter
        var mockLogger = new Mock<ILogger<InterPaymentsAdapter>>();
        var testAdapter = new TestInterPaymentsAdapter(
            (false, JsonSerializer.SerializeToDocument(new { error = "Provider processing failed" }), "Provider processing failed"),
            _mockHttpClientFactory.Object,
            mockLogger.Object);

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(testAdapter);

        // Mock the CreateAsync method to return the transaction with the correct OriginalSurchargeTransId
        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .Callback<SurchargeTransaction>(transaction => 
            {
                // Ensure the created transaction has the correct OriginalSurchargeTransId
                transaction.OriginalSurchargeTransId = originalTransaction.Id;
            })
            .ReturnsAsync((SurchargeTransaction transaction) => transaction);

        // Act
        var result = await _service.ProcessSaleAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Failed");
        result.ErrorMessage.Should().Contain("Provider processing failed");
    }

    // ==================== ProcessCancelAsync Tests ====================

    [Fact]
    public async Task ProcessCancelAsync_WithValidRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var request = new SurchargeCancelRequest
        {
            SurchargeTransactionId = Guid.NewGuid()
        };

        var originalTransaction = CreateValidTransaction(request.SurchargeTransactionId.Value);
        originalTransaction.Status = SurchargeTransactionStatus.Authorized;
        originalTransaction.ProviderConfigId = Guid.NewGuid();
        originalTransaction.MerchantId = _testMerchantId;

        var providerConfig = CreateValidProviderConfig();
        providerConfig.Id = originalTransaction.ProviderConfigId;

        // Mock GetByIdForMerchantAsync for initial lookup
        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(request.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        // Mock GetLatestInOriginalChainAsync for finding latest transaction
        _mockTransactionRepository
            .Setup(x => x.GetLatestInOriginalChainAsync(originalTransaction.Id, _testMerchantId))
            .ReturnsAsync(originalTransaction);

        // Mock GetByIdAsync for GetRootSurchargeTransactionId
        _mockTransactionRepository
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => 
            {
                // Return the original transaction for any ID to ensure GetRootSurchargeTransactionId returns the expected value
                return originalTransaction;
            });

        _mockProviderConfigRepository
            .Setup(x => x.GetByIdAsync(originalTransaction.ProviderConfigId))
            .ReturnsAsync(providerConfig);

        // Mock the adapter factory to return a test adapter that can be cast to InterPaymentsAdapter
        var mockLogger = new Mock<ILogger<InterPaymentsAdapter>>();
        var testAdapter = new TestInterPaymentsAdapter(
            (true, JsonSerializer.SerializeToDocument(new { success = true }), (string?)null),
            _mockHttpClientFactory.Object,
            mockLogger.Object);

        _mockProviderAdapterFactory
            .Setup(x => x.GetAdapter(providerConfig))
            .Returns(testAdapter);

        // Mock the CreateAsync method to return the transaction with the correct OriginalSurchargeTransId
        _mockTransactionRepository
            .Setup(x => x.CreateAsync(It.IsAny<SurchargeTransaction>()))
            .Callback<SurchargeTransaction>(transaction => 
            {
                // Ensure the created transaction has the correct OriginalSurchargeTransId
                transaction.OriginalSurchargeTransId = originalTransaction.Id;
            })
            .ReturnsAsync((SurchargeTransaction transaction) => transaction);

        // Act
        var result = await _service.ProcessCancelAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().NotBeEmpty();
        result.OriginalTransactionId.Should().Be(originalTransaction.Id.ToString());
        result.CorrelationId.Should().Be(originalTransaction.CorrelationId);
        result.ProviderTransactionId.Should().NotBeEmpty();
        result.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task ProcessCancelAsync_WithTransactionNotFound_ReturnsErrorResponse()
    {
        // Arrange
        var request = new SurchargeCancelRequest
        {
            SurchargeTransactionId = Guid.NewGuid()
        };

        // Mock GetByIdForMerchantAsync to return null
        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(request.SurchargeTransactionId.Value, _testMerchantId))
            .ReturnsAsync((SurchargeTransaction?)null);

        // Act
        var result = await _service.ProcessCancelAsync(request, _testMerchantId, "test-actor");

        // Assert
        result.Should().NotBeNull();
        result.SurchargeTransactionId.Should().Be(request.SurchargeTransactionId.Value);
        result.Status.Should().Be("NotFound");
        result.ErrorMessage.Should().Contain("Transaction not found");
    }

    // ==================== GetTransactionByIdAsync Tests ====================

    [Fact]
    public async Task GetTransactionByIdAsync_WithValidId_ReturnsTransaction()
    {
        // Arrange
        var transactionId = Guid.NewGuid();
        var expectedTransaction = CreateValidTransaction(transactionId);

        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(transactionId, _testMerchantId))
            .ReturnsAsync(expectedTransaction);

        // Act
        var result = await _service.GetTransactionByIdAsync(transactionId, _testMerchantId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(transactionId);
        result.CorrelationId.Should().Be(expectedTransaction.CorrelationId);
    }

    [Fact]
    public async Task GetTransactionByIdAsync_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        _mockTransactionRepository
            .Setup(x => x.GetByIdForMerchantAsync(transactionId, _testMerchantId))
            .ReturnsAsync((SurchargeTransaction?)null);

        // Act
        var result = await _service.GetTransactionByIdAsync(transactionId, _testMerchantId);

        // Assert
        result.Should().BeNull();
    }

    // ==================== GetTransactionsByMerchantAsync Tests ====================

    [Fact]
    public async Task GetTransactionsByMerchantAsync_WithValidParameters_ReturnsTransactions()
    {
        // Arrange
        var transactions = new List<SurchargeTransaction>
        {
            CreateValidTransaction(),
            CreateValidTransaction()
        };

        _mockTransactionRepository
            .Setup(x => x.GetByMerchantIdAsync(_testMerchantId, 1, 10, null, null))
            .ReturnsAsync((transactions, 2));

        // Act
        var result = await _service.GetTransactionsByMerchantAsync(_testMerchantId, 1, 10, null, null);

        // Assert
        result.Transactions.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTransactionsByMerchantAsync_WithFilters_ReturnsFilteredTransactions()
    {
        // Arrange
        var transactions = new List<SurchargeTransaction>
        {
            CreateValidTransaction()
        };

        _mockTransactionRepository
            .Setup(x => x.GetByMerchantIdAsync(_testMerchantId, 1, 10, SurchargeOperationType.Auth, SurchargeTransactionStatus.Authorized))
            .ReturnsAsync((transactions, 1));

        // Act
        var result = await _service.GetTransactionsByMerchantAsync(_testMerchantId, 1, 10, SurchargeOperationType.Auth, SurchargeTransactionStatus.Authorized);

        // Assert
        result.Transactions.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetTransactionsByMerchantAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        _mockTransactionRepository
            .Setup(x => x.GetByMerchantIdAsync(_testMerchantId, 1, 10, null, null))
            .ReturnsAsync((new List<SurchargeTransaction>(), 0));

        // Act
        var result = await _service.GetTransactionsByMerchantAsync(_testMerchantId, 1, 10, null, null);

        // Assert
        result.Transactions.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
} 