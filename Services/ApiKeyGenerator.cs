using System;
using System.Security.Cryptography;

namespace FeeNominalService.Services
{
    public class ApiKeyGenerator : IApiKeyGenerator
    {
        public (string apiKey, string secret) GenerateApiKeyAndSecret()
        {
            var apiKey = GenerateSecureRandomString(32);
            var secret = GenerateSecureRandomString(64);
            return (apiKey, secret);
        }

        private string GenerateSecureRandomString(int length)
        {
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .Substring(0, length);
        }
    }
} 