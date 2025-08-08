using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FeeNominalService.Data;
using Microsoft.EntityFrameworkCore;

namespace FeeNominalService.Tests.Infrastructure;

/// <summary>
/// Utility methods for testing
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Executes an action within a database transaction that is automatically rolled back
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="action">The action to execute</param>
    public static async Task ExecuteInTransactionAsync(ApplicationDbContext context, Func<Task> action)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await action();
            // Don't commit - always rollback to isolate tests
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Executes an action within a database transaction that is automatically rolled back
    /// </summary>
    /// <param name="context">The database context</param>
    /// <param name="action">The action to execute</param>
    /// <returns>The result of the action</returns>
    public static async Task<T> ExecuteInTransactionAsync<T>(ApplicationDbContext context, Func<Task<T>> action)
    {
        using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            var result = await action();
            // Don't commit - always rollback to isolate tests
            return result;
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    /// <summary>
    /// Verifies that an entity exists in the database
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="context">The database context</param>
    /// <param name="predicate">The predicate to find the entity</param>
    /// <returns>True if the entity exists</returns>
    public static async Task<bool> EntityExistsAsync<TEntity>(
        ApplicationDbContext context, 
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate) 
        where TEntity : class
    {
        return await context.Set<TEntity>().AnyAsync(predicate);
    }

    /// <summary>
    /// Gets the count of entities matching a predicate
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="context">The database context</param>
    /// <param name="predicate">The predicate to filter entities</param>
    /// <returns>The count of matching entities</returns>
    public static async Task<int> GetEntityCountAsync<TEntity>(
        ApplicationDbContext context, 
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate) 
        where TEntity : class
    {
        return await context.Set<TEntity>().CountAsync(predicate);
    }

    /// <summary>
    /// Finds an entity by its primary key and includes related data
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="context">The database context</param>
    /// <param name="keyValue">The primary key value</param>
    /// <param name="includeProperties">Navigation properties to include</param>
    /// <returns>The entity with included data or null if not found</returns>
    public static async Task<TEntity?> FindWithIncludesAsync<TEntity>(
        ApplicationDbContext context,
        object keyValue,
        params string[] includeProperties) 
        where TEntity : class
    {
        IQueryable<TEntity> query = context.Set<TEntity>();
        
        foreach (var includeProperty in includeProperties)
        {
            query = query.Include(includeProperty);
        }
        
        return await query.FirstOrDefaultAsync(e => EF.Property<object>(e, GetPrimaryKeyName<TEntity>(context)).Equals(keyValue));
    }

    /// <summary>
    /// Gets the primary key property name for an entity type
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <param name="context">The database context</param>
    /// <returns>The primary key property name</returns>
    private static string GetPrimaryKeyName<TEntity>(ApplicationDbContext context) where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var primaryKey = entityType?.FindPrimaryKey();
        return primaryKey?.Properties.First().Name ?? "Id";
    }

    /// <summary>
    /// Creates a unique string identifier for testing
    /// </summary>
    /// <param name="prefix">Optional prefix</param>
    /// <returns>A unique string</returns>
    public static string CreateUniqueString(string prefix = "test")
    {
        return $"{prefix}-{Guid.NewGuid().ToString()[..8]}";
    }

    /// <summary>
    /// Creates a unique integer identifier for testing
    /// </summary>
    /// <returns>A unique integer based on timestamp</returns>
    public static int CreateUniqueInt()
    {
        return (int)(DateTime.UtcNow.Ticks % int.MaxValue);
    }

    /// <summary>
    /// Waits for a condition to be true with timeout
    /// </summary>
    /// <param name="condition">The condition to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="interval">Check interval</param>
    /// <returns>True if condition became true within timeout</returns>
    public static async Task<bool> WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? interval = null)
    {
        interval ??= TimeSpan.FromMilliseconds(100);
        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (condition())
                return true;
                
            await Task.Delay(interval.Value);
        }
        
        return condition();
    }

    /// <summary>
    /// Waits for an async condition to be true with timeout
    /// </summary>
    /// <param name="condition">The async condition to wait for</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="interval">Check interval</param>
    /// <returns>True if condition became true within timeout</returns>
    public static async Task<bool> WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? interval = null)
    {
        interval ??= TimeSpan.FromMilliseconds(100);
        var endTime = DateTime.UtcNow.Add(timeout);
        
        while (DateTime.UtcNow < endTime)
        {
            if (await condition())
                return true;
                
            await Task.Delay(interval.Value);
        }
        
        return await condition();
    }
}