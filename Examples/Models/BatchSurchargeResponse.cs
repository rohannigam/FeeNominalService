using FeeNominalService.Models;

namespace FeeNominalService.Examples.Models
{
    /// <summary>
    /// Response model for batch surcharge calculations
    /// </summary>
    public class BatchSurchargeResponse
    {
        /// <summary>
        /// Unique identifier for the batch
        /// </summary>
        public required string BatchId { get; set; }

        /// <summary>
        /// List of individual surcharge responses
        /// </summary>
        public required List<SurchargeResponse> Transactions { get; set; }

        /// <summary>
        /// Total amount of all transactions
        /// </summary>
        public required decimal TotalAmount { get; set; }

        /// <summary>
        /// Total surcharge amount for all transactions
        /// </summary>
        public required decimal TotalSurchargeAmount { get; set; }

        /// <summary>
        /// Status of the batch operation
        /// </summary>
        public required string Status { get; set; }

        /// <summary>
        /// When the batch was created
        /// </summary>
        public required DateTime CreatedAt { get; set; }
    }
} 