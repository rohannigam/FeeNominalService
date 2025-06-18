using System;
using System.Linq;
using System.Threading.Tasks;
using FeeNominalService.Models.ApiKey;
using Microsoft.EntityFrameworkCore;

namespace FeeNominalService.Repositories
{
    public class ApiKeyUsageRepository : IApiKeyUsageRepository
    {
        private readonly Data.ApplicationDbContext _context;

        public ApiKeyUsageRepository(Data.ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiKeyUsage?> GetCurrentUsageAsync(Guid apiKeyId, string endpoint, string ipAddress, DateTime windowStart, DateTime windowEnd)
        {
            return await _context.ApiKeyUsages
                .Where(u => u.ApiKeyId == apiKeyId && u.Endpoint == endpoint && u.IpAddress == ipAddress && u.WindowStart == windowStart && u.WindowEnd == windowEnd)
                .FirstOrDefaultAsync();
        }

        public async Task<ApiKeyUsage> CreateUsageAsync(ApiKeyUsage usage)
        {
            _context.ApiKeyUsages.Add(usage);
            await _context.SaveChangesAsync();
            return usage;
        }

        public async Task<ApiKeyUsage> UpdateUsageAsync(ApiKeyUsage usage)
        {
            _context.ApiKeyUsages.Update(usage);
            await _context.SaveChangesAsync();
            return usage;
        }

        public async Task<int> GetTotalRequestCountAsync(Guid apiKeyId, DateTime windowStart, DateTime windowEnd)
        {
            return await _context.ApiKeyUsages
                .Where(u => u.ApiKeyId == apiKeyId && u.WindowStart >= windowStart && u.WindowEnd <= windowEnd)
                .SumAsync(u => u.RequestCount);
        }
    }
} 