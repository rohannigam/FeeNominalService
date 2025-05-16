using System;
using System.Threading.Tasks;
using FeeNominalService.Models.Merchant;

namespace FeeNominalService.Services
{
    public interface IMerchantService
    {
        Task<Merchant> CreateMerchantAsync(Merchant merchant);
        Task<Merchant?> GetMerchantAsync(Guid id);
        Task<Merchant> UpdateMerchantStatusAsync(Guid id, Guid statusId);
    }
} 