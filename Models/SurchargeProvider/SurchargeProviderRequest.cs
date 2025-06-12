using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Request model for creating or updating a surcharge provider
    /// </summary>
    public class SurchargeProviderRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(200)]
        public string BaseUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string AuthenticationType { get; set; } = string.Empty;

        [Required]
        public object CredentialsSchema { get; set; } = new();

        [Required]
        [StringLength(50)]
        public string StatusCode { get; set; } = "ACTIVE";
    }
} 