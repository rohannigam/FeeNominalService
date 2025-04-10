using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeeNominalService.Models;
using FeeNominalService.Services;

namespace FeeNominalService.Controllers.Common
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SurchargeFeeController : ControllerBase
    {
        private readonly ISurchargeFeeService _surchargeFeeService;
        private readonly ILogger<SurchargeFeeController> _logger;

        public SurchargeFeeController(
            ISurchargeFeeService surchargeFeeService,
            ILogger<SurchargeFeeController> logger)
        {
            _surchargeFeeService = surchargeFeeService;
            _logger = logger;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateSurcharge([FromBody] SurchargeRequest request)
        {
            try
            {
                var result = await _surchargeFeeService.CalculateSurchargeAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating surcharge");
                return StatusCode(500, new { message = "An error occurred while calculating the surcharge" });
            }
        }

        [HttpPost("calculate-batch")]
        public async Task<IActionResult> CalculateBatchSurcharges([FromBody] BatchSurchargeRequest request)
        {
            try
            {
                var results = await _surchargeFeeService.CalculateBatchSurchargesAsync(request.Transactions);
                return Ok(new { results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating batch surcharges");
                return StatusCode(500, new { message = "An error occurred while calculating batch surcharges" });
            }
        }
    }
} 