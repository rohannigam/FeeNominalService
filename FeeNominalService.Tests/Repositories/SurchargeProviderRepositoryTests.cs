using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Repositories;
using FeeNominalService.Tests.Infrastructure;
using FeeNominalService.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FluentAssertions;

namespace FeeNominalService.Tests.Repositories;

public class SurchargeProviderRepositoryTests : BaseRepositoryTest<SurchargeProviderRepository>
{
    public SurchargeProviderRepositoryTests() : base((context, logger) => new SurchargeProviderRepository(context, logger))
    {
        SeedProviderStatuses();
    }

    private void SeedProviderStatuses()
    {
        if (!Context.SurchargeProviderStatuses.Any())
        {
            var statuses = new[]
            {
                new SurchargeProviderStatus { StatusId = 1, Code = "ACTIVE", Name = "Active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new SurchargeProviderStatus { StatusId = 2, Code = "INACTIVE", Name = "Inactive", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new SurchargeProviderStatus { StatusId = 3, Code = "DELETED", Name = "Deleted", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };
            Context.SurchargeProviderStatuses.AddRange(statuses);
            Context.SaveChanges();
        }
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingProvider_ShouldReturnProvider()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 1;
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByIdAsync(provider.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(provider.Id);
        result.Status.Should().NotBeNull();
        result.Status!.Code.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetByIdAsync_WithDeletedProvider_ShouldReturnNull()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 3; // DELETED
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByIdAsync(provider.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithIncludeDeleted_ShouldReturnDeletedProvider()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 3; // DELETED
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByIdAsync(provider.Id, includeDeleted: true);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(provider.Id);
    }

    [Fact]
    public async Task GetByCodeAsync_WithExistingCode_ShouldReturnProvider()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 1;
        provider.Code = "TEST_PROVIDER";
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByCodeAsync("TEST_PROVIDER");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("TEST_PROVIDER");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnActiveProviders()
    {
        // Arrange
        var activeProvider = TestDataBuilder.CreateValidSurchargeProvider();
        activeProvider.StatusId = 1;
        var deletedProvider = TestDataBuilder.CreateValidSurchargeProvider();
        deletedProvider.StatusId = 3;

        Context.SurchargeProviders.AddRange(activeProvider, deletedProvider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetAllAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(activeProvider.Id);
    }

    [Fact]
    public async Task GetByMerchantIdAsync_ShouldReturnMerchantProviders()
    {
        // Arrange
        var merchantId = "merchant-123";
        var provider1 = TestDataBuilder.CreateValidSurchargeProvider();
        provider1.StatusId = 1;
        provider1.CreatedBy = merchantId;
        var provider2 = TestDataBuilder.CreateValidSurchargeProvider();
        provider2.StatusId = 1;
        provider2.CreatedBy = "other-merchant";

        Context.SurchargeProviders.AddRange(provider1, provider2);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByMerchantIdAsync(merchantId);

        // Assert
        result.Should().ContainSingle();
        result.First().CreatedBy.Should().Be(merchantId);
    }

    [Fact]
    public async Task AddAsync_WithValidProvider_ShouldCreateProvider()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 1;

        // Act
        var result = await Repository.AddAsync(provider);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(provider.Id);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_WithValidProvider_ShouldUpdateProvider()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 1;
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        Context.Entry(provider).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        provider.Name = "Updated Name";

        // Act
        var result = await Repository.UpdateAsync(provider);

        // Assert
        result.Name.Should().Be("Updated Name");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingProvider_ShouldDeleteProvider()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.DeleteAsync(provider.Id);

        // Assert
        result.Should().BeTrue();
        var deletedProvider = await Context.SurchargeProviders.FindAsync(provider.Id);
        deletedProvider.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_WithValidProvider_ShouldMarkAsDeleted()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 1;
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.SoftDeleteAsync(provider.Id, "admin");

        // Assert
        result.Should().BeTrue();
        var softDeletedProvider = await Context.SurchargeProviders
            .Include(p => p.Status)
            .FirstOrDefaultAsync(p => p.Id == provider.Id);
        softDeletedProvider!.Status.Code.Should().Be("DELETED");
    }

    [Fact]
    public async Task RestoreAsync_WithDeletedProvider_ShouldRestoreProvider()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 3; // DELETED
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.RestoreAsync(provider.Id, "admin");

        // Assert
        result.Should().BeTrue();
        var restoredProvider = await Context.SurchargeProviders
            .Include(p => p.Status)
            .FirstOrDefaultAsync(p => p.Id == provider.Id);
        restoredProvider!.Status.Code.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task ExistsAsync_WithExistingProvider_ShouldReturnTrue()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 1;
        Context.SurchargeProviders.Add(provider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.ExistsAsync(provider.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetActiveAsync_ShouldReturnOnlyActiveProviders()
    {
        // Arrange
        var activeProvider = TestDataBuilder.CreateValidSurchargeProvider();
        activeProvider.StatusId = 1;
        var inactiveProvider = TestDataBuilder.CreateValidSurchargeProvider();
        inactiveProvider.StatusId = 2;

        Context.SurchargeProviders.AddRange(activeProvider, inactiveProvider);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetActiveAsync();

        // Assert
        result.Should().ContainSingle();
        result.First().Id.Should().Be(activeProvider.Id);
    }

    [Fact]
    public async Task AddWithLimitCheckAsync_WhenUnderLimit_ShouldSucceed()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.StatusId = 1;
        provider.CreatedBy = "merchant-123";

        // Act
        var result = await Repository.AddWithLimitCheckAsync(provider, maxProvidersPerMerchant: 5);

        // Assert
        result.Should().NotBeNull();
        VerifyInformationLogged();
    }

    [Fact]
    public async Task AddWithLimitCheckAsync_WhenAtLimit_ShouldThrowException()
    {
        // Arrange
        var merchantId = "merchant-123";
        var existingProviders = TestDataBuilder.CreateMultipleProviders(3);
        foreach (var p in existingProviders)
        {
            p.StatusId = 1;
            p.CreatedBy = merchantId;
        }
        Context.SurchargeProviders.AddRange(existingProviders);
        await Context.SaveChangesAsync();

        var newProvider = TestDataBuilder.CreateValidSurchargeProvider();
        newProvider.StatusId = 1;
        newProvider.CreatedBy = merchantId;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Repository.AddWithLimitCheckAsync(newProvider, maxProvidersPerMerchant: 3));
        
        exception.Message.Should().Contain("maximum number of providers");
    }
}