using System;
using System.Linq;
using System.Threading.Tasks;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Repositories;
using FeeNominalService.Tests.Infrastructure;
using FeeNominalService.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace FeeNominalService.Tests.Repositories;

public class ApiKeyRepositoryTests : BaseRepositoryTest<ApiKeyRepository>
{
    public ApiKeyRepositoryTests() : base((context, logger) => new ApiKeyRepository(context, logger))
    {
    }

    [Fact]
    public async Task GetByMerchantIdAsync_ShouldReturnMerchantApiKeys()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var apiKey1 = TestDataBuilder.CreateValidApiKey(merchantId);
        apiKey1.Scope = "merchant";
        var apiKey2 = TestDataBuilder.CreateValidApiKey(Guid.NewGuid());
        apiKey2.Scope = "merchant";

        Context.ApiKeys.AddRange(apiKey1, apiKey2);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByMerchantIdAsync(merchantId);

        // Assert
        result.Should().ContainSingle();
        result.First().MerchantId.Should().Be(merchantId);
    }

    [Fact]
    public async Task GetByScopeAsync_ShouldReturnApiKeysWithMatchingScope()
    {
        // Arrange
        var merchantKey = TestDataBuilder.CreateValidApiKey();
        merchantKey.Scope = "merchant";
        var adminKey = TestDataBuilder.CreateValidApiKey();
        adminKey.Scope = "admin";

        Context.ApiKeys.AddRange(merchantKey, adminKey);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByScopeAsync("admin");

        // Assert
        result.Should().ContainSingle();
        result.First().Scope.Should().Be("admin");
    }

    [Fact]
    public async Task GetByKeyAsync_WithExistingKey_ShouldReturnApiKey()
    {
        // Arrange
        var apiKey = TestDataBuilder.CreateValidApiKey();
        apiKey.Key = "test-key-123";
        Context.ApiKeys.Add(apiKey);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByKeyAsync("test-key-123");

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be("test-key-123");
    }

    [Fact]
    public async Task GetByKeyAsync_WithNonExistentKey_ShouldReturnNull()
    {
        // Act
        var result = await Repository.GetByKeyAsync("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAdminKeyAsync_WithExistingAdminKey_ShouldReturnKey()
    {
        // Arrange
        var adminKey = TestDataBuilder.CreateValidApiKey();
        adminKey.Scope = "admin";
        adminKey.Status = "ACTIVE";
        Context.ApiKeys.Add(adminKey);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetAdminKeyAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Scope.Should().Be("admin");
        result.Status.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task GetAdminKeyAsync_WithNoAdminKey_ShouldReturnNull()
    {
        // Arrange
        var merchantKey = TestDataBuilder.CreateValidApiKey();
        merchantKey.Scope = "merchant";
        Context.ApiKeys.Add(merchantKey);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetAdminKeyAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAdminKeyAsync_WithMultipleAdminKeys_ShouldReturnMostRecent()
    {
        // Arrange
        var oldAdminKey = TestDataBuilder.CreateValidApiKey();
        oldAdminKey.Scope = "admin";
        oldAdminKey.Status = "ACTIVE";
        oldAdminKey.CreatedAt = DateTime.UtcNow.AddDays(-5);
        
        var newAdminKey = TestDataBuilder.CreateValidApiKey();
        newAdminKey.Scope = "admin";
        newAdminKey.Status = "ACTIVE";
        newAdminKey.CreatedAt = DateTime.UtcNow;

        Context.ApiKeys.AddRange(oldAdminKey, newAdminKey);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetAdminKeyAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(newAdminKey.Id);
    }

    [Fact]
    public async Task CreateAsync_WithValidApiKey_ShouldCreateApiKey()
    {
        // Arrange
        var apiKey = TestDataBuilder.CreateValidApiKey();

        // Act
        var result = await Repository.CreateAsync(apiKey);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(apiKey.Id);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        VerifyInformationLogged();
    }

    [Fact]
    public async Task UpdateAsync_WithValidApiKey_ShouldUpdateApiKey()
    {
        // Arrange
        var apiKey = TestDataBuilder.CreateValidApiKey();
        Context.ApiKeys.Add(apiKey);
        await Context.SaveChangesAsync();

        Context.Entry(apiKey).State = EntityState.Detached;
        apiKey.Description = "Updated Description";

        // Act
        var result = await Repository.UpdateAsync(apiKey);

        // Assert
        result.Description.Should().Be("Updated Description");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        VerifyInformationLogged();
    }

    [Fact]
    public async Task GetByMerchantIdAsync_WithEmptyDatabase_ShouldReturnEmpty()
    {
        // Act
        var result = await Repository.GetByMerchantIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByScopeAsync_WithEmptyDatabase_ShouldReturnEmpty()
    {
        // Act
        var result = await Repository.GetByScopeAsync("admin");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByScopeAsync_ShouldOrderByCreatedAt()
    {
        // Arrange
        var oldKey = TestDataBuilder.CreateValidApiKey();
        oldKey.Scope = "merchant";
        oldKey.CreatedAt = DateTime.UtcNow.AddDays(-2);
        
        var newKey = TestDataBuilder.CreateValidApiKey();
        newKey.Scope = "merchant";
        newKey.CreatedAt = DateTime.UtcNow;

        Context.ApiKeys.AddRange(oldKey, newKey);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByScopeAsync("merchant");

        // Assert
        result.Should().HaveCount(2);
        result.First().Id.Should().Be(oldKey.Id); // Should be ordered by CreatedAt ascending
        result.Last().Id.Should().Be(newKey.Id);
    }
}