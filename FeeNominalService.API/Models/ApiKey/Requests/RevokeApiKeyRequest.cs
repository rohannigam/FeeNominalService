using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.ApiKey.Requests
{
    /// <summary>
    /// Request model for revoking an API key
    /// </summary>
    public class RevokeApiKeyRequest
    {
        /// <summary>
        /// The merchant ID associated with the API key to revoke
        /// </summary>
        [Required]
        public string MerchantId { get; set; } = string.Empty;

        /// <summary>
        /// The API key to revoke
        /// </summary>
        [Required]
        public string ApiKey { get; set; } = string.Empty;
    }
} 