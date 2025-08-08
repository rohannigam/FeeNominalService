using System.Net.Http;
using System.Text.Json;
using FeeNominalService.Models;
using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Services;
using FeeNominalService.Services.Adapters.InterPayments;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Tests.Services;

public class TestInterPaymentsAdapter : InterPaymentsAdapter
{
    private readonly (bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage) _mockResponse;

    public TestInterPaymentsAdapter(
        (bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage) mockResponse,
        IHttpClientFactory httpClientFactory,
        ILogger<InterPaymentsAdapter> logger) : base(httpClientFactory, logger)
    {
        _mockResponse = mockResponse;
    }

    public new (bool IsValid, string? ErrorMessage) ValidateRequest(SurchargeAuthRequest request)
    {
        return (true, null);
    }

    public async Task<SurchargeAuthResponse> CalculateSurchargeAsync(SurchargeAuthRequest request, SurchargeProviderConfig providerConfig)
    {
        await Task.CompletedTask;
        return new SurchargeAuthResponse
        {
            SurchargeTransactionId = Guid.NewGuid(),
            OriginalAmount = request.Amount,
            SurchargeAmount = 10.00m,
            TotalAmount = request.Amount + 10.00m,
            CorrelationId = request.CorrelationId,
            Status = "Authorized",
            Provider = "InterPayments",
            ProviderType = "INTERPAYMENTS",
            ProviderCode = "INTERPAYMENTS",
            ProcessedAt = DateTime.UtcNow
        };
    }

    public async Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessSaleAsync(
        SurchargeTransaction transaction, 
        SurchargeProviderConfig providerConfig, 
        SurchargeSaleRequest saleRequest)
    {
        await Task.CompletedTask;
        return _mockResponse;
    }

    public async Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessBulkSaleAsync(
        List<SurchargeSaleRequest> sales, 
        SurchargeProviderConfig providerConfig)
    {
        await Task.CompletedTask;
        return _mockResponse;
    }

    public async Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessCancelAsync(
        string sTxId,
        SurchargeProviderConfig providerConfig,
        string? mTxId = null,
        string? cardToken = null,
        string? reasonCode = null,
        List<string>? data = null,
        string? authCode = null)
    {
        await Task.CompletedTask;
        return _mockResponse;
    }

    public async Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessRefundAsync(
        string sTxId,
        SurchargeProviderConfig providerConfig,
        decimal amount,
        string? mTxId = null,
        string? cardToken = null,
        List<string>? data = null)
    {
        await Task.CompletedTask;
        return _mockResponse;
    }
}
