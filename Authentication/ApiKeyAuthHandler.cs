using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FeeNominalService.Services;
using FeeNominalService.Models.Configuration;
using System.Security.Claims;

namespace FeeNominalService.Authentication
{
    public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IRequestSigningService _requestSigningService;
        private readonly ILogger<ApiKeyAuthHandler> _logger;
        private readonly ApiKeyConfiguration _apiKeyConfig;
        private readonly IApiKeyService _apiKeyService;

        public ApiKeyAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IRequestSigningService requestSigningService,
            IOptions<ApiKeyConfiguration> apiKeyConfig,
            IApiKeyService apiKeyService)
            : base(options, logger, encoder)
        {
            _requestSigningService = requestSigningService;
            _logger = logger.CreateLogger<ApiKeyAuthHandler>();
            _apiKeyConfig = apiKeyConfig.Value;
            _apiKeyService = apiKeyService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                // Skip authentication for public endpoints
                if (Request.Path.StartsWithSegments("/api/v1/onboarding/apikey/generate") ||
                    Request.Path.StartsWithSegments("/api/v1/ping"))
                {
                    return AuthenticateResult.NoResult();
                }

                // Get required headers
                if (!Request.Headers.TryGetValue("X-Merchant-ID", out var merchantId) ||
                    !Request.Headers.TryGetValue("X-API-Key", out var apiKey) ||
                    !Request.Headers.TryGetValue("X-Timestamp", out var timestamp) ||
                    !Request.Headers.TryGetValue("X-Nonce", out var nonce) ||
                    !Request.Headers.TryGetValue("X-Signature", out var signature))
                {
                    _logger.LogWarning("Missing required headers for API key authentication");
                    return AuthenticateResult.Fail("Missing required headers");
                }

                // Read request body
                string requestBody;
                Request.EnableBuffering();
                using (var reader = new StreamReader(Request.Body, leaveOpen: true))
                {
                    requestBody = await reader.ReadToEndAsync();
                    Request.Body.Position = 0;
                }

                // Validate request
                var isValid = await _requestSigningService.ValidateRequestAsync(
                    merchantId.ToString(),
                    apiKey.ToString(),
                    timestamp.ToString(),
                    nonce.ToString(),
                    requestBody,
                    signature.ToString());

                if (!isValid)
                {
                    _logger.LogWarning("Invalid API key authentication for merchant: {MerchantId}", merchantId.ToString());
                    return AuthenticateResult.Fail("Invalid API key or signature");
                }

                // Create claims identity
                var claims = new[]
                {
                    new System.Security.Claims.Claim("MerchantId", merchantId.ToString()),
                    new System.Security.Claims.Claim("ApiKey", apiKey.ToString())
                };

                var identity = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API key authentication");
                return AuthenticateResult.Fail("Authentication failed");
            }
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            _logger.LogWarning("Authentication challenge issued. Scheme: {Scheme}", Scheme.Name);
            Response.Headers["WWW-Authenticate"] = "ApiKey";
            await base.HandleChallengeAsync(properties);
        }
    }
} 