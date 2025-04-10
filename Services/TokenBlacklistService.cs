using System.Security.Cryptography;
using System.Text;
using FeeNominalService.Models;

namespace FeeNominalService.Services
{
    public interface ITokenBlacklistService
    {
        Task BlacklistTokenAsync(string tokenId, string userId, DateTime expiresAt);
        Task<bool> IsTokenBlacklistedAsync(string tokenId);
        Task RevokeAllUserTokensAsync(string userId);
    }

    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly ILogger<TokenBlacklistService> _logger;
        private static readonly Dictionary<string, BlacklistedToken> _blacklist = new();
        private static readonly object _lock = new();

        public TokenBlacklistService(ILogger<TokenBlacklistService> logger)
        {
            _logger = logger;
            _logger.LogInformation("TokenBlacklistService initialized with {Count} blacklisted tokens", _blacklist.Count);
        }

        public Task BlacklistTokenAsync(string tokenId, string userId, DateTime expiresAt)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                throw new ArgumentException("Token ID cannot be empty", nameof(tokenId));
            }

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            if (expiresAt <= DateTime.UtcNow)
            {
                throw new ArgumentException("Expiration time must be in the future", nameof(expiresAt));
            }
            
            lock (_lock)
            {
                _blacklist[tokenId] = new BlacklistedToken
                {
                    UserId = userId,
                    ExpiresAt = expiresAt,
                    BlacklistedAt = DateTime.UtcNow
                };

                // Clean up expired tokens
                CleanupExpiredTokens();
                
                _logger.LogInformation("Token {TokenId} blacklisted for user {UserId} until {ExpiresAt}. Current blacklist count: {Count}", 
                    tokenId, userId, expiresAt, _blacklist.Count);
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsTokenBlacklistedAsync(string tokenId)
        {
            if (string.IsNullOrEmpty(tokenId))
            {
                _logger.LogWarning("IsTokenBlacklistedAsync called with empty tokenId");
                return Task.FromResult(false);
            }
            
            lock (_lock)
            {
                if (_blacklist.TryGetValue(tokenId, out var blacklistedToken))
                {
                    if (blacklistedToken.ExpiresAt < DateTime.UtcNow)
                    {
                        _blacklist.Remove(tokenId);
                        _logger.LogInformation("Token {TokenId} expired and removed from blacklist. Current blacklist count: {Count}", 
                            tokenId, _blacklist.Count);
                        return Task.FromResult(false);
                    }
                    _logger.LogInformation("Token {TokenId} found in blacklist. Expires at: {ExpiresAt}", 
                        tokenId, blacklistedToken.ExpiresAt);
                    return Task.FromResult(true);
                }
                
                _logger.LogDebug("Token {TokenId} not found in blacklist", tokenId);
            }

            return Task.FromResult(false);
        }

        public Task RevokeAllUserTokensAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }

            lock (_lock)
            {
                var tokensToRemove = _blacklist
                    .Where(kvp => kvp.Value.UserId == userId)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var token in tokensToRemove)
                {
                    _blacklist.Remove(token);
                }

                _logger.LogInformation("Revoked {Count} tokens for user {UserId}. Current blacklist count: {Count}", 
                    tokensToRemove.Count, userId, _blacklist.Count);
            }

            return Task.CompletedTask;
        }

        private void CleanupExpiredTokens()
        {
            var expiredTokens = _blacklist
                .Where(kvp => kvp.Value.ExpiresAt < DateTime.UtcNow)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var token in expiredTokens)
            {
                _blacklist.Remove(token);
            }

            if (expiredTokens.Any())
            {
                _logger.LogInformation("Cleaned up {Count} expired tokens from blacklist. Current blacklist count: {Count}", 
                    expiredTokens.Count, _blacklist.Count);
            }
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    public class BlacklistedToken
    {
        public required string UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime BlacklistedAt { get; set; }
    }
} 