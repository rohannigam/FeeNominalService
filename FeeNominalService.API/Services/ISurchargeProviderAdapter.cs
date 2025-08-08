namespace FeeNominalService.Services;

using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Models.SurchargeProvider;
using System.Text.Json;

public interface ISurchargeProviderAdapter
{
    (bool IsValid, string? ErrorMessage) ValidateRequest(SurchargeAuthRequest request);
    Task<SurchargeAuthResponse> CalculateSurchargeAsync(SurchargeAuthRequest request, SurchargeProviderConfig providerConfig);
    Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessSaleAsync(SurchargeTransaction transaction, SurchargeProviderConfig providerConfig, SurchargeSaleRequest saleRequest);
    Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessBulkSaleAsync(List<SurchargeSaleRequest> sales, SurchargeProviderConfig providerConfig);
    Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessRefundAsync(
        string sTxId,
        SurchargeProviderConfig providerConfig,
        decimal amount,
        string? mTxId = null,
        string? cardToken = null,
        List<string>? data = null);
} 