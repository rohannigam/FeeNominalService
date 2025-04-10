using FeeNominalService.Models;

namespace FeeNominalService.Services;

public interface ISaleService
{
    Task<string> ProcessSaleAsync(SaleRequest request);
    Task<List<string>> ProcessBatchSalesAsync(List<SaleRequest> requests);
} 