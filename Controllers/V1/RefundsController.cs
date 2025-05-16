using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeeNominalService.Models;
using FeeNominalService.Services;

namespace FeeNominalService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "ApiKeyAccess")]
    public class RefundsController : ControllerBase
    {
        private readonly IRefundService _refundService;
        private readonly ILogger<RefundsController> _logger;

        public RefundsController(
            IRefundService refundService,
            ILogger<RefundsController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessRefund([FromBody] RefundRequest request)
        {
            try
            {
                var result = await _refundService.ProcessRefundAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund");
                return StatusCode(500, new { message = "An error occurred while processing the refund" });
            }
        }

        [HttpPost("process-batch")]
        public async Task<IActionResult> ProcessBatchRefunds([FromBody] BatchRefundRequest request)
        {
            try
            {
                var results = await _refundService.ProcessBatchRefundsAsync(request.Refunds);
                return Ok(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch refunds");
                return StatusCode(500, new { message = "An error occurred while processing batch refunds" });
            }
        }
    }
} 