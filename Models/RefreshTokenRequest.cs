using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models
{
    public class RefreshTokenRequest
    {
        public required string RefreshToken { get; set; }
    }
} 