using Microsoft.AspNetCore.Mvc;
using FeeNominalService.Models;
using FeeNominalService.Services;
using Microsoft.AspNetCore.Authorization;

namespace FeeNominalService.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[ApiVersion("1.0")]
[Authorize(Policy = "ApiKeyAccess")]
public class CancelController : ControllerBase
{
    private readonly ICancelService _cancelService;
    private readonly ILogger<CancelController> _logger;

    public CancelController(ICancelService cancelService, ILogger<CancelController> logger)
    {
        _cancelService = cancelService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Cancel([FromBody] CancelRequest request)
    {
        try
        {
            _logger.LogInformation("Processing cancel request for transaction {TxId}", request.sTxId);
            var result = await _cancelService.ProcessCancelAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cancel request for transaction {TxId}", request.sTxId);
            return StatusCode(500, "An error occurred while processing the cancel request");
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> BatchCancel([FromBody] BatchCancelRequest request)
    {
        try
        {
            _logger.LogInformation("Processing batch cancel request for {Count} transactions", request.Cancellations.Count);
            var results = await _cancelService.ProcessBatchCancellationsAsync(request.Cancellations);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch cancel request");
            return StatusCode(500, "An error occurred while processing the batch cancel request");
        }
    }
} 