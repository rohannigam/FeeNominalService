using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FeeNominalService.Services;
using FeeNominalService.Models.Configuration;
using System.Security.Claims;
using FeeNominalService.Utils;
using FeeNominalService.Repositories;
using FeeNominalService.Models.ApiKey;
using System.Collections.Generic;
using System.Text.Json;
using FeeNominalService.Services.AWS;
using Microsoft.AspNetCore.Authorization;

namespace FeeNominalService.Authentication
{
    public class ApiKeyAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IRequestSigningService _requestSigningService;
        private readonly ILogger<ApiKeyAuthHandler> _logger;
        private readonly ApiKeyConfiguration _apiKeyConfig;
        private readonly IApiKeyService _apiKeyService;
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly IApiKeyUsageRepository _apiKeyUsageRepository;
        private readonly IAwsSecretsManagerService _secretsManager;
        private readonly IMerchantRepository _merchantRepository;

        public ApiKeyAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IRequestSigningService requestSigningService,
            IOptions<ApiKeyConfiguration> apiKeyConfig,
            IApiKeyService apiKeyService,
            IApiKeyRepository apiKeyRepository,
            IApiKeyUsageRepository apiKeyUsageRepository,
            IAwsSecretsManagerService secretsManager,
            IMerchantRepository merchantRepository)
            : base(options, logger, encoder)
        {
            _requestSigningService = requestSigningService;
            _logger = logger.CreateLogger<ApiKeyAuthHandler>();
            _apiKeyConfig = apiKeyConfig.Value;
            _apiKeyService = apiKeyService;
            _apiKeyRepository = apiKeyRepository;
            _apiKeyUsageRepository = apiKeyUsageRepository;
            _secretsManager = secretsManager;
            _merchantRepository = merchantRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                _logger.LogDebug("Starting API key authentication for path: {Path}", Request.Path);

                // Skip authentication for AllowAnonymous endpoints
                if (Context?.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
                {
                    _logger.LogDebug("Skipping API key authentication for AllowAnonymous endpoint: {Path}", Request.Path);
                    return AuthenticateResult.NoResult();
                }

                // Skip authentication for admin API key generation endpoint
                if (Request.Path.StartsWithSegments("/api/v1/admin/apiKey/generate"))
                {
                    _logger.LogDebug("Skipping authentication for admin API key generation endpoint");
                    return AuthenticateResult.NoResult();
                }

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

                // Debug: Log all headers received
                _logger.LogInformation("=== AUTHENTICATION HEADERS DEBUG ===");
                foreach (var header in Request.Headers)
                {
                    _logger.LogInformation("Header: {HeaderName} = {HeaderValue}", header.Key, header.Value);
                }
                _logger.LogInformation("=== END AUTHENTICATION HEADERS DEBUG ===");

                // Validate required headers (except X-Merchant-ID for admin keys)
                var (isValidApiKey, apiKey, apiKeyError) = HeaderValidationHelper.ValidateRequiredHeader(Request.Headers, "X-API-Key");
                var (isValidTimestamp, timestamp, timestampError) = HeaderValidationHelper.ValidateRequiredHeader(Request.Headers, "X-Timestamp");
                var (isValidNonce, nonce, nonceError) = HeaderValidationHelper.ValidateRequiredHeader(Request.Headers, "X-Nonce");
                var (isValidSignature, signature, signatureError) = HeaderValidationHelper.ValidateRequiredHeader(Request.Headers, "X-Signature");

                // Get merchant ID header (optional for admin keys)
                var (isValidMerchantId, merchantId, merchantIdError) = HeaderValidationHelper.ValidateRequiredHeader(Request.Headers, "X-Merchant-ID");

                _logger.LogInformation("=== HEADER VALIDATION DEBUG ===");
                _logger.LogInformation("ApiKey: Valid={IsValid}, Value={ApiKey}, Error={Error}", isValidApiKey, apiKey, apiKeyError);
                _logger.LogInformation("Timestamp: Valid={IsValid}, Value={Timestamp}, Error={Error}", isValidTimestamp, timestamp, timestampError);
                _logger.LogInformation("Nonce: Valid={IsValid}, Value={Nonce}, Error={Error}", isValidNonce, nonce, nonceError);
                _logger.LogInformation("Signature: Valid={IsValid}, Value={Signature}, Error={Error}", isValidSignature, signature, signatureError);
                _logger.LogInformation("MerchantId: Valid={IsValid}, Value={MerchantId}, Error={Error}", isValidMerchantId, merchantId, merchantIdError);
                _logger.LogInformation("=== END HEADER VALIDATION DEBUG ===");

                if (!isValidApiKey || !isValidTimestamp || !isValidNonce || !isValidSignature)
                {
                    var errors = new[]
                    {
                        apiKeyError,
                        timestampError,
                        nonceError,
                        signatureError
                    }.Where(e => !string.IsNullOrEmpty(e));

                    _logger.LogWarning("Missing or invalid headers: {Errors}", string.Join(", ", errors));
                    return AuthenticateResult.Fail("Missing or invalid headers");
                }

                // Check if this is an admin API key
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("API key is null or empty");
                    return AuthenticateResult.Fail("API key is null or empty");
                }
                
                var apiKeyEntity = await _apiKeyRepository.GetByKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    _logger.LogWarning("API key {ApiKey} not found", apiKey);
                    return AuthenticateResult.Fail("API key not found");
                }

                var isAdminKey = apiKeyEntity.IsAdmin || apiKeyEntity.MerchantId == null;

                // For admin keys, merchant ID is optional (can be empty/null)
                // For merchant keys, merchant ID is required
                if (!isAdminKey && !isValidMerchantId)
                {
                    _logger.LogWarning("Merchant ID header required for non-admin API keys: {Error}", merchantIdError);
                    return AuthenticateResult.Fail("Merchant ID header required for non-admin API keys");
                }

                // Ensure merchantId is never null
                var finalMerchantId = string.Empty;
                if (isAdminKey)
                {
                    // For admin keys, use empty string if no merchant ID provided
                    finalMerchantId = isValidMerchantId ? (merchantId ?? string.Empty) : string.Empty;
                }
                else
                {
                    // For merchant keys, merchant ID is required and should be valid
                    finalMerchantId = merchantId ?? string.Empty;
                }

                // Validate timestamp
                if (!DateTime.TryParse(timestamp, out var requestTime))
                {
                    _logger.LogWarning("Invalid timestamp format: {Timestamp}", timestamp);
                    return AuthenticateResult.Fail("Invalid timestamp format");
                }

                // Convert to UTC if not already
                requestTime = requestTime.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(requestTime, DateTimeKind.Utc)
                    : requestTime.ToUniversalTime();

                var currentTime = DateTime.UtcNow;
                var timeDifference = Math.Abs((currentTime - requestTime).TotalMinutes);

                _logger.LogDebug("Request timestamp: {RequestTime} UTC, Current time: {CurrentTime} UTC, Difference: {Difference} minutes",
                    requestTime, currentTime, timeDifference);

                if (timeDifference > 5) // 5 minutes tolerance
                {
                    _logger.LogWarning("Request timestamp is too old: {RequestTime} UTC, Current time: {CurrentTime} UTC, Difference: {Difference} minutes",
                        requestTime, currentTime, timeDifference);
                    return AuthenticateResult.Fail("Request timestamp is too old");
                }

                // Validate signature
                if (string.IsNullOrEmpty(apiKey) || 
                    string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(nonce) || 
                    string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Missing required authentication parameters");
                    return AuthenticateResult.Fail("Missing required authentication parameters");
                }

                var isValid = await _apiKeyService.ValidateApiKeyAsync(finalMerchantId, apiKey, timestamp, nonce, signature, isAdminKey ? (apiKeyEntity.ServiceName ?? string.Empty) : string.Empty);
                if (!isValid)
                {
                    _logger.LogWarning("Invalid signature for {KeyType} {Identifier}", 
                        isAdminKey ? "admin key" : "merchant", 
                        isAdminKey ? apiKey : finalMerchantId);
                    return AuthenticateResult.Fail("Invalid signature");
                }

                _logger.LogDebug("API key validation successful for {KeyType} {Identifier}", 
                    isAdminKey ? "admin key" : "merchant", 
                    isAdminKey ? apiKey : finalMerchantId);

                // Update last_used_at timestamp
                try 
                {
                    if (apiKeyEntity != null)
                    {
                        apiKeyEntity.LastUsedAt = DateTime.UtcNow;
                        await _apiKeyRepository.UpdateAsync(apiKeyEntity);
                        _logger.LogDebug("Updated last_used_at for API key {ApiKey}", apiKey);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail authentication if we can't update last_used_at
                    _logger.LogError(ex, "Failed to update last_used_at for API key {ApiKey}", apiKey);
                }

                // Get API key info to create claims
                var apiKeyInfo = await _apiKeyService.GetApiKeyInfoAsync(apiKey);
                if (apiKeyInfo == null)
                {
                    _logger.LogWarning("API key info not found for key {ApiKey}", apiKey);
                    return AuthenticateResult.Fail("API key not found");
                }

                _logger.LogDebug("Retrieved API key info - Status: {Status}, AllowedEndpoints: {Endpoints}", 
                    apiKeyInfo.Status, string.Join(", ", apiKeyInfo.AllowedEndpoints));

                // Create claims
                var claims = new[]
                {
                    new Claim("MerchantId", isAdminKey ? string.Empty : finalMerchantId),
                    new Claim("ApiKey", apiKey),
                    new Claim("AllowedEndpoints", string.Join(",", apiKeyInfo.AllowedEndpoints)),
                    new Claim("Scope", apiKeyInfo.Scope ?? "merchant"),
                    new Claim("IsAdmin", apiKeyInfo.IsAdmin ? "true" : "false")
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);

                _logger.LogDebug("Created claims principal with claims: {Claims}", 
                    string.Join(", ", claims.Select(c => $"{c.Type}: {c.Value}")));

                // Usage count tracking
                try
                {
                    if (apiKeyEntity != null)
                    {
                        var windowStart = DateTime.UtcNow.AddMinutes(-_apiKeyConfig.RequestTimeWindowMinutes);
                        var windowEnd = DateTime.UtcNow;
                        var currentUsage = await _apiKeyUsageRepository.GetCurrentUsageAsync(apiKeyEntity.Id, Request.Path, Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", windowStart, windowEnd);
                        if (currentUsage != null)
                        {
                            currentUsage.RequestCount++;
                            await _apiKeyUsageRepository.UpdateUsageAsync(currentUsage);
                        }
                        else
                        {
                            var newUsage = new ApiKeyUsage
                            {
                                ApiKeyId = apiKeyEntity.Id,
                                Endpoint = Request.Path,
                                IpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                                RequestCount = 1,
                                WindowStart = windowStart,
                                WindowEnd = windowEnd,
                                Timestamp = DateTime.UtcNow,
                                HttpMethod = Request.Method,
                                StatusCode = 200,
                                ResponseTimeMs = 0
                            };
                            await _apiKeyUsageRepository.CreateUsageAsync(newUsage);
                        }

                        var totalRequestCount = await _apiKeyUsageRepository.GetTotalRequestCountAsync(apiKeyEntity.Id, windowStart, windowEnd);
                        if (totalRequestCount > apiKeyEntity.RateLimit)
                        {
                            _logger.LogWarning("Rate limit exceeded for API key {ApiKey}. Total requests: {TotalRequests}, Rate limit: {RateLimit}", apiKey, totalRequestCount, apiKeyEntity.RateLimit);
                            return AuthenticateResult.Fail("Rate limit exceeded");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail authentication if we can't update usage count
                    _logger.LogError(ex, "Failed to update usage count for API key {ApiKey}", apiKey);
                }

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