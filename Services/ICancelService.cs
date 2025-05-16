using FeeNominalService.Models;

namespace FeeNominalService.Services;

public interface ICancelService
{
    Task<string> ProcessCancelAsync(CancelRequest request);
    Task<List<string>> ProcessBatchCancellationsAsync(List<CancelRequest> requests);
} 