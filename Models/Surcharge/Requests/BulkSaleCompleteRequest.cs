using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FeeNominalService.Models.Surcharge.Requests
{
    /// <summary>
    /// Request to complete a batch of surcharge sale transactions (admin/cross-merchant).
    /// </summary>
    /// <remarks>
    /// Each sale item must have either <c>surchargeTransactionId</c> (preferred) or all of <c>providerTransactionId</c>, <c>providerCode</c>, <c>providerType</c>, <c>correlationId</c>.
    /// </remarks>
    /// <example>
    /// {
    ///   "sales": [
    ///     { "surchargeTransactionId": "b1e2c3d4-e5f6-7890-abcd-1234567890ef" },
    ///     { "providerTransactionId": "ip-tx-001", "providerCode": "INTERPAY", "providerType": "INTERPAYMENTS", "correlationId": "sale-123456" }
    ///   ]
    /// }
    /// </example>
    public class BulkSaleCompleteRequest
    {
        [Required]
        public List<BulkSaleItem> Sales { get; set; } = new();
    }

    /// <summary>
    /// Represents a single sale item in a bulk sale complete request.
    /// </summary>
    /// <remarks>
    /// Must provide either <c>surchargeTransactionId</c> or all of <c>providerTransactionId</c>, <c>providerCode</c>, <c>providerType</c>, <c>correlationId</c>.
    /// </remarks>
    public class BulkSaleItem
    {
        /// <summary>
        /// The surcharge transaction ID (auth) to complete. If not provided, must provide providerTransactionId, providerCode, and correlationId.
        /// </summary>
        public Guid? SurchargeTransactionId { get; set; }

        /// <summary>
        /// Provider transaction ID for direct provider-based sale (optional, required if SurchargeTransactionId is not provided)
        /// </summary>
        public string? ProviderTransactionId { get; set; }

        /// <summary>
        /// Provider code for direct provider-based sale (optional, required if SurchargeTransactionId is not provided)
        /// </summary>
        public string? ProviderCode { get; set; }

        /// <summary>
        /// Correlation ID for direct provider-based sale (optional, required if SurchargeTransactionId is not provided)
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Provider type for direct provider-based sale (optional, can be looked up if not provided)
        /// </summary>
        public string? ProviderType { get; set; }

        // Amount field is ignored and not used in processing
    }
} 