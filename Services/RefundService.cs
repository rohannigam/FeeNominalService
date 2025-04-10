using System.Text;
using System.Text.Json;
using FeeNominalService.Models;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Services;

public class RefundService : IRefundService
{
    private readonly ILogger<RefundService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public RefundService(IHttpClientFactory httpClientFactory, ILogger<RefundService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> ProcessRefundAsync(RefundRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            var jsonContent = JsonSerializer.Serialize(request);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            _logger.LogInformation("Refund request: {RefundRequest}", jsonContent);

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseRefundAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Refund request processed successfully: {Content}", responseContent);
                return responseContent;
            }

            _logger.LogError("Error processing refund request: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"Request failed with status code: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in processing refund request");
            throw;
        }
    }

    public async Task<List<string>> ProcessBatchRefundsAsync(List<RefundRequest> requests)
    {
        var tasks = requests.Select(ProcessRefundAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
} 