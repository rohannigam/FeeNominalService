using System.Collections.Generic;
using System;

namespace FeeNominalService.Models.Surcharge.Responses
{
    /// <summary>
    /// Response for a bulk sale complete operation (admin/cross-merchant).
    /// </summary>
    /// <remarks>
    /// Includes batch ID, counts, and per-sale results (see SurchargeSaleResponse).
    /// </remarks>
    /// <example>
    /// {
    ///   "batchId": "c9c1668a34b14a7c950b58d4314f6895",
    ///   "totalCount": 2,
    ///   "successCount": 2,
    ///   "failureCount": 0,
    ///   "results": [
    ///     { ...SurchargeSaleResponse... }
    ///   ],
    ///   "processedAt": "2025-05-20T04:52:24.8137133Z"
    /// }
    /// </example>
    public class BulkSaleCompleteResponse
    {
        public string BatchId { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<SurchargeSaleResponse> Results { get; set; } = new();
        public DateTime ProcessedAt { get; set; }
    }
} 