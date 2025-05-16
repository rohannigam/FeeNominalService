using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Data;

namespace FeeNominalService.Repositories
{
    public class MerchantRepository : IMerchantRepository
    {
        private readonly ApplicationDbContext _context;

        public MerchantRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Merchant?> GetByExternalIdAsync(string externalId)
        {
            return await _context.Merchants
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.ExternalId == externalId);
        }

        public async Task<Merchant> GetByIdAsync(Guid id)
        {
            var merchant = await _context.Merchants
                .Include(m => m.Status)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (merchant == null)
            {
                throw new KeyNotFoundException($"Merchant with ID {id} not found");
            }

            return merchant;
        }

        public async Task<Merchant> CreateAsync(Merchant merchant)
        {
            _context.Merchants.Add(merchant);
            await _context.SaveChangesAsync();
            return merchant;
        }

        public async Task<Merchant> UpdateAsync(Merchant merchant)
        {
            _context.Merchants.Update(merchant);
            await _context.SaveChangesAsync();
            return merchant;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var merchant = await _context.Merchants.FindAsync(id);
            if (merchant == null)
            {
                return false;
            }

            _context.Merchants.Remove(merchant);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 