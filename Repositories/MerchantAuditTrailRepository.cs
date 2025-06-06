using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Data;
using Microsoft.Extensions.Logging;

namespace FeeNominalService.Repositories;

public class MerchantAuditTrailRepository : IMerchantAuditTrailRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MerchantAuditTrailRepository> _logger;

    public MerchantAuditTrailRepository(ApplicationDbContext context, ILogger<MerchantAuditTrailRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MerchantAuditTrail> CreateAsync(MerchantAuditTrail auditTrail)
    {
        try
        {
            _context.MerchantAuditTrail.Add(auditTrail);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created audit trail entry for merchant {MerchantId}", auditTrail.MerchantId);
            return auditTrail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit trail entry for merchant {MerchantId}", auditTrail.MerchantId);
            throw;
        }
    }

    public async Task<IEnumerable<MerchantAuditTrail>> GetByMerchantIdAsync(Guid merchantId)
    {
        try
        {
            return await _context.MerchantAuditTrail
                .Where(a => a.MerchantId == merchantId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for merchant {MerchantId}", merchantId);
            throw;
        }
    }

    public async Task<IEnumerable<MerchantAuditTrail>> GetByMerchantIdAndActionAsync(Guid merchantId, string action)
    {
        try
        {
            return await _context.MerchantAuditTrail
                .Where(a => a.MerchantId == merchantId && a.Action == action)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for merchant {MerchantId} with action {Action}", merchantId, action);
            throw;
        }
    }
} 