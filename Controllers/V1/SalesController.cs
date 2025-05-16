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
    public class SalesController : ControllerBase
    {
        private readonly ISaleService _saleService;
        private readonly ILogger<SalesController> _logger;

        public SalesController(
            ISaleService saleService,
            ILogger<SalesController> logger)
        {
            _saleService = saleService;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessSale([FromBody] SaleRequest request)
        {
            try
            {
                var result = await _saleService.ProcessSaleAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sale");
                return StatusCode(500, new { message = "An error occurred while processing the sale" });
            }
        }

        [HttpPost("process-batch")]
        public async Task<IActionResult> ProcessBatchSales([FromBody] BatchSaleRequest request)
        {
            try
            {
                var results = await _saleService.ProcessBatchSalesAsync(request.Sales);
                return Ok(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch sales");
                return StatusCode(500, new { message = "An error occurred while processing batch sales" });
            }
        }
    }
} 