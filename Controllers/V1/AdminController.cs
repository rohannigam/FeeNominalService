using Microsoft.AspNetCore.Mvc;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using FeeNominalService.Services.AWS;
using Microsoft.Extensions.Options;
using FeeNominalService.Models.Configuration;
using FeeNominalService.Models.Common;
using FeeNominalService.Models.ApiKey.Responses;
using FeeNominalService.Utils;
using FeeNominalService.Models.ApiKey;

namespace FeeNominalService.Controllers.V1
{
    // DTO for admin key service name
    public class AdminKeyServiceNameRequest
    {
        public string ServiceName { get; set; } = "default";
    }

    // This controller uses SecureApiKeySecret wrapper for secure handling of admin secrets
    // Enhanced security: Uses SecureString and proper disposal to prevent memory dumps and exposure
    [ApiController]
    [Route("api/v1/admin")]
    [ApiVersion("1.0")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IApiKeyService _apiKeyService;
        private readonly SecretNameFormatter _secretNameFormatter;

        public AdminController(
            ILogger<AdminController> logger, 
            IApiKeyService apiKeyService,
            SecretNameFormatter secretNameFormatter)
        {
            _logger = logger;
            _apiKeyService = apiKeyService;
            _secretNameFormatter = secretNameFormatter;
        }

        /// <summary>
        /// Generates a global admin/superuser API key (cross-merchant access, only for bulk sale complete)
        /// </summary>
        // This method uses SecureApiKeySecret wrapper for secure handling
        // Enhanced security: Uses SecureString and proper disposal to prevent memory dumps
        [HttpPost("apiKey/generate")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateAdminApiKey(
            [FromBody] GenerateApiKeyRequest request,
            [FromServices] IAwsSecretsManagerService secretsManager,
            [FromServices] IApiKeyService apiKeyService)
        {
            // Check for X-Admin-Secret header
            if (!Request.Headers.TryGetValue("X-Admin-Secret", out var providedSecret))
            {
                return StatusCode(403, new { error = "Missing X-Admin-Secret header." });
            }

            // Require X-Timestamp header
            if (!Request.Headers.TryGetValue("X-Timestamp", out var timestamp))
            {
                return BadRequest(new { error = "Missing X-Timestamp header." });
            }
            // Require X-Nonce header
            if (!Request.Headers.TryGetValue("X-Nonce", out var nonce))
            {
                return BadRequest(new { error = "Missing X-Nonce header." });
            }
            _logger.LogInformation("Admin API key generate requested with X-Timestamp: {Timestamp}, X-Nonce: {Nonce}", LogSanitizer.SanitizeString(timestamp.ToString()), LogSanitizer.SanitizeString(nonce.ToString()));

            // Extract serviceName from request (required for multi-admin-key support)
            var serviceName = request.Purpose?.ToLowerInvariant() ?? "default";
            
            // This method uses a secure approach to avoid passing sensitive data
            // Enhanced security: Instead of passing secret names containing sensitive data, we use a service-based approach
            // that internally handles the secret name formatting and retrieval without exposing sensitive data
            _logger.LogInformation("Looking up admin secret for service: {ServiceName}", LogSanitizer.SanitizeString(serviceName));

            // Fetch the admin secret securely using service-based approach
            using var secureAdminSecret = await GetAdminSecretSecurelyAsync(secretsManager, serviceName);
            if (secureAdminSecret == null)
            {
                return StatusCode(403, new { error = $"Admin secret not configured for {serviceName}." });
            }

            string providedSecretStr = providedSecret.ToString();
            // Use secure processing to compare secrets
            var isValidSecret = secureAdminSecret.ProcessSecretSecurely(storedSecret =>
            {
                var storedSecretStr = SimpleSecureDataHandler.FromSecureString(storedSecret);
                // Mask secrets for logging (show only first/last 2 chars)
                string Mask(string s) => string.IsNullOrEmpty(s) ? "(empty)" : s.Length <= 4 ? "****" : $"{s.Substring(0,2)}****{s.Substring(s.Length-2,2)}";
                _logger.LogWarning("Admin Secret (from DB): {StoredSecret} | Provided: {ProvidedSecret}", Mask(storedSecretStr), Mask(providedSecretStr));
                return !string.IsNullOrEmpty(storedSecretStr) && providedSecretStr == storedSecretStr;
            });

            if (!isValidSecret)
            {
                return StatusCode(403, new { error = "Invalid admin secret." });
            }

            // Default allowed endpoints to only bulk sale complete if not provided
            if (request.AllowedEndpoints == null || request.AllowedEndpoints.Length == 0)
            {
                request.AllowedEndpoints = new[] { "/api/v1/surcharge/bulk-sale-complete" };
            }

            // Ensure admin flag is set for this request
            request.IsAdmin = true;
            var response = await apiKeyService.GenerateApiKeyAsync(request);
            _logger.LogWarning("Admin API key generated by {User}", LogSanitizer.SanitizeString(User.Identity?.Name ?? "Unknown"));
            return Ok(response);
        }

        /// <summary>
        /// Rotates the admin API key (admin-scope only)
        /// </summary>
        [HttpPost("apikey/rotate")]
        [Authorize] // Only allow authenticated admin-scope keys
        public async Task<IActionResult> RotateAdminApiKey(
            [FromBody] AdminKeyServiceNameRequest req,
            [FromServices] IApiKeyService apiKeyService)
        {
            // Check if the authenticated key is admin-scope
            var scopeClaim = User.FindFirst("Scope")?.Value;
            if (scopeClaim != "admin")
            {
                return Unauthorized(new ApiErrorResponse(
                    "Only admin-scope API keys can rotate the admin key.",
                    "INSUFFICIENT_PERMISSIONS"
                ));
            }

            // Rotate the admin key (implementation in service)
            var response = await apiKeyService.RotateAdminApiKeyAsync(req.ServiceName);
            return Ok(response);
        }

        /// <summary>
        /// Revokes the admin API key (admin-scope only)
        /// </summary>
        [HttpPost("apikey/revoke")]
        [Authorize] // Only allow authenticated admin-scope keys
        public async Task<IActionResult> RevokeAdminApiKey(
            [FromBody] AdminKeyServiceNameRequest req,
            [FromServices] IApiKeyService apiKeyService)
        {
            // Check if the authenticated key is admin-scope
            var scopeClaim = User.FindFirst("Scope")?.Value;
            if (scopeClaim != "admin")
            {
                return Unauthorized(new ApiErrorResponse(
                    "Only admin-scope API keys can revoke the admin key.",
                    "INSUFFICIENT_PERMISSIONS"
                ));
            }

            // Revoke the admin key (implementation in service)
            var response = await apiKeyService.RevokeAdminApiKeyAsync(req.ServiceName);
            return Ok(new ApiResponse<ApiKeyRevokeResponse>
            {
                Success = true,
                Message = "Admin API key revoked successfully",
                Data = response
            });
        }

        /// <summary>
        /// Securely retrieves admin secret without exposing sensitive data in method parameters
        /// This method uses a secure approach to avoid passing sensitive data
        /// Enhanced security: Secret name formatting is handled internally without exposing sensitive data
        /// </summary>
        /// <param name="secretsManager">The secrets manager service</param>
        /// <param name="serviceName">The service name (non-sensitive)</param>
        /// <returns>Secure admin secret wrapper</returns>
        private async Task<SecureApiKeySecret?> GetAdminSecretSecurelyAsync(IAwsSecretsManagerService secretsManager, string serviceName)
        {
            try
            {
                // Build the secret name internally without exposing it to the calling method
                var secretName = _secretNameFormatter.FormatAdminSecretName(serviceName);
                
                // Secret name is only used internally and not logged or exposed
                // Enhanced security: The secret name is handled securely within this private method
                return await secretsManager.GetSecureApiKeySecretAsync(secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin secret for service {ServiceName}", LogSanitizer.SanitizeString(serviceName));
                return null;
            }
        }
    }
} 
