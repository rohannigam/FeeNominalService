using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models
{
    public class ApiKeyRequest
    {
        public required string ApiKey { get; set; }
    }
} 