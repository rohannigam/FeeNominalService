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
using FeeNominalService.Models.SurchargeProvider;

namespace FeeNominalService.Services.Adapters.InterPayments;

public class InterPaymentsAdapter : ISurchargeProviderAdapter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InterPaymentsAdapter> _logger;

    public InterPaymentsAdapter(IHttpClientFactory httpClientFactory, ILogger<InterPaymentsAdapter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public (bool IsValid, string? ErrorMessage) ValidateRequest(SurchargeAuthRequest request)
    {
        // Accept both 2/3-letter and 3-digit IBAN country codes
        if (string.IsNullOrWhiteSpace(request.Country))
            return (false, SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.InterPayments.COUNTRY_REQUIRED));

        string normalizedCountry = request.Country.Trim().ToUpperInvariant();
        string ibanCountry = NormalizeCountryToIban(normalizedCountry);

        switch (normalizedCountry)
        {
            case "USA":
            case "US":
            case "840":
                if (string.IsNullOrWhiteSpace(request.PostalCode) ||
                    (!Regex.IsMatch(request.PostalCode ?? string.Empty, @"^\d{5}$") && !Regex.IsMatch(request.PostalCode ?? string.Empty, @"^\d{5}-\d{4}$")))
                    return (false, SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.InterPayments.POSTAL_CODE_INVALID_US));
                break;
            case "CAN":
            case "CANADA":
            case "124":
                if (string.IsNullOrWhiteSpace(request.PostalCode) ||
                    !Regex.IsMatch(request.PostalCode ?? string.Empty, @"^[A-Za-z]\d[A-Za-z][ ]?\d[A-Za-z]\d$"))
                    return (false, SurchargeErrorCodes.GetErrorMessage(SurchargeErrorCodes.InterPayments.POSTAL_CODE_INVALID_CANADA));
                break;
            default:
                // Accept as-is for other countries
                break;
        }

        return (true, null);
    }

    private static string NormalizeCountryToIban(string country)
    {
        if (string.IsNullOrWhiteSpace(country)) return country;
        string c = country.Trim().ToUpperInvariant();
        return c switch
        {
            "US" or "USA" => "840",
            "CA" or "CAN" or "CANADA" => "124",
            _ when Regex.IsMatch(c, "^\\d{3}$") => c,
            _ => c
        };
    }

    private static string GetBaseUrl(SurchargeProviderConfig providerConfig)
    {
        var baseUrl = providerConfig.Provider?.BaseUrl;
        if (!string.IsNullOrWhiteSpace(baseUrl))
            return baseUrl.TrimEnd('/');
        throw new SurchargeException("Provider baseUrl is not configured. Please set a valid baseUrl in the provider configuration.");
    }

    public async Task<SurchargeAuthResponse> CalculateSurchargeAsync(SurchargeAuthRequest request, SurchargeProviderConfig providerConfig)
    {
        var httpClient = _httpClientFactory.CreateClient();

        // Extract JWT token and token type from credentials
        var credentials = providerConfig.Credentials;
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
            ["country"] = NormalizeCountryToIban(request.Country),
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

        var baseUrl = GetBaseUrl(providerConfig);
        _logger.LogInformation("Using InterPayments baseUrl: {BaseUrl}", baseUrl);
        _logger.LogInformation("Sending request to Interpayments: {Url}", baseUrl);
        _logger.LogInformation("Interpayments Request JSON: {Request}", JsonSerializer.Serialize(interpaymentsRequest, new JsonSerializerOptions { WriteIndented = true }));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(baseUrl, interpaymentsRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request to InterPayments");
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
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Map response fields
        var surchargeAmount = root.GetProperty("transactionFee").GetDecimal();
        var providerTransactionId = root.TryGetProperty("sTxId", out var sTxIdProp) ? sTxIdProp.GetString() : null;
        var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
        var percent = root.TryGetProperty("transactionFeePercent", out var percentProp) ? percentProp.GetDecimal() : (decimal?)null;

        // Get provider information from the database through providerConfig
        var providerType = providerConfig.Provider?.ProviderType ?? "UNKNOWN";
        var providerCode = providerConfig.Provider?.Code ?? "UNKNOWN";
        var providerName = providerConfig.Provider?.Name ?? "Unknown";

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
            Provider = providerName,
            ProviderType = providerType,
            ProviderCode = providerCode,
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = null,
            SurchargeFeePercent = percent
        };
    }

    public async Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessSaleAsync(SurchargeTransaction saleTransaction, SurchargeProviderConfig providerConfig, SurchargeSaleRequest saleRequest)
    {
        var httpClient = _httpClientFactory.CreateClient();
        // Extract JWT token and token type from provider config credentials
        var credentials = providerConfig.Credentials;
        if (!credentials.RootElement.TryGetProperty("jwt_token", out var jwtTokenProp))
            return (false, null, "Missing JWT token in provider credentials.");
        var jwtToken = jwtTokenProp.GetString();
        var tokenType = "Bearer";
        if (credentials.RootElement.TryGetProperty("token_type", out var tokenTypeProp))
            tokenType = tokenTypeProp.GetString() ?? "Bearer";
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, jwtToken);

        // Build request payload
        var payload = new Dictionary<string, object?>
        {
            ["sTxId"] = saleTransaction.ProviderTransactionId
        };
        // Add mTxId from the current sale request or extracted from original auth
        var mTxId = saleRequest.MerchantTransactionId;
        if (string.IsNullOrWhiteSpace(mTxId) && saleTransaction.RequestPayload.RootElement.TryGetProperty("merchantTransactionId", out var mTxIdProp))
        {
            mTxId = mTxIdProp.GetString();
        }
        if (!string.IsNullOrWhiteSpace(mTxId))
        {
            payload["mTxId"] = mTxId;
        }

        var baseUrl = GetBaseUrl(providerConfig);
        var saleUrl = baseUrl + "/sale";
        _logger.LogInformation("Using InterPayments baseUrl: {BaseUrl}", baseUrl);
        _logger.LogInformation("Sending InterPayments sale request to: {Url}", saleUrl);
        _logger.LogInformation("InterPayments Sale Request JSON: {Request}", JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(saleUrl, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending sale request to InterPayments");
            return (false, null, ex.Message);
        }

        var json = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("InterPayments Sale Response JSON: {Response}", json);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("InterPayments sale API returned error: {StatusCode} {Content}", response.StatusCode, json);
            return (false, null, json);
        }

        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        // Expect sTxId in response
        var sTxId = root.TryGetProperty("sTxId", out var sTxIdProp) ? sTxIdProp.GetString() : null;
        if (string.IsNullOrWhiteSpace(sTxId))
        {
            return (false, doc, "Missing sTxId in InterPayments sale response.");
        }
        // Optionally, check for other fields or error messages
        return (true, doc, null);
    }

    public Task<(bool IsSuccess, JsonDocument? ResponsePayload, string? ErrorMessage)> ProcessBulkSaleAsync(List<SurchargeSaleRequest> sales, JsonDocument credentials)
    {
        // This method now requires providerConfig to be available elsewhere. If not, throw a NotImplementedException for now.
        throw new NotImplementedException("ProcessBulkSaleAsync requires providerConfig. Please update the interface or provide providerConfig another way.");
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
        var httpClient = _httpClientFactory.CreateClient();
        // Extract JWT token and token type from provider config credentials
        var credentials = providerConfig.Credentials;
        if (!credentials.RootElement.TryGetProperty("jwt_token", out var jwtTokenProp))
            return (false, null, "Missing JWT token in provider credentials.");
        var jwtToken = jwtTokenProp.GetString();
        var tokenType = "Bearer";
        if (credentials.RootElement.TryGetProperty("token_type", out var tokenTypeProp))
            tokenType = tokenTypeProp.GetString() ?? "Bearer";
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokenType, jwtToken);

        // Build request payload
        var payload = new Dictionary<string, object?>
        {
            ["sTxId"] = sTxId
        };
        if (!string.IsNullOrWhiteSpace(mTxId))
            payload["mTxId"] = mTxId;
        if (!string.IsNullOrWhiteSpace(cardToken))
            payload["cardToken"] = cardToken;
        if (!string.IsNullOrWhiteSpace(reasonCode))
            payload["reasonCode"] = reasonCode;
        if (data != null && data.Count > 0)
            payload["data"] = data;
        if (!string.IsNullOrWhiteSpace(authCode))
            payload["authCode"] = authCode;

        var baseUrl = GetBaseUrl(providerConfig);
        var cancelUrl = baseUrl + "/cancel";
        _logger.LogInformation("Using InterPayments baseUrl: {BaseUrl}", baseUrl);
        _logger.LogInformation("Sending InterPayments cancel request to: {Url}", cancelUrl);
        _logger.LogInformation("InterPayments Cancel Request JSON: {Request}", JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsJsonAsync(cancelUrl, payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cancel request to InterPayments");
            return (false, null, ex.Message);
        }

        var json = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("InterPayments Cancel Response JSON: {Response}", json);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("InterPayments cancel API returned error: {StatusCode} {Content}", response.StatusCode, json);
            return (false, null, json);
        }

        var doc = JsonDocument.Parse(json);
        return (true, doc, null);
    }
} 