using System.Text;
using System.Text.Json;
using FeeNominalService.Models;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Services;

public class CancelService : ICancelService
{
    private readonly ILogger<CancelService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public CancelService(IHttpClientFactory httpClientFactory, ILogger<CancelService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> ProcessCancelAsync(CancelRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            var jsonContent = JsonSerializer.Serialize(request);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            _logger.LogInformation("Cancel request: {CancelRequest}", jsonContent);

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseCancelAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Cancel request processed successfully: {Content}", responseContent);
                return responseContent;
            }

            _logger.LogError("Error processing cancel request: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"Request failed with status code: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in processing cancel request");
            throw;
        }
    }

    public async Task<List<string>> ProcessBatchCancellationsAsync(List<CancelRequest> requests)
    {
        var tasks = requests.Select(ProcessCancelAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
} 