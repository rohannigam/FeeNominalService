using FeeNominalService.Data;
using FeeNominalService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FeeNominalService.Repositories;

/// <summary>
/// Repository implementation for managing surcharge transactions
/// </summary>
public class SurchargeTransactionRepository : ISurchargeTransactionRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SurchargeTransactionRepository> _logger;

    public SurchargeTransactionRepository(ApplicationDbContext context, ILogger<SurchargeTransactionRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SurchargeTransaction> CreateAsync(SurchargeTransaction transaction)
    {
        try
        {
            _logger.LogDebug("Creating surcharge transaction for merchant {MerchantId}, operation {OperationType}", 
                transaction.MerchantId, transaction.OperationType);

            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;

            _context.SurchargeTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created surcharge transaction {TransactionId} for merchant {MerchantId}", 
                transaction.Id, transaction.MerchantId);

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating surcharge transaction for merchant {MerchantId}", transaction.MerchantId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transaction by ID {TransactionId}", id);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetByIdForMerchantAsync(Guid id, Guid merchantId)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .FirstOrDefaultAsync(t => t.Id == id && t.MerchantId == merchantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transaction by ID {TransactionId} for merchant {MerchantId}", id, merchantId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetByCorrelationIdAsync(string correlationId)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .FirstOrDefaultAsync(t => t.CorrelationId == correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transaction by correlation ID {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<(List<SurchargeTransaction> Transactions, int TotalCount)> GetByMerchantIdAsync(
        Guid merchantId, 
        int page = 1, 
        int pageSize = 20,
        SurchargeOperationType? operationType = null,
        SurchargeTransactionStatus? status = null)
    {
        try
        {
            var query = _context.SurchargeTransactions
                .Include(t => t.ProviderConfig)
                .Where(t => t.MerchantId == merchantId);

            if (operationType.HasValue)
            {
                query = query.Where(t => t.OperationType == operationType.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (transactions, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transactions for merchant {MerchantId}", merchantId);
            throw;
        }
    }

    public async Task<IEnumerable<SurchargeTransaction>> GetByMerchantIdAndStatusAsync(string merchantId, SurchargeTransactionStatus status)
    {
        try
        {
            // Convert string merchantId to Guid for database comparison
            if (!Guid.TryParse(merchantId, out Guid merchantGuid))
            {
                _logger.LogWarning("Invalid merchant ID format: {MerchantId}", merchantId);
                return Enumerable.Empty<SurchargeTransaction>();
            }

            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .ThenInclude(c => c!.Provider)
                .Where(t => t.MerchantId == merchantGuid && t.Status == status)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for merchant {MerchantId} with status {Status}", merchantId, status);
            throw;
        }
    }

    public async Task<List<SurchargeTransaction>> GetByStatusAsync(SurchargeTransactionStatus status)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transactions by status {Status}", status);
            throw;
        }
    }

    public async Task<List<SurchargeTransaction>> GetByProviderConfigIdAsync(Guid providerConfigId)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Where(t => t.ProviderConfigId == providerConfigId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transactions by provider config ID {ProviderConfigId}", providerConfigId);
            throw;
        }
    }

    public async Task<SurchargeTransaction> UpdateAsync(SurchargeTransaction transaction)
    {
        try
        {
            _logger.LogDebug("Updating surcharge transaction {TransactionId}", transaction.Id);

            transaction.UpdatedAt = DateTime.UtcNow;

            _context.SurchargeTransactions.Update(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated surcharge transaction {TransactionId}", transaction.Id);

            return transaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating surcharge transaction {TransactionId}", transaction.Id);
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(Guid id, SurchargeTransactionStatus status, string? errorMessage = null)
    {
        try
        {
            _logger.LogDebug("Updating status for surcharge transaction {TransactionId} to {Status}", id, status);

            var transaction = await _context.SurchargeTransactions.FindAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("Surcharge transaction {TransactionId} not found for status update", id);
                return false;
            }

            transaction.Status = status;
            transaction.ErrorMessage = errorMessage;
            transaction.UpdatedAt = DateTime.UtcNow;

            if (status == SurchargeTransactionStatus.Completed || status == SurchargeTransactionStatus.Failed)
            {
                transaction.ProcessedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated status for surcharge transaction {TransactionId} to {Status}", id, status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status for surcharge transaction {TransactionId}", id);
            throw;
        }
    }

    public async Task<bool> UpdateResponseAsync(Guid id, JsonDocument responsePayload, DateTime processedAt)
    {
        try
        {
            _logger.LogDebug("Updating response for surcharge transaction {TransactionId}", id);

            var transaction = await _context.SurchargeTransactions.FindAsync(id);
            if (transaction == null)
            {
                _logger.LogWarning("Surcharge transaction {TransactionId} not found for response update", id);
                return false;
            }

            transaction.ResponsePayload = responsePayload;
            transaction.ProcessedAt = processedAt;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated response for surcharge transaction {TransactionId}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating response for surcharge transaction {TransactionId}", id);
            throw;
        }
    }

    public async Task<bool> ExistsByCorrelationIdAsync(string correlationId)
    {
        try
        {
            return await _context.SurchargeTransactions
                .AnyAsync(t => t.CorrelationId == correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of surcharge transaction by correlation ID {CorrelationId}", correlationId);
            throw;
        }
    }

    public async Task<object> GetTransactionStatisticsAsync(Guid merchantId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        try
        {
            var query = _context.SurchargeTransactions
                .Where(t => t.MerchantId == merchantId);

            if (fromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(t => t.CreatedAt <= toDate.Value);
            }

            var statistics = await query
                .GroupBy(t => t.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count(),
                    TotalAmount = g.Sum(t => t.Amount)
                })
                .ToListAsync();

            var totalTransactions = await query.CountAsync();
            var totalAmount = await query.SumAsync(t => t.Amount);

            return new
            {
                TotalTransactions = totalTransactions,
                TotalAmount = totalAmount,
                StatusBreakdown = statistics
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction statistics for merchant {MerchantId}", merchantId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetByProviderTransactionIdAsync(string providerTransactionId)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .FirstOrDefaultAsync(t => t.ProviderTransactionId == providerTransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transaction by provider transaction ID {ProviderTransactionId}", providerTransactionId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetByProviderTransactionIdAndCorrelationIdAsync(string providerTransactionId, string correlationId)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .FirstOrDefaultAsync(t => t.ProviderTransactionId == providerTransactionId && t.CorrelationId == correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transaction by provider transaction ID {ProviderTransactionId} and correlation ID {CorrelationId}", providerTransactionId, correlationId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetByProviderTransactionIdAndCorrelationIdForMerchantAsync(string providerTransactionId, string correlationId, Guid merchantId)
    {
        try
        {
            return await _context.SurchargeTransactions
                .Include(t => t.Merchant)
                .Include(t => t.ProviderConfig)
                .FirstOrDefaultAsync(t => t.ProviderTransactionId == providerTransactionId && 
                                         t.CorrelationId == correlationId && 
                                         t.MerchantId == merchantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transaction by provider transaction ID {ProviderTransactionId}, correlation ID {CorrelationId}, and merchant ID {MerchantId}", providerTransactionId, correlationId, merchantId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetLatestInOriginalChainAsync(Guid rootTransactionId, Guid merchantId)
    {
        try
        {
            // Start from the root transaction
            var current = await _context.SurchargeTransactions
                .FirstOrDefaultAsync(t => t.Id == rootTransactionId && t.MerchantId == merchantId);
            if (current == null)
                return null;

            while (true)
            {
                if (current == null)
                    break;
                Guid? currentId = current?.Id;
                if (currentId == null)
                    break;
                var child = await _context.SurchargeTransactions
                    .Where(t => t.OriginalSurchargeTransId == currentId && t.MerchantId == merchantId)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();
                if (child == null)
                    break;
                current = child;
            }
            return current;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error traversing original_surcharge_trans_id chain from {RootTransactionId}", rootTransactionId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetLatestInProviderTransactionChainAsync(string providerTransactionId, Guid merchantId)
    {
        try
        {
            // Find the root transaction for this providerTransactionId and merchant
            var root = await _context.SurchargeTransactions
                .Where(t => t.ProviderTransactionId == providerTransactionId && t.MerchantId == merchantId)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync();
            if (root == null)
                return null;

            // Traverse the chain to the latest transaction
            var current = root;
            while (true)
            {
                if (current == null)
                    break;
                Guid? currentId = current?.Id;
                if (currentId == null)
                    break;
                var child = await _context.SurchargeTransactions
                    .Where(t => t.OriginalSurchargeTransId == currentId && t.MerchantId == merchantId)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();
                if (child == null)
                    break;
                current = child;
            }
            return current;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error traversing providerTransactionId chain from {ProviderTransactionId}", providerTransactionId);
            throw;
        }
    }

    public async Task<SurchargeTransaction?> GetByIdWithProviderConfigAsync(Guid id)
    {
        try
        {
            // EF Core's ThenInclude will not dereference nulls, but to silence the warning, we can use a pragma
            #pragma warning disable CS8602 // Dereference of a possibly null reference.
            var result = await _context.SurchargeTransactions
                .Include(t => t.ProviderConfig)
                .ThenInclude(pc => pc.Provider)
                .FirstOrDefaultAsync(t => t.Id == id);
            #pragma warning restore CS8602
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting surcharge transaction with provider config by ID {TransactionId}", id);
            throw;
        }
    }
}
