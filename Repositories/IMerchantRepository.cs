using System;
using System.Threading.Tasks;
using FeeNominalService.Models.Merchant;

namespace FeeNominalService.Repositories;

public interface IMerchantRepository
{
    Task<Merchant?> GetByExternalIdAsync(string externalMerchantId);
    Task<Merchant> GetByIdAsync(Guid merchantId);
    Task<Merchant> CreateAsync(Merchant merchant);
    Task<Merchant> UpdateAsync(Merchant merchant);
    Task<bool> DeleteAsync(Guid merchantId);
    Task<Merchant?> GetByExternalMerchantIdAsync(string externalMerchantId);
    Task<Merchant?> GetByExternalMerchantGuidAsync(Guid externalMerchantGuid);
    Task<bool> ExistsByExternalMerchantIdAsync(string externalMerchantId);
    Task<bool> ExistsByExternalMerchantGuidAsync(Guid externalMerchantGuid);
    Task<MerchantStatus?> GetMerchantStatusAsync(int statusId);
    Task<bool> IsValidStatusIdAsync(int statusId);
} 