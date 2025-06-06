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
                // For initial API key generation, only validate timestamp and nonce
                if (Request.Path.StartsWithSegments("/api/v1/onboarding/apikey/initial-generate"))
                {
                    if (!Request.Headers.TryGetValue("X-Timestamp", out var initTimestamp) ||
                        !Request.Headers.TryGetValue("X-Nonce", out var initNonce))
                    {
                        _logger.LogWarning("Missing required headers for initial API key generation");
                        return AuthenticateResult.Fail("Missing required headers: X-Timestamp and X-Nonce");
                    }

                    // Read request body
                    string initRequestBody;
                    Request.EnableBuffering();
                    using (var reader = new StreamReader(Request.Body, leaveOpen: true))
                    {
                        initRequestBody = await reader.ReadToEndAsync();
                        Request.Body.Position = 0;
                    }

                    // Validate timestamp and nonce only
                    var isValidInitTimestampAndNonce = _requestSigningService.ValidateTimestampAndNonce(
                        initTimestamp.ToString(),
                        initNonce.ToString());

                    if (!isValidInitTimestampAndNonce)
                    {
                        _logger.LogWarning("Invalid timestamp or nonce for initial API key generation");
                        return AuthenticateResult.Fail("Invalid timestamp or nonce");
                    }

                    // Create claims identity for initial API key generation
                    var initClaims = new[]
                    {
                        new Claim("IsInitialKeyGeneration", "true")
                    };

                    var initIdentity = new ClaimsIdentity(initClaims, Scheme.Name);
                    var initPrincipal = new ClaimsPrincipal(initIdentity);

                    return AuthenticateResult.Success(new AuthenticationTicket(initPrincipal, Scheme.Name));
                }

                // Skip authentication for ping endpoint
                if (Request.Path.StartsWithSegments("/api/v1/ping"))
                {
                    return AuthenticateResult.NoResult();
                }

                // For all other endpoints, require full authentication
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

                // Validate signature
                var isValid = await _requestSigningService.ValidateRequestAsync(
                    merchantId.ToString(),
                    apiKey.ToString(),
                    timestamp.ToString(),
                    nonce.ToString(),
                    requestBody,
                    signature.ToString());

                if (!isValid)
                {
                    _logger.LogWarning("Invalid request signature");
                    return AuthenticateResult.Fail("Invalid request signature");
                }

                // Create claims identity
                var claims = new[]
                {
                    new Claim("MerchantId", merchantId.ToString()),
                    new Claim("ApiKey", apiKey.ToString())
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during API key authentication");
                return AuthenticateResult.Fail("Error during authentication");
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