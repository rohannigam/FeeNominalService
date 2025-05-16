using System;
using System.Threading.Tasks;
using FeeNominalService.Models.Merchant;

namespace FeeNominalService.Repositories;

public interface IMerchantRepository
{
    Task<Merchant?> GetByExternalIdAsync(string externalId);
    Task<Merchant> GetByIdAsync(Guid id);
    Task<Merchant> CreateAsync(Merchant merchant);
    Task<Merchant> UpdateAsync(Merchant merchant);
    Task<bool> DeleteAsync(Guid id);
} 