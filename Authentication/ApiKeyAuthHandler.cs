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
                _logger.LogDebug("Starting API key authentication for path: {Path}", Request.Path);

                // For initial API key generation, only validate timestamp and nonce
                if (Request.Path.StartsWithSegments("/api/v1/onboarding/apikey/initial-generate"))
                {
                    _logger.LogDebug("Processing initial API key generation request");
                    if (!Request.Headers.TryGetValue("X-Timestamp", out var initTimestamp) ||
                        !Request.Headers.TryGetValue("X-Nonce", out var initNonce))
                    {
                        _logger.LogWarning("Missing required headers for initial API key generation");
                        return AuthenticateResult.Fail("Missing required headers: X-Timestamp and X-Nonce");
                    }

                    _logger.LogDebug("Initial generation headers - Timestamp: {Timestamp}, Nonce: {Nonce}", 
                        initTimestamp, initNonce);

                    // Read request body
                    string initRequestBody;
                    Request.EnableBuffering();
                    using (var reader = new StreamReader(Request.Body, leaveOpen: true))
                    {
                        initRequestBody = await reader.ReadToEndAsync();
                        Request.Body.Position = 0;
                    }

                    _logger.LogDebug("Initial generation request body: {RequestBody}", initRequestBody);

                    // Validate timestamp and nonce only
                    var isValidInitTimestampAndNonce = _requestSigningService.ValidateTimestampAndNonce(
                        initTimestamp.ToString(),
                        initNonce.ToString());

                    if (!isValidInitTimestampAndNonce)
                    {
                        _logger.LogWarning("Invalid timestamp or nonce for initial API key generation");
                        throw new UnauthorizedAccessException("Invalid timestamp or nonce");
                    }

                    _logger.LogDebug("Initial generation timestamp and nonce validation successful");

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
                    _logger.LogDebug("Skipping authentication for ping endpoint");
                    return AuthenticateResult.NoResult();
                }

                // Get required headers
                if (!Request.Headers.TryGetValue("X-Merchant-ID", out var merchantIdValues) ||
                    !Request.Headers.TryGetValue("X-API-Key", out var apiKeyValues) ||
                    !Request.Headers.TryGetValue("X-Timestamp", out var timestampValues) ||
                    !Request.Headers.TryGetValue("X-Nonce", out var nonceValues) ||
                    !Request.Headers.TryGetValue("X-Signature", out var signatureValues))
                {
                    _logger.LogWarning("Missing required headers");
                    return AuthenticateResult.Fail("Missing required headers");
                }

                var merchantId = merchantIdValues.ToString();
                var apiKey = apiKeyValues.ToString();
                var timestamp = timestampValues.ToString();
                var nonce = nonceValues.ToString();
                var receivedSignature = signatureValues.ToString();

                _logger.LogDebug("Authentication headers - MerchantId: {MerchantId}, ApiKey: {ApiKey}, Timestamp: {Timestamp}, Nonce: {Nonce}, Signature: {Signature}",
                    merchantId, apiKey, timestamp, nonce, receivedSignature);

                // Generate expected signature using only specific fields
                var expectedSignature = await _requestSigningService.GenerateSignatureAsync(
                    merchantId,
                    apiKey,
                    timestamp,
                    nonce,
                    string.Empty); // No longer using request body

                _logger.LogDebug("Signature validation details:\nMerchantId: {MerchantId}\nApiKey: {ApiKey}\nTimestamp: {Timestamp}\nNonce: {Nonce}\nReceived signature: {ReceivedSignature}\nExpected signature: {ExpectedSignature}\nMatch: {Match}",
                    merchantId, apiKey, timestamp, nonce, receivedSignature, expectedSignature, receivedSignature == expectedSignature);

                if (receivedSignature != expectedSignature)
                {
                    _logger.LogWarning("Invalid request signature");
                    return AuthenticateResult.Fail("Invalid request signature");
                }

                // Create claims identity
                var claims = new[]
                {
                    new Claim("MerchantId", merchantId),
                    new Claim("ApiKey", apiKey)
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