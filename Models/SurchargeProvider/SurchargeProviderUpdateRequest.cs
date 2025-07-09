using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Request model for updating a surcharge provider (includes StatusCode)
    /// </summary>
    public class SurchargeProviderUpdateRequest : SurchargeProviderRequest
    {
        /// <summary>
        /// Optional credentials schema for updates. If not provided, existing schema is preserved.
        /// If provided, must be a valid credentials schema structure.
        /// </summary>
        public new object? CredentialsSchema { get; set; }

        [StringLength(50)]
        public string? StatusCode { get; set; }
    }
} 