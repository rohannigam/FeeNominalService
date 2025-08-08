using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using FeeNominalService.Data;
using FeeNominalService.Models.Merchant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FeeNominalService.Tests.Infrastructure;

/// <summary>
/// Factory for creating test database contexts with proper EF Core configuration
/// </summary>
public static class TestDbContextFactory
{
    /// <summary>
    /// Creates a new ApplicationDbContext configured for testing with in-memory database
    /// </summary>
    /// <param name="databaseName">Optional database name. If null, generates unique name</param>
    /// <returns>Configured ApplicationDbContext for testing</returns>
    public static ApplicationDbContext CreateInMemoryContext(string? databaseName = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        
        // Create configuration
        var configuration = CreateTestConfiguration();
        
        // Configure EF Core with in-memory database and custom model configuration
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        // Create context directly without service provider
        var context = new TestApplicationDbContext(options, configuration);
        
        // Skip EnsureCreated() and manually seed data
        SeedTestData(context);
        
        return context;
    }
    
    /// <summary>
    /// Creates a test configuration with required settings
    /// </summary>
    private static IConfiguration CreateTestConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Database:Schema", "public"},
            {"Logging:LogLevel:Default", "Warning"},
            {"Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }
    
    /// <summary>
    /// Seeds the test database with required reference data
    /// </summary>
    private static void SeedTestData(ApplicationDbContext context)
    {
        // Only seed if not already present (avoid duplicates in tests)
        try
        {
            if (!context.MerchantStatuses.Any())
            {
                var statuses = new[]
                {
                    new MerchantStatus 
                    { 
                        MerchantStatusId = 1, 
                        Code = "ACTIVE", 
                        Name = "Active", 
                        Description = "Active merchant status",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new MerchantStatus 
                    { 
                        MerchantStatusId = 2, 
                        Code = "INACTIVE", 
                        Name = "Inactive", 
                        Description = "Inactive merchant status",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new MerchantStatus 
                    { 
                        MerchantStatusId = 3, 
                        Code = "PENDING", 
                        Name = "Pending", 
                        Description = "Pending merchant status",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new MerchantStatus 
                    { 
                        MerchantStatusId = 4, 
                        Code = "SUSPENDED", 
                        Name = "Suspended", 
                        Description = "Suspended merchant status",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                };

                context.MerchantStatuses.AddRange(statuses);
                context.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - some tests may not need seed data
            Console.WriteLine($"Warning: Failed to seed test data: {ex.Message}");
        }
    }
}

/// <summary>
/// Test-specific ApplicationDbContext that overrides model configuration for in-memory testing
/// </summary>
internal class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options, configuration)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // For testing, ignore JsonDocument properties that cause issues with InMemory provider
        // This avoids the need to modify production code while still allowing comprehensive testing
        modelBuilder.Entity<FeeNominalService.Models.SurchargeProvider.SurchargeProvider>()
            .Ignore(e => e.CredentialsSchema);

        modelBuilder.Entity<FeeNominalService.Models.SurchargeProvider.SurchargeProviderConfig>()
            .Ignore(e => e.Credentials)
            .Ignore(e => e.Metadata);

        modelBuilder.Entity<FeeNominalService.Models.SurchargeTransaction>()
            .Ignore(e => e.RequestPayload)
            .Ignore(e => e.ResponsePayload);

        // Remove any PostgreSQL-specific configurations that don't work with InMemory
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.GetColumnType() == "jsonb")
                {
                    property.SetColumnType(null);
                }
            }
        }
    }
}