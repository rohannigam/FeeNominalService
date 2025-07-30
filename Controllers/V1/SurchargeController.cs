using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;
using FeeNominalService.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using FeeNominalService.Models.Common;
using FeeNominalService.Utils;

namespace FeeNominalService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/surcharge")]
    [ApiVersion("1.0")]
    [Authorize(Policy = "ApiKeyAccess")]
    public class SurchargeController : ControllerBase
    {
        private readonly ISurchargeTransactionService _surchargeTransactionService;
        private readonly ILogger<SurchargeController> _logger;

        public SurchargeController(
            ISurchargeTransactionService surchargeTransactionService,
            ILogger<SurchargeController> logger)
        {
            _surchargeTransactionService = surchargeTransactionService;
            _logger = logger;
        }

        /// <summary>
        /// Process surcharge authorization transaction
        /// </summary>
        /// <param name="request">Surcharge authorization request</param>
        /// <returns>Surcharge authorization response</returns>
        [HttpPost("auth")]
        public async Task<IActionResult> ProcessAuth([FromBody] SurchargeAuthRequest request)
        {
            try
            {
                _logger.LogInformation("Processing surcharge auth for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                // Removed direct request logging to prevent log forging vulnerabilities
                // Enhanced security: Only log non-sensitive, sanitized fields

                // Validate merchant ID from claims
                var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantIdClaim))
                {
                    _logger.LogWarning("Merchant ID not found in claims for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (!Guid.TryParse(merchantIdClaim, out Guid merchantId))
                {
                    _logger.LogWarning("Invalid merchant ID format in claims: {MerchantId} for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeMerchantId(merchantIdClaim), LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Invalid merchant ID format" });
                }

                // Extract API key or user identity for audit
                var apiKey = User.FindFirst("ApiKey")?.Value;
                var actor = !string.IsNullOrEmpty(apiKey) ? apiKey : "system";

                // Process the surcharge authorization
                var response = await _surchargeTransactionService.ProcessAuthAsync(request, merchantId, actor);

                _logger.LogInformation("Successfully processed surcharge auth for transaction: {CorrelationId}, surcharge transaction ID: {SurchargeTransactionId}", LogSanitizer.SanitizeString(request.CorrelationId), LogSanitizer.SanitizeGuid(response.SurchargeTransactionId));
                // Removed direct response logging to prevent log forging vulnerabilities
                // Enhanced security: Only log non-sensitive, sanitized fields

                return Ok(response);
            }
            catch (FeeNominalService.Exceptions.SurchargeException ex)
            {
                _logger.LogWarning(ex, "Surcharge error while processing auth for transaction: {CorrelationId}", LogSanitizer.SanitizeString(request.CorrelationId));
                return BadRequest(ex.ToErrorResponse());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing surcharge auth for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing surcharge auth for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return StatusCode(500, new { message = "An error occurred while processing the surcharge authorization" });
            }
        }

        /// <summary>
        /// Process surcharge sale transaction
        /// </summary>
        /// <param name="request">Surcharge sale request</param>
        /// <returns>Surcharge sale response</returns>
        [HttpPost("sale")]
        public async Task<IActionResult> ProcessSale([FromBody] SurchargeSaleRequest request)
        {
            try
            {
                _logger.LogInformation("Processing surcharge sale for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                // Removed direct request logging to prevent log forging vulnerabilities
                // Enhanced security: Only log non-sensitive, sanitized fields

                // Validate merchant ID from claims
                var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantIdClaim))
                {
                    _logger.LogWarning("Merchant ID not found in claims for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (!Guid.TryParse(merchantIdClaim, out Guid merchantId))
                {
                    _logger.LogWarning("Invalid merchant ID format in claims: {MerchantId} for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeMerchantId(merchantIdClaim), LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Invalid merchant ID format" });
                }

                // Extract API key or user identity for audit
                var apiKey = User.FindFirst("ApiKey")?.Value;
                var actor = !string.IsNullOrEmpty(apiKey) ? apiKey : "system";

                // Process the surcharge sale
                var response = await _surchargeTransactionService.ProcessSaleAsync(request, merchantId, actor);

                _logger.LogInformation("Successfully processed surcharge sale for transaction: {CorrelationId}, " +
                    "surcharge transaction ID: {SurchargeTransactionId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId), LogSanitizer.SanitizeGuid(response.SurchargeTransactionId));
                // Removed direct response logging to prevent log forging vulnerabilities
                // Enhanced security: Only log non-sensitive, sanitized fields

                return Ok(response);
            }
            catch (FeeNominalService.Exceptions.SurchargeException ex)
            {
                _logger.LogWarning(ex, "Surcharge error while processing sale for transaction: {CorrelationId}", LogSanitizer.SanitizeString(request.CorrelationId));
                return BadRequest(ex.ToErrorResponse());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing surcharge sale for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing surcharge sale for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return StatusCode(500, new { message = "An error occurred while processing the surcharge sale" });
            }
        }

        /// <summary>
        /// Process surcharge refund transaction
        /// </summary>
        /// <param name="request">Surcharge refund request</param>
        /// <returns>Surcharge refund response</returns>
        [HttpPost("refund")]
        public async Task<IActionResult> ProcessRefund([FromBody] SurchargeRefundRequest request)
        {
            try
            {
                _logger.LogInformation("Processing surcharge refund for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));

                // Validate merchant ID from claims
                var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantIdClaim))
                {
                    _logger.LogWarning("Merchant ID not found in claims for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (!Guid.TryParse(merchantIdClaim, out Guid merchantId))
                {
                    _logger.LogWarning("Invalid merchant ID format in claims: {MerchantId} for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeMerchantId(merchantIdClaim), LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Invalid merchant ID format" });
                }

                // Process the surcharge refund
                var response = await _surchargeTransactionService.ProcessRefundAsync(request, merchantId);

                _logger.LogInformation("Successfully processed surcharge refund for transaction: {CorrelationId}, " +
                    "refund ID: {RefundId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId), LogSanitizer.SanitizeGuid(response.RefundId));

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing surcharge refund for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing surcharge refund for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return StatusCode(500, new { message = "An error occurred while processing the surcharge refund" });
            }
        }

        /// <summary>
        /// Process surcharge cancellation transaction
        /// </summary>
        /// <param name="request">Surcharge cancel request</param>
        /// <returns>Surcharge cancel response</returns>
        [HttpPost("cancel")]
        public async Task<IActionResult> ProcessCancel([FromBody] SurchargeCancelRequest request)
        {
            try
            {
                _logger.LogInformation("Processing surcharge cancel for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));

                // Validate merchant ID from claims
                var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantIdClaim))
                {
                    _logger.LogWarning("Merchant ID not found in claims for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (!Guid.TryParse(merchantIdClaim, out Guid merchantId))
                {
                    _logger.LogWarning("Invalid merchant ID format in claims: {MerchantId} for transaction: {CorrelationId}", 
                        LogSanitizer.SanitizeMerchantId(merchantIdClaim), LogSanitizer.SanitizeString(request.CorrelationId));
                    return BadRequest(new { message = "Invalid merchant ID format" });
                }

                // Extract API key or user identity for audit
                var apiKey = User.FindFirst("ApiKey")?.Value;
                var actor = !string.IsNullOrEmpty(apiKey) ? apiKey : "system";

                // Process the surcharge cancellation
                var response = await _surchargeTransactionService.ProcessCancelAsync(request, merchantId, actor);

                _logger.LogInformation("Successfully processed surcharge cancel for transaction: {CorrelationId}, " +
                    "surcharge transaction ID: {SurchargeTransactionId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId), LogSanitizer.SanitizeGuid(response.SurchargeTransactionId));

                return Ok(response);
            }
            catch (FeeNominalService.Exceptions.SurchargeException ex)
            {
                _logger.LogWarning(ex, "Surcharge error while processing cancel for transaction: {CorrelationId}", LogSanitizer.SanitizeString(request.CorrelationId));
                return BadRequest(ex.ToErrorResponse());
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing surcharge cancel for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing surcharge cancel for transaction: {CorrelationId}", 
                    LogSanitizer.SanitizeString(request.CorrelationId));
                return StatusCode(500, new { message = "An error occurred while processing the surcharge cancellation" });
            }
        }

        /// <summary>
        /// Process a batch of surcharge sale transactions (admin, cross-merchant)
        /// </summary>
        /// <param name="request">Batch sale complete request</param>
        /// <returns>Batch sale complete response</returns>
        [HttpPost("bulk-sale-complete")]
        [Authorize(Policy = "ApiKeyAccess")]
        public async Task<IActionResult> BulkSaleComplete([FromBody] BulkSaleCompleteRequest request)
        {
            try
            {
                // Validate admin scope from claims
                var scopeClaim = User.FindFirst("Scope")?.Value;
                var isAdminClaim = User.FindFirst("IsAdmin")?.Value;
                var adminUser = User.FindFirst("ApiKey")?.Value ?? "unknown-admin";

                if (scopeClaim != "admin" || isAdminClaim != "true")
                {
                    _logger.LogWarning("Non-admin API key attempted to access bulk sale complete endpoint");
                    return Unauthorized(new ApiErrorResponse(
                        "Admin access required for bulk operations",
                        "INSUFFICIENT_PERMISSIONS"
                    ));
                }

                // Validate sales array
                if (request.Sales == null || request.Sales.Count == 0)
                {
                    _logger.LogWarning("No sales provided for bulk sale complete");
                    return BadRequest(new { message = "At least one sale item is required for bulk operations" });
                }

                // Validate each sale item
                for (int i = 0; i < request.Sales.Count; i++)
                {
                    var sale = request.Sales[i];
                    bool hasAuth = sale.SurchargeTransactionId.HasValue;
                    bool hasDirect = !string.IsNullOrWhiteSpace(sale.ProviderTransactionId)
                        && !string.IsNullOrWhiteSpace(sale.ProviderCode)
                        && !string.IsNullOrWhiteSpace(sale.CorrelationId);
                    if (!hasAuth && !hasDirect)
                    {
                        _logger.LogWarning("Sale item at index {Index} missing required identifiers", i);
                        return BadRequest(new { message = $"Each sale must have either surchargeTransactionId or all of providerTransactionId, providerCode, and correlationId (index {i})" });
                    }
                }

                _logger.LogInformation("[AUDIT] Admin {AdminUser} starting bulk sale complete. Count: {Count}", LogSanitizer.SanitizeString(adminUser), request.Sales?.Count ?? 0);
                var response = await _surchargeTransactionService.ProcessBulkSaleCompleteAsync(request);
                _logger.LogInformation("[AUDIT] Admin {AdminUser} completed bulk sale processing. BatchId: {BatchId}, Success: {SuccessCount}, Failed: {FailureCount}", LogSanitizer.SanitizeString(adminUser), LogSanitizer.SanitizeGuid(response.BatchId), response.SuccessCount, response.FailureCount);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation while processing bulk sale complete");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk sale complete");
                return StatusCode(500, new { message = "An error occurred while processing the bulk sale complete" });
            }
        }

        /// <summary>
        /// Get surcharge transaction by ID
        /// </summary>
        /// <param name="id">Surcharge transaction ID</param>
        /// <returns>Surcharge transaction details</returns>
        [HttpGet("transactions/{id}")]
        public async Task<IActionResult> GetTransactionById(Guid id)
        {
            try
            {
                _logger.LogInformation("Getting surcharge transaction by ID: {TransactionId}", LogSanitizer.SanitizeGuid(id));

                // Validate merchant ID from claims
                var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantIdClaim))
                {
                    _logger.LogWarning("Merchant ID not found in claims for transaction lookup: {TransactionId}", LogSanitizer.SanitizeGuid(id));
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (!Guid.TryParse(merchantIdClaim, out Guid merchantId))
                {
                    _logger.LogWarning("Invalid merchant ID format in claims: {MerchantId} for transaction lookup: {TransactionId}", 
                        LogSanitizer.SanitizeMerchantId(merchantIdClaim), LogSanitizer.SanitizeGuid(id));
                    return BadRequest(new { message = "Invalid merchant ID format" });
                }

                var transaction = await _surchargeTransactionService.GetTransactionByIdAsync(id, merchantId);
                if (transaction == null)
                {
                    _logger.LogWarning("Surcharge transaction not found: {TransactionId} for merchant: {MerchantId}", LogSanitizer.SanitizeGuid(id), LogSanitizer.SanitizeGuid(merchantId));
                    return NotFound(new { message = $"Surcharge transaction with ID {id} not found" });
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surcharge transaction by ID: {TransactionId}", LogSanitizer.SanitizeGuid(id));
                return StatusCode(500, new { message = "An error occurred while retrieving the surcharge transaction" });
            }
        }

        /// <summary>
        /// Get surcharge transactions for merchant with pagination and filtering
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Page size (default: 20, max: 100)</param>
        /// <param name="operationType">Filter by operation type</param>
        /// <param name="status">Filter by status</param>
        /// <returns>Paginated list of surcharge transactions</returns>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? operationType = null,
            [FromQuery] string? status = null)
        {
            try
            {
                _logger.LogInformation("Getting surcharge transactions for merchant, page: {Page}, pageSize: {PageSize}", page, pageSize);

                // Validate merchant ID from claims
                var merchantIdClaim = User.FindFirst("MerchantId")?.Value;
                if (string.IsNullOrEmpty(merchantIdClaim))
                {
                    _logger.LogWarning("Merchant ID not found in claims for transaction list");
                    return BadRequest(new { message = "Merchant ID not found in claims" });
                }

                if (!Guid.TryParse(merchantIdClaim, out Guid merchantId))
                {
                    _logger.LogWarning("Invalid merchant ID format in claims: {MerchantId}", LogSanitizer.SanitizeMerchantId(merchantIdClaim));
                    return BadRequest(new { message = "Invalid merchant ID format" });
                }

                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // Parse optional filters
                SurchargeOperationType? operationTypeEnum = null;
                if (!string.IsNullOrEmpty(operationType) && 
                    Enum.TryParse<SurchargeOperationType>(operationType, true, out var parsedOperationType))
                {
                    operationTypeEnum = parsedOperationType;
                }

                SurchargeTransactionStatus? statusEnum = null;
                if (!string.IsNullOrEmpty(status) && 
                    Enum.TryParse<SurchargeTransactionStatus>(status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }

                var (transactions, totalCount) = await _surchargeTransactionService.GetTransactionsByMerchantAsync(
                    merchantId, page, pageSize, operationTypeEnum, statusEnum);

                var response = new
                {
                    Transactions = transactions,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting surcharge transactions for merchant");
                return StatusCode(500, new { message = "An error occurred while retrieving surcharge transactions" });
            }
        }
    }
} 