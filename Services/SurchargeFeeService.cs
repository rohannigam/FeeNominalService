using FeeNominalService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
namespace FeeNominalService.Services;
public class SurchargeFeeService : ISurchargeFeeService
{
    private readonly ILogger<SurchargeFeeService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public SurchargeFeeService(IHttpClientFactory httpClientFactory, ILogger<SurchargeFeeService> logger)
    {
         _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> CalculateSurchargeAsync(SurchargeRequest request)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            if (request.amount == null)
            {
                throw new ArgumentException("Amount is required");
            }

            // Add more validations here : nicn, processor, etc.
            if (request.nicn == null)
            {
                throw new ArgumentException("NICN is required");
            }
            if (request.processor == null)
            {
                request.processor = "default";
            }
            var jsonContent = JsonSerializer.Serialize(request);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            _logger.LogInformation("Surcharge Calculation request: {SurchargeFeeCalculation}", jsonContent);

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("SurchargeFeeCalculation request processed successfully: {Content}", responseContent);
                return responseContent;
            }

            _logger.LogError("Error processing SurchargeFeeCalculation request: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            throw new HttpRequestException($"Request failed with status code: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CalculateSurchargeAsync");
            throw;
        }
    }

    public async Task<List<string>> CalculateBatchSurchargesAsync(List<SurchargeRequest> requests)
    {
        var tasks = requests.Select(CalculateSurchargeAsync);
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }
} 