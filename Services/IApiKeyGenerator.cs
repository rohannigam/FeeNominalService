using System;

namespace FeeNominalService.Services
{
    public interface IApiKeyGenerator
    {
        (string apiKey, string secret) GenerateApiKeyAndSecret();
    }
} 