using FeeNominalService.Models;
using FeeNominalService.Models.Surcharge.Requests;
using FeeNominalService.Models.Surcharge.Responses;

namespace FeeNominalService.Services;

public interface ISurchargeTransactionService
{
    Task<SurchargeAuthResponse> ProcessAuthAsync(SurchargeAuthRequest request, Guid merchantId, string actor);
    Task<SurchargeSaleResponse> ProcessSaleAsync(SurchargeSaleRequest request, Guid merchantId, string actor);
    Task<SurchargeRefundResponse> ProcessRefundAsync(SurchargeRefundRequest request, Guid merchantId, string actor);
    Task<SurchargeCancelResponse> ProcessCancelAsync(SurchargeCancelRequest request, Guid merchantId, string actor);
    Task<SurchargeTransaction?> GetTransactionByIdAsync(Guid id, Guid merchantId);
    Task<(List<SurchargeTransaction> Transactions, int TotalCount)> GetTransactionsByMerchantAsync(
        Guid merchantId, int page, int pageSize, SurchargeOperationType? operationType, SurchargeTransactionStatus? status);
    Task<BulkSaleCompleteResponse> ProcessBulkSaleCompleteAsync(BulkSaleCompleteRequest request);
} 