using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Data;

namespace FeeNominalService.Repositories
{
    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly ApplicationDbContext _context;

        public ApiKeyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiKey?> GetByKeyAsync(string key)
        {
            return await _context.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == key);
        }

        public async Task<List<ApiKey>> GetByMerchantIdAsync(Guid merchantId)
        {
            return await _context.ApiKeys
                .Where(k => k.MerchantId == merchantId)
                .OrderByDescending(k => k.CreatedAt)
                .ToListAsync();
        }

        public async Task<ApiKey> CreateAsync(ApiKey apiKey)
        {
            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();
            return apiKey;
        }

        public async Task<ApiKey> UpdateAsync(ApiKey apiKey)
        {
            _context.ApiKeys.Update(apiKey);
            await _context.SaveChangesAsync();
            return apiKey;
        }
    }
} 