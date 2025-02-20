using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using FeeNominalService.Models;

namespace FeeNominalService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SurchargeFeeController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SalesController> _logger;

        public SurchargeFeeController(IHttpClientFactory httpClientFactory, ILogger<SalesController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("calculation")]
        public async Task<IActionResult> CalculateSurchargeFee([FromBody] TransactionFeeRequest request)
        {
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            var jsonContent = JsonSerializer.Serialize(request);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            _logger.LogInformation("Surcharge Calculation request: {SurchargeFeeCalculation}", jsonContent);

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("SurchargeFeeCalculation request processed successfully: {Content}", responseContent);
                return Ok(responseContent);
            }

             _logger.LogError("Error processing SurchargeFeeCalculation request: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
        }
    }
} 