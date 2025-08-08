using System;
using FeeNominalService.Data;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FeeNominalService.Tests.Infrastructure;

/// <summary>
/// Base class for repository unit tests providing common infrastructure
/// </summary>
/// <typeparam name="TRepository">The repository type being tested</typeparam>
public abstract class BaseRepositoryTest<TRepository> : IDisposable
{
    /// <summary>
    /// The test database context
    /// </summary>
    protected readonly ApplicationDbContext Context;
    
    /// <summary>
    /// Mock logger for the repository
    /// </summary>
    protected readonly Mock<ILogger<TRepository>> MockLogger;
    
    /// <summary>
    /// The repository instance being tested
    /// </summary>
    protected readonly TRepository Repository;

    /// <summary>
    /// Initializes a new instance of the BaseRepositoryTest
    /// </summary>
    /// <param name="repositoryFactory">Factory function to create the repository instance</param>
    protected BaseRepositoryTest(Func<ApplicationDbContext, ILogger<TRepository>, TRepository> repositoryFactory)
    {
        // Use unique database name for each test class to avoid entity tracking conflicts
        Context = TestDbContextFactory.CreateInMemoryContext($"{typeof(TRepository).Name}_{Guid.NewGuid()}");
        MockLogger = new Mock<ILogger<TRepository>>();
        Repository = repositoryFactory(Context, MockLogger.Object);
    }

    /// <summary>
    /// Disposes of test resources
    /// </summary>
    public virtual void Dispose()
    {
        Context?.Dispose();
    }

    /// <summary>
    /// Clears all data from the test database while keeping reference data
    /// </summary>
    protected void ClearTestData()
    {
        // Remove test data but keep reference data like statuses
        var merchants = Context.Merchants.ToList();
        Context.Merchants.RemoveRange(merchants);
        
        var providers = Context.SurchargeProviders.ToList();
        Context.SurchargeProviders.RemoveRange(providers);
        
        var configs = Context.SurchargeProviderConfigs.ToList();
        Context.SurchargeProviderConfigs.RemoveRange(configs);
        
        var transactions = Context.SurchargeTransactions.ToList();
        Context.SurchargeTransactions.RemoveRange(transactions);
        
        var apiKeys = Context.ApiKeys.ToList();
        Context.ApiKeys.RemoveRange(apiKeys);
        
        Context.SaveChanges();
    }

    /// <summary>
    /// Verifies that a logger method was called with the expected log level
    /// </summary>
    /// <param name="logLevel">Expected log level</param>
    /// <param name="times">Expected number of times (default: once)</param>
    protected void VerifyLogged(LogLevel logLevel, Times? times = null)
    {
        times ??= Times.Once();
        
        MockLogger.Verify(
            logger => logger.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            times.Value);
    }

    /// <summary>
    /// Verifies that an information log was written
    /// </summary>
    /// <param name="times">Expected number of times (default: once)</param>
    protected void VerifyInformationLogged(Times? times = null)
    {
        VerifyLogged(LogLevel.Information, times);
    }

    /// <summary>
    /// Verifies that an error log was written
    /// </summary>
    /// <param name="times">Expected number of times (default: once)</param>
    protected void VerifyErrorLogged(Times? times = null)
    {
        VerifyLogged(LogLevel.Error, times);
    }

    /// <summary>
    /// Verifies that a warning log was written
    /// </summary>
    /// <param name="times">Expected number of times (default: once)</param>
    protected void VerifyWarningLogged(Times? times = null)
    {
        VerifyLogged(LogLevel.Warning, times);
    }
}