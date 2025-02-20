using FeeNominalService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace FeeNominalService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RefundsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<RefundsController> _logger;

        public RefundsController(IHttpClientFactory httpClientFactory,ILogger<RefundsController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost("RequestRefund")]
        public async Task<IActionResult> PostRefundRequest([FromBody] RefundRequest refundRequest)
        {
            if (refundRequest == null)
            {
                _logger.LogWarning("Received a null refund request.");
                return BadRequest("Refund request cannot be null.");
            }

            _logger.LogInformation("Processing refund request with sTxId: {sTxId}, mTxId: {mTxId}", refundRequest.sTxId, refundRequest.mTxId);

            // Process the refundRequest here
            // Process the saleRequest here
            var client = _httpClientFactory.CreateClient("InterpaymentsClient");

            var jsonContent = JsonSerializer.Serialize(refundRequest);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            _logger.LogInformation("Refund request: {RefundRequest}", jsonContent);

            var response = await client.PostAsync(ApiConstants.InterpaymentsBaseRefundAddress, stringContent);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Refund request processed successfully: {Content}", responseContent);
                return Ok(responseContent);
            }

            _logger.LogError("Error processing Refund request: {StatusCode} - {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
            return StatusCode((int)response.StatusCode, response.ReasonPhrase);
        }
    }
} 