using System.Text;
using System.Text.Json;
using FeeNominalService.Models;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Services;

public class SaleService : ISaleService
{
    private readonly ILogger<SaleService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SaleService(IHttpClientFactory httpClientFactory, ILogger<SaleService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> ProcessSaleAsync(SaleRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            var jsonContent = JsonSerializer.Serialize(request);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            _logger.LogInformation("Sale request: {SaleRequest}", jsonContent);

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseSaleAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Sale request processed successfully: {Content}", responseContent);
                return responseContent;
            }

            _logger.LogError("Error processing sale request: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"Request failed with status code: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessSaleAsync");
            throw;
        }
    }

    public async Task<List<string>> ProcessBatchSalesAsync(List<SaleRequest> requests)
    {
        var tasks = requests.Select(ProcessSaleAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
} 