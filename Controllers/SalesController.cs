using FeeNominalService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;

namespace FeeNominalService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public SalesController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseSaleAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }

            return StatusCode((int)response.StatusCode, response.ReasonPhrase);

            //return Ok("Sale request processed successfully.");
        }
    }
} 