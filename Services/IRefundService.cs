using FeeNominalService.Models;

namespace FeeNominalService.Services;

public interface IRefundService
{
    Task<string> ProcessRefundAsync(RefundRequest request);
    Task<List<string>> ProcessBatchRefundsAsync(List<RefundRequest> requests);
} 