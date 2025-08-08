using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Models;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.Merchant.Requests;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.ApiKey.Requests;
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
            MerchantId = Guid.NewGuid(),
            Amount = 200.00m,
            CorrelationId = "test-correlation-id",
            ProviderConfigId = Guid.NewGuid(),
            ProviderTransactionId = "test-provider-tx-id",
            RequestPayload = JsonSerializer.SerializeToDocument(new { test = "data" }),
            OperationType = SurchargeOperationType.Sale,
            Status = SurchargeTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static SurchargeProviderConfig CreateValidProviderConfig(Guid? id = null)
    {
        return new SurchargeProviderConfig
        {
            Id = id ?? Guid.NewGuid(),
            ConfigName = "Test Config",
            MerchantId = Guid.NewGuid(),
            ProviderId = Guid.NewGuid(),
            Credentials = JsonSerializer.SerializeToDocument(new { jwt_token = "test-token", token_type = "Bearer" }),
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            IsPrimary = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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

    // Merchant-related test data builders
    public static GenerateInitialApiKeyRequest CreateValidGenerateInitialApiKeyRequest()
    {
        return new GenerateInitialApiKeyRequest
        {
            ExternalMerchantId = "test-merchant-123",
            ExternalMerchantGuid = Guid.NewGuid(),
            MerchantName = "Test Merchant",
            Description = "Test API Key",
            RateLimit = 1000,
            AllowedEndpoints = new[] { "/api/v1/surcharge" },
            OnboardingMetadata = CreateValidOnboardingMetadata()
        };
    }

    public static Merchant CreateValidMerchant(Guid? merchantId = null)
    {
        return new Merchant
        {
            MerchantId = merchantId ?? Guid.NewGuid(),
            ExternalMerchantId = "test-merchant-123",
            ExternalMerchantGuid = Guid.NewGuid(),
            Name = "Test Merchant",
            StatusId = 1, // Active - don't set navigation property to avoid tracking conflicts
            CreatedBy = "test-admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static MerchantStatus CreateValidMerchantStatus(int statusId = 1)
    {
        return new MerchantStatus
        {
            MerchantStatusId = statusId,
            Code = statusId switch
            {
                -2 => "SUSPENDED",
                -1 => "INACTIVE", 
                0 => "UNKNOWN",
                1 => "ACTIVE",
                2 => "PENDING",
                3 => "VERIFIED",
                _ => "UNKNOWN"
            },
            Name = statusId switch
            {
                -2 => "Suspended",
                -1 => "Inactive",
                0 => "Unknown", 
                1 => "Active",
                2 => "Pending",
                3 => "Verified",
                _ => "Unknown"
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static MerchantAuditTrail CreateValidMerchantAuditTrail(Guid merchantId)
    {
        return new MerchantAuditTrail
        {
            MerchantAuditTrailId = Guid.NewGuid(),
            MerchantId = merchantId,
            Action = "UPDATE",
            EntityType = "Name",
            PropertyName = "Name",
            OldValue = "Old Name",
            NewValue = "New Name",
            UpdatedBy = "test-admin",
            CreatedAt = DateTime.UtcNow
        };
    }

    public static UpdateMerchantRequest CreateValidUpdateMerchantRequest()
    {
        return new UpdateMerchantRequest
        {
            Name = "Updated Merchant Name"
        };
    }

    // API Key-related test data builders
    public static GenerateApiKeyRequest CreateValidGenerateApiKeyRequest()
    {
        return new GenerateApiKeyRequest
        {
            Description = "Test API Key",
            RateLimit = 1000,
            AllowedEndpoints = new[] { "/api/v1/surcharge" },
            Purpose = "PRODUCTION"
        };
    }

    public static ApiKey CreateValidApiKey(Guid? merchantId = null)
    {
        return new ApiKey
        {
            Key = Guid.NewGuid().ToString("N"),
            MerchantId = merchantId ?? Guid.NewGuid(),
            Name = "Test API Key",
            Description = "Test API Key Description",
            RateLimit = 1000,
            AllowedEndpoints = new[] { "/api/v1/surcharge" },
            Status = "ACTIVE",
            ExpirationDays = 30,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedBy = "test-admin",
            CreatedAt = DateTime.UtcNow,
            Purpose = "PRODUCTION"
        };
    }

    public static OnboardingMetadata CreateValidOnboardingMetadata()
    {
        return new OnboardingMetadata
        {
            AdminUserId = "admin-123",
            OnboardingReference = "onboarding-ref-456"
        };
    }

    // Extended SurchargeProviderConfig builders
    public static SurchargeProviderConfig CreateValidProviderConfigWithMerchant(Guid? merchantId = null, Guid? providerId = null, bool isPrimary = true)
    {
        return new SurchargeProviderConfig
        {
            Id = Guid.NewGuid(),
            MerchantId = merchantId ?? Guid.NewGuid(),
            ProviderId = providerId ?? Guid.NewGuid(),
            ConfigName = "Test Provider Config",
            Credentials = JsonSerializer.SerializeToDocument(new { jwt_token = "test-token", token_type = "Bearer" }),
            IsActive = true,
            IsPrimary = isPrimary,
            RateLimit = 100,
            RateLimitPeriod = 60,
            Timeout = 30,
            SuccessCount = 0,
            ErrorCount = 0,
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static List<SurchargeProviderConfig> CreateMultipleProviderConfigs(int count, Guid? merchantId = null, Guid? providerId = null)
    {
        var configs = new List<SurchargeProviderConfig>();
        for (int i = 0; i < count; i++)
        {
            var config = CreateValidProviderConfigWithMerchant(merchantId, providerId, i == 0); // First one is primary
            config.ConfigName = $"Test Config {i + 1}";
            configs.Add(config);
        }
        return configs;
    }

    // SurchargeProvider builders
    public static SurchargeProvider CreateValidSurchargeProvider(Guid? id = null)
    {
        return new SurchargeProvider
        {
            Id = id ?? Guid.NewGuid(),
            Code = "INTERPAYMENTS",
            Name = "InterPayments",
            BaseUrl = "https://test.interpayments.com",
            AuthenticationType = "JWT",
            CredentialsSchema = JsonSerializer.SerializeToDocument(new { 
                username = "string", 
                password = "string",
                jwt_token = "string"
            }),
            StatusId = 1,
            ProviderType = "PAYMENT",
            CreatedBy = "test-admin",
            UpdatedBy = "test-admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static List<SurchargeProvider> CreateMultipleProviders(int count, Guid? merchantId = null)
    {
        var providers = new List<SurchargeProvider>();
        for (int i = 0; i < count; i++)
        {
            var provider = CreateValidSurchargeProvider();
            provider.Code = $"PROVIDER_{i + 1}";
            provider.Name = $"Test Provider {i + 1}";
            providers.Add(provider);
        }
        return providers;
    }
} 