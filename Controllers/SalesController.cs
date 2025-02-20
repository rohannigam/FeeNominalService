using FeeNominalService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace FeeNominalService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SalesController> _logger;

        public SalesController(IHttpClientFactory httpClientFactory, ILogger<SalesController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("Completion")]
        public async Task<IActionResult> PostSaleRequest([FromBody] SaleRequest saleRequest)
        {
            if (saleRequest == null || string.IsNullOrEmpty(saleRequest.sTxId))
            {
                return BadRequest("sTxId is required.");
            }

            // Process the saleRequest here
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            var jsonContent = JsonSerializer.Serialize(saleRequest);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            _logger.LogInformation("Sale request: {SaleRequest}", jsonContent);

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseSaleAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Sale request processed successfully: {Content}", responseContent);
                return Ok(responseContent);
            }

            _logger.LogError("Error processing sale request: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);

            //return Ok("Sale request processed successfully.");
        }
    }
} 