namespace FeeNominalService.Settings
{
    public class BulkSaleSettings
    {
        public bool UseProviderBulkSale { get; set; } = false;
        public int MaxRequestsPerBulkSale { get; set; } = 5000;
    }
} 