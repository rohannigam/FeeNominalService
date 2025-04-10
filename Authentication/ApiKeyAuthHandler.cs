using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Authentication
{
    public class ApiKeyAuthOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "ApiKey";
        public new TimeProvider? TimeProvider { get; set; }
    }

    public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions>
    {
        private readonly ILogger<ApiKeyAuthHandler> _logger;

        public ApiKeyAuthHandler(
            IOptionsMonitor<ApiKeyAuthOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
            _logger = logger.CreateLogger<ApiKeyAuthHandler>();
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeaderValues))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key is missing"));
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (string.IsNullOrEmpty(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key is empty"));
            }

            // TODO: Replace this with actual API key validation logic
            // This is just a sample implementation
            if (providedApiKey == "your-api-key-here")
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "API-User"),
                    new Claim(ClaimTypes.Role, "ApiUser")
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            _logger.LogWarning("Invalid API key provided");
            return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = "ApiKey";
            return base.HandleChallengeAsync(properties);
        }
    }
} 