using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeeNominalService.Models.Merchant;

namespace FeeNominalService.Repositories;

public interface IMerchantAuditTrailRepository
{
    Task<MerchantAuditTrail> CreateAsync(MerchantAuditTrail auditTrail);
    Task<IEnumerable<MerchantAuditTrail>> GetByMerchantIdAsync(Guid merchantId);
    Task<IEnumerable<MerchantAuditTrail>> GetByMerchantIdAndActionAsync(Guid merchantId, string action);
} 