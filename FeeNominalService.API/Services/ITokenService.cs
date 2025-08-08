using System.Security.Claims;

namespace FeeNominalService.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(string userId, string[]? roles = null);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        Task<string> RefreshAccessTokenAsync(string refreshToken);
    }
} 