using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FeeNominalService.Models;
using FeeNominalService.Examples.Models;

namespace FeeNominalService.Examples
{
    public class MerchantApiClientExample
    {
        public static async Task RunExample()
        {
            // Configuration
            var merchantId = "MERCH123";
            var apiKey = "your-api-key"; // This should be securely stored
            var baseUrl = "https://api.feenominal.com";

            // Create merchant API client
            var merchantClient = new MerchantApiClient(
                baseUrl,
                merchantId,
                apiKey
            );

            try
            {
                // Example 1: Calculate single surcharge
                var surchargeRequest = new SurchargeRequest
                {
                    nicn = "NICN123",
                    processor = "VISA",
                    amount = 100.00m,
                    totalAmount = 100.00m,
                    country = "US",
                    region = "CA",
                    campaign = new List<string> { "SUMMER2024" },
                    data = new List<string> { "ECOMMERCE" },
                    sTxId = "STX123",
                    mTxId = "MTX123",
                    cardToken = "CARD123",
                    entryMethod = EntryMethod.DIPPED,
                    nonSurchargableAmount = 0m
                };

                var surchargeResponse = await merchantClient.CalculateSurchargeAsync(surchargeRequest);
                Console.WriteLine($"Surcharge amount: {surchargeResponse.SurchargeAmount}");

                // Example 2: Calculate batch surcharge
                var batchRequest = new BatchSurchargeRequest
                {
                    Transactions = new List<SurchargeRequest>
                    {
                        new SurchargeRequest
                        {
                            nicn = "NICN124",
                            processor = "VISA",
                            amount = 100.00m,
                            totalAmount = 100.00m,
                            country = "US",
                            region = "CA",
                            campaign = new List<string> { "SUMMER2024" },
                            data = new List<string> { "ECOMMERCE" },
                            sTxId = "STX124",
                            mTxId = "MTX124",
                            cardToken = "CARD124",
                            entryMethod = EntryMethod.DIPPED,
                            nonSurchargableAmount = 0m
                        },
                        new SurchargeRequest
                        {
                            nicn = "NICN125",
                            processor = "MASTERCARD",
                            amount = 200.00m,
                            totalAmount = 200.00m,
                            country = "US",
                            region = "NY",
                            campaign = new List<string> { "SUMMER2024" },
                            data = new List<string> { "ECOMMERCE" },
                            sTxId = "STX125",
                            mTxId = "MTX125",
                            cardToken = "CARD125",
                            entryMethod = EntryMethod.DIPPED,
                            nonSurchargableAmount = 0m
                        }
                    }
                };

                var batchResponse = await merchantClient.CalculateBatchSurchargeAsync(batchRequest);
                foreach (var transaction in batchResponse.Transactions)
                {
                    Console.WriteLine($"Transaction {transaction.TransactionId}: Surcharge amount: {transaction.SurchargeAmount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
            }
        }
    }
} 