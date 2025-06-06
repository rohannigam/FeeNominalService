using System;

namespace FeeNominalService.Models.Merchant.Responses
{
    public class MerchantResponse
    {
        public Guid MerchantId { get; set; }
        public string ExternalMerchantId { get; set; } = string.Empty;
        public Guid? ExternalMerchantGuid { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
} 