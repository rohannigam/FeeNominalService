using System.ComponentModel.DataAnnotations;
using FeeNominalService.Settings;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Models.SurchargeProvider
{
    /// <summary>
    /// Request model for updating a surcharge provider (includes StatusCode)
    /// </summary>
    public class SurchargeProviderUpdateRequest : SurchargeProviderRequest
    {
        [StringLength(50)]
        public string? StatusCode { get; set; }

        /// <summary>
        /// Override to handle optional credentials schema for updates
        /// </summary>
        public override bool ValidateCredentialsSchema(out List<string> errors, SurchargeProviderValidationSettings? settings = null)
        {
            errors = new List<string>();

            // Debug: Log what we're receiving
            System.Diagnostics.Debug.WriteLine($"SurchargeProviderUpdateRequest.ValidateCredentialsSchema called. CredentialsSchema is null: {CredentialsSchema == null}");
            System.Diagnostics.Debug.WriteLine($"SurchargeProviderUpdateRequest.ValidateCredentialsSchema - CredentialsSchema type: {CredentialsSchema?.GetType().Name ?? "null"}");

            // For updates, credentials schema is optional
            if (CredentialsSchema == null)
            {
                System.Diagnostics.Debug.WriteLine("CredentialsSchema is null, returning true (valid for updates)");
                return true; // Valid - schema is optional in updates
            }

            System.Diagnostics.Debug.WriteLine("CredentialsSchema is provided, calling base validation");
            // If provided, validate using base class logic
            return base.ValidateCredentialsSchema(out errors, settings);
        }
    }
} 