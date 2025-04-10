using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using FeeNominalService.Models;
using FeeNominalService.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FeeNominalService.Controllers.V1
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [ApiVersion("1.0")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly ITokenBlacklistService _blacklistService;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            ITokenService tokenService,
            ITokenBlacklistService blacklistService,
            IAuditLogService auditLogService,
            ILogger<AuthController> logger)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // For demo purposes, accept any login
                // In a real application, validate credentials against a database
                var userId = "1"; // Hardcoded user ID for demo
                var roles = new[] { "User" }; // Hardcoded roles for demo

                var accessToken = _tokenService.GenerateAccessToken(userId, roles);
                var refreshToken = _tokenService.GenerateRefreshToken();

                await LogSuccessfulLogin(request.Email, userId);

                return Ok(new TokenResponse
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = 3600 // 1 hour
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {Email}", request.Email);
                await LogFailedAttempt(request.Email, ex.Message);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("refresh")]
        [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    _logger.LogWarning("Refresh token is empty");
                    return BadRequest(new { message = "Refresh token is required" });
                }

                // Check if the refresh token is blacklisted
                if (await _blacklistService.IsTokenBlacklistedAsync(request.RefreshToken))
                {
                    _logger.LogWarning("Attempt to use blacklisted refresh token");
                    return Unauthorized(new { message = "Token has been revoked" });
                }

                // For demo purposes, we'll accept any non-blacklisted refresh token
                // In a real application, you would validate this against a stored refresh token
                var userId = "1"; // Hardcoded user ID for demo
                var roles = new[] { "User" }; // Hardcoded roles for demo

                // Generate new tokens
                var newAccessToken = _tokenService.GenerateAccessToken(userId, roles);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                // Blacklist the old refresh token
                await _blacklistService.BlacklistTokenAsync(request.RefreshToken, userId, DateTime.UtcNow.AddDays(7));

                _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);

                return Ok(new TokenResponse
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresIn = 3600 // 1 hour
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, new { message = "An error occurred while refreshing the token" });
            }
        }

        [Authorize]
        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                _logger.LogInformation("Token revocation requested by user: {UserId}", userId);

                if (string.IsNullOrEmpty(request.RefreshToken))
                {
                    _logger.LogWarning("Empty refresh token provided");
                    return BadRequest(new { message = "Refresh token is required" });
                }

                // Blacklist the refresh token directly
                _logger.LogInformation("Blacklisting refresh token");
                await _blacklistService.BlacklistTokenAsync(request.RefreshToken, userId, DateTime.UtcNow.AddDays(7));

                // Also blacklist the current access token
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                {
                    var accessToken = authHeader.Substring("Bearer ".Length);
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(accessToken);
                        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                        
                        if (!string.IsNullOrEmpty(jti))
                        {
                            _logger.LogInformation("Blacklisting access token with JTI: {Jti}", jti);
                            await _blacklistService.BlacklistTokenAsync(jti, userId, DateTime.UtcNow.AddDays(7));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to blacklist access token");
                        // Continue with the response even if access token blacklisting fails
                    }
                }

                // Verify the refresh token is blacklisted
                var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(request.RefreshToken);
                _logger.LogInformation("Refresh token blacklist status: {IsBlacklisted}", isBlacklisted);

                // Log the revocation
                await _auditLogService.LogAuthenticationAttemptAsync(new AuthenticationAttempt
                {
                    UserId = userId,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    Timestamp = DateTime.UtcNow,
                    Success = true,
                    AuthenticationType = "Token Revocation",
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                });

                return Ok(new { message = "Token revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking token");
                return StatusCode(500, new { message = "An error occurred while revoking the token" });
            }
        }

        [Authorize]
        [HttpGet("validate")]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
                
                // Get the current token's JTI
                var authHeader = HttpContext.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Invalid Authorization header format");
                    return Unauthorized(new { message = "Invalid Authorization header format" });
                }
                
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                
                // Check if the token is blacklisted
                if (!string.IsNullOrEmpty(jti) && await _blacklistService.IsTokenBlacklistedAsync(jti))
                {
                    _logger.LogWarning("Token {Jti} is blacklisted", jti);
                    return Unauthorized(new { message = "Token has been revoked" });
                }
                
                return Ok(new
                {
                    IsValid = true,
                    Email = userId,
                    Roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return Unauthorized(new { message = "Invalid token" });
            }
        }

        private async Task LogSuccessfulAttempt(string userId, string authType)
        {
            await _auditLogService.LogAuthenticationAttemptAsync(new AuthenticationAttempt
            {
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                Success = true,
                AuthenticationType = authType,
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            });
        }

        private async Task LogFailedAttempt(string userId, string reason)
        {
            await _auditLogService.LogAuthenticationAttemptAsync(new AuthenticationAttempt
            {
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                Success = false,
                AuthenticationType = "Unknown",
                FailureReason = reason,
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            });
        }

        private async Task LogSuccessfulLogin(string email, string userId)
        {
            await _auditLogService.LogAuthenticationAttemptAsync(new AuthenticationAttempt
            {
                UserId = userId,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                Success = true,
                AuthenticationType = "Login",
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
            });
        }
    }
} 