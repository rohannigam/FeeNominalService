using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Models.Common;
using FeeNominalService.Exceptions;
using Microsoft.Extensions.Logging;
using FeeNominalService.Utils;

namespace FeeNominalService.Services.Adapters.InterPayments;

public class InterPaymentsAdapter : ISurchargeProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InterPaymentsAdapter> _logger;
    private const string InterpaymentsUrl = ApiConstants.InterpaymentsBaseAddress;

    public InterPaymentsAdapter(IHttpClientFactory httpClientFactory, ILogger<InterPaymentsAdapter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public (bool IsValid, string? ErrorMessage) ValidateRequest(SurchargeAuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PostalCode))
            return (false, SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.InterPayments.POSTAL_CODE_REQUIRED));

        switch (request.Country.ToUpperInvariant())
        {
            case "USA":
            case "US":
                if (!Regex.IsMatch(request.PostalCode, @"^\d{5}$") && !Regex.IsMatch(request.PostalCode, @"^\d{5}-\d{4}$"))
                    return (false, SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.InterPayments.POSTAL_CODE_INVALID_US));
                break;
            case "CAN":
            case "CANADA":
                if (!Regex.IsMatch(request.PostalCode, @"^[A-Za-z]\d[A-Za-z]\d[A-Za-z]\d$"))
                    return (false, SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.InterPayments.POSTAL_CODE_INVALID_CANADA));
                break;
            default:
                // Accept as-is for other countries
                break;
        }

        return (true, null);
    }

    public async Task<SurchargeAuthResponse> CalculateSurchargeAsync(SurchargeAuthRequest request, JsonDocument credentials)
    {
        var httpClient = _httpClientFactory.CreateClient();

        // Extract JWT token and token type from credentials
        if (!credentials.RootElement.TryGetProperty("jwt_token", out var jwtTokenProp))
            throw new SurchargeException(SurchargeErrorCodes.InterPayments.MISSING_JWT_TOKEN);
        var jwtToken = jwtTokenProp.GetString();
        var tokenType = "Bearer";
        if (credentials.RootElement.TryGetProperty("token_type", out var tokenTypeProp))
            tokenType = tokenTypeProp.GetString() ?? "Bearer";

        // Mask jwtToken for logging
        var maskedJwt = Masker.MaskSecret(jwtToken);
        _logger.LogDebug("Using masked jwtToken for InterPayments: {MaskedJwt}", maskedJwt);

        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, jwtToken);

        var interpaymentsRequest = new Dictionary<string, object?>
        {
            ["nicn"] = request.BinValue,
            ["processor"] = request.SurchargeProcessor,
            ["amount"] = request.Amount,
            ["country"] = request.Country,
            ["region"] = request.PostalCode,
            ["mTxId"] = request.MerchantTransactionId,
            ["entryMethod"] = request.EntryMethod
        };
        if (request.TotalAmount.HasValue && request.TotalAmount.Value > 0)
        {
            interpaymentsRequest["totalAmount"] = request.TotalAmount.Value;
        }
        if (request.NonSurchargableAmount.HasValue)
        {
            interpaymentsRequest["nonSurchargableAmount"] = request.NonSurchargableAmount.Value;
        }
        if (request.Campaign != null)
        {
            interpaymentsRequest["campaign"] = request.Campaign;
        }
        if (request.Data != null)
        {
            interpaymentsRequest["data"] = request.Data;
        }
        if (request.CardToken != null)
        {
            interpaymentsRequest["cardToken"] = request.CardToken;
        }
        if (request.ProviderTransactionId != null)
        {
            interpaymentsRequest["sTxId"] = request.ProviderTransactionId;
        }

        _logger.LogInformation("Sending request to Interpayments: {Url}", InterpaymentsUrl);
        _logger.LogInformation("Interpayments Request JSON: {Request}", JsonSerializer.Serialize(interpaymentsRequest, new JsonSerializerOptions { WriteIndented = true }));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(InterpaymentsUrl, interpaymentsRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request to Interpayments");
            throw new SurchargeException(SurchargeErrorCodes.InterPayments.SEND_REQUEST_FAILED, ex.Message, ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Interpayments API returned error: {StatusCode} {Content}", response.StatusCode, errorContent);
            throw new SurchargeException(SurchargeErrorCodes.InterPayments.API_ERROR, errorContent);
        }

        var json = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Interpayments Response JSON: {Response}", JsonSerializer.Serialize(JsonDocument.Parse(json).RootElement, new JsonSerializerOptions { WriteIndented = true }));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Map response fields
        var surchargeAmount = root.GetProperty("transactionFee").GetDecimal();
        var providerTransactionId = root.TryGetProperty("sTxId", out var sTxIdProp) ? sTxIdProp.GetString() : null;
        var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
        var percent = root.TryGetProperty("transactionFeePercent", out var percentProp) ? percentProp.GetDecimal() : (decimal?)null;

        return new SurchargeAuthResponse
        {
            SurchargeTransactionId = Guid.Empty, // Will be replaced by the actual transaction ID in the service
            CorrelationId = request.CorrelationId,
            MerchantTransactionId = request.MerchantTransactionId,
            ProviderTransactionId = providerTransactionId,
            OriginalAmount = request.Amount,
            SurchargeAmount = surchargeAmount,
            TotalAmount = (request.Amount + surchargeAmount),
            Status = message ?? "ok",
            Provider = "InterPayments",
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = null,
            SurchargeFeePercent = percent
        };
    }
} 