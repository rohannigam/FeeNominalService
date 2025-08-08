using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Models;
using FeeNominalService.Models.SurchargeProvider;
using System.Text.Json;

namespace FeeNominalService.Tests.TestHelpers;

public static class TestDataBuilder
{
    public static SurchargeRefundRequest CreateValidRefundRequest()
    {
        return new SurchargeRefundRequest
        {
            SurchargeTransactionId = Guid.NewGuid(),
            Amount = 100.00m,
            CorrelationId = "test-correlation-id",
            MerchantTransactionId = "test-merchant-tx-id",
            RefundReason = "Customer request"
        };
    }

    public static SurchargeTransaction CreateValidSurchargeTransaction(Guid? id = null)
    {
        return new SurchargeTransaction
        {
            Id = id ?? Guid.NewGuid(),
            Amount = 200.00m,
            CorrelationId = "test-correlation-id",
            ProviderConfigId = Guid.NewGuid(),
            ProviderTransactionId = "test-provider-tx-id",
            MerchantTransactionId = "test-merchant-tx-id",
            RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
    }

    public static SurchargeProviderConfig CreateValidProviderConfig(Guid? id = null)
    {
        return new SurchargeProviderConfig
        {
            Id = id ?? Guid.NewGuid(),
            ConfigName = "Test Config",
            Credentials = JsonSerializer.SerializeToDocument(new { jwt_token = "test-token", token_type = "Bearer" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            IsPrimary = true
        };
    }

    public static (bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage) CreateSuccessfulInterPaymentsResponse()
    {
        var response = new
        {
            refundId = Guid.NewGuid().ToString(),
            refund = 5.00m,
            previouslyRefundedTransactionFees = 0.00m,
            originalTransactionFee = 10.00m,
            message = "Refund processed successfully"
        };

        return (true, JsonDocument.Parse(JsonSerializer.Serialize(response)), null);
    }

    public static (bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage) CreateFailedInterPaymentsResponse()
    {
        return (false, null, "Refund amount exceeds original transaction amount");
    }

    public static (bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage) CreateWarningInterPaymentsResponse()
    {
        var response = new
        {
            refundId = Guid.NewGuid().ToString(),
            refund = 0.00m,
            previouslyRefundedTransactionFees = 10.00m,
            originalTransactionFee = 10.00m,
            message = "Asking for too much refund"
        };

        return (true, JsonDocument.Parse(JsonSerializer.Serialize(response)), null);
    }

    public static SurchargeRefundResponse CreateSuccessfulRefundResponse()
    {
        return new SurchargeRefundResponse
        {
            SurchargeTransactionId = Guid.NewGuid(),
            OriginalSurchargeTransactionId = Guid.NewGuid(),
            RefundAmount = 100.00m,
            OriginalAmount = 200.00m,
            CorrelationId = "test-correlation-id",
            ProviderCode = "INTERPAYMENTS",
            ProviderType = "PAYMENT",
            Status = "Completed",
            ProcessedAt = DateTime.UtcNow,
            MerchantTransactionId = "test-merchant-tx-id",
            ProviderTransactionId = "test-provider-tx-id",
            OriginalProviderTransactionId = "original-provider-tx-id",
            RefundTransactionFee = 5.00m,
            PrevRefundedTransactionFees = 0.00m,
            OriginalTransactionFee = 10.00m
        };
    }

    public static SurchargeRefundResponse CreateErrorRefundResponse()
    {
        return new SurchargeRefundResponse
        {
            SurchargeTransactionId = Guid.Empty,
            OriginalSurchargeTransactionId = Guid.NewGuid(),
            RefundAmount = 100.00m,
            OriginalAmount = 0.00m,
            CorrelationId = "test-correlation-id",
            ProviderCode = string.Empty,
            ProviderType = string.Empty,
            Status = "NotFound",
            ProcessedAt = DateTime.UtcNow,
            Error = new RefundErrorDetails
            {
                Code = "TRANSACTION_NOT_FOUND",
                Message = "Transaction not found."
            }
        };
    }

    public static List<SurchargeTransaction> CreateMultipleTransactions(int count)
    {
        var transactions = new List<SurchargeTransaction>();
        for (int i = 0; i < count; i++)
        {
            transactions.Add(CreateValidSurchargeTransaction());
        }
        return transactions;
    }
} 