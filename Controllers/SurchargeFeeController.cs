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

        public SurchargeFeeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateSurchargeFee([FromBody] TransactionFeeRequest request)
        {
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            var jsonContent = JsonSerializer.Serialize(request);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }

            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
        }
    }
} 