namespace FeeNominalService.Models
{
    public class TransactionFeeRequest
    {
        public decimal amount { get; set; }
        //public decimal totalAmount { get; set; }
        //public string Currency { get; set; }
        public string region { get; set; }
        public string nicn { get; set; }
    }
} 