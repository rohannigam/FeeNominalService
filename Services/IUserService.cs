using FeeNominalService.Models;

namespace FeeNominalService.Services
{
    public interface IUserService
    {
        Task<(bool success, string[] roles)> ValidateUserAsync(string email, string password);
        Task<(bool success, string[] roles)> ValidateApiKeyAsync(string apiKey);
    }
} 