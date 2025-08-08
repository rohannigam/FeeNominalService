using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Repositories;
using FeeNominalService.Tests.Infrastructure;
using FeeNominalService.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace FeeNominalService.Tests.Repositories;

public class SurchargeProviderConfigRepositoryTests : BaseRepositoryTest<SurchargeProviderConfigRepository>
{
    public SurchargeProviderConfigRepositoryTests() : base((context, logger) => new SurchargeProviderConfigRepository(context, logger))
    {
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingConfig_ShouldReturnConfig()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.ProviderId = provider.Id;
        
        Context.SurchargeProviders.Add(provider);
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByIdAsync(config.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(config.Id);
    }

    [Fact]
    public async Task GetPrimaryConfigAsync_WithPrimaryConfig_ShouldReturnConfig()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.Id = providerId;
        
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, providerId, isPrimary: true);
        
        Context.SurchargeProviders.Add(provider);
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetPrimaryConfigAsync(merchantId, providerId);

        // Assert
        result.Should().NotBeNull();
        result!.IsPrimary.Should().BeTrue();
        result.MerchantId.Should().Be(merchantId);
    }

    [Fact]
    public async Task GetPrimaryConfigByProviderCodeAsync_WithValidCode_ShouldReturnConfig()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.Code = "TEST_PROVIDER";
        
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, provider.Id, isPrimary: true);
        config.IsActive = true;
        
        Context.SurchargeProviders.Add(provider);
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetPrimaryConfigByProviderCodeAsync("TEST_PROVIDER", merchantId);

        // Assert
        result.Should().NotBeNull();
        result!.IsPrimary.Should().BeTrue();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetByMerchantIdAsync_ShouldReturnMerchantConfigs()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var provider1 = TestDataBuilder.CreateValidSurchargeProvider();
        var provider2 = TestDataBuilder.CreateValidSurchargeProvider();
        
        var config1 = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, provider1.Id);
        var config2 = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, provider2.Id);
        var otherConfig = TestDataBuilder.CreateValidProviderConfigWithMerchant(Guid.NewGuid(), provider1.Id);

        Context.SurchargeProviders.AddRange(provider1, provider2);
        Context.SurchargeProviderConfigs.AddRange(config1, config2, otherConfig);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.GetByMerchantIdAsync(merchantId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(c => c.MerchantId.Should().Be(merchantId));
    }

    [Fact]
    public async Task CreateAsync_WithValidConfig_ShouldCreateConfig()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.ProviderId = provider.Id;
        config.MerchantId = Guid.NewGuid();

        Context.SurchargeProviders.Add(provider);

        // Act
        var result = await Repository.CreateAsync(config);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(config.Id);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateAsync_WithValidConfig_ShouldUpdateConfig()
    {
        // Arrange
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        var config = TestDataBuilder.CreateValidProviderConfig();
        config.ProviderId = provider.Id;

        Context.SurchargeProviders.Add(provider);
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        Context.Entry(config).State = EntityState.Detached;
        config.ConfigName = "Updated Config";

        // Act
        var result = await Repository.UpdateAsync(config);

        // Assert
        result.ConfigName.Should().Be("Updated Config");
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingConfig_ShouldDeleteConfig()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.DeleteAsync(config.Id);

        // Assert
        result.Should().BeTrue();
        var deletedConfig = await Context.SurchargeProviderConfigs.FindAsync(config.Id);
        deletedConfig.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.ExistsAsync(config.Id);

        // Assert
        result.Should().BeTrue();
    }

    // TODO: Fix this test - issue with in-memory database and JsonDocument properties
    // [Fact]
    // public async Task GetActiveConfigsAsync_ShouldReturnOnlyActiveConfigs()
    // {
    //     // Arrange
    //     var merchantId = Guid.NewGuid();
    //     var providerId1 = Guid.NewGuid();
    //     var providerId2 = Guid.NewGuid();
        
    //     var activeConfig = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, providerId1);
    //     activeConfig.ConfigName = "Active Config";
    //     activeConfig.IsActive = true;
    //     activeConfig.IsPrimary = true;
        
    //     var inactiveConfig = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, providerId2);
    //     inactiveConfig.ConfigName = "Inactive Config";
    //     inactiveConfig.IsActive = false;
    //     inactiveConfig.IsPrimary = false;

    //     Context.SurchargeProviderConfigs.AddRange(activeConfig, inactiveConfig);
    //     await Context.SaveChangesAsync();

    //     // Act
    //     var result = await Repository.GetActiveConfigsAsync(merchantId);

    //     // Assert
    //     result.Should().HaveCount(1);
    //     if (result.Any())
    //     {
    //         result.First().IsActive.Should().BeTrue();
    //     }
    // }

    [Fact]
    public async Task HasActiveConfigAsync_WithActiveConfig_ShouldReturnTrue()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var provider = TestDataBuilder.CreateValidSurchargeProvider();
        provider.Id = providerId;
        
        var config = TestDataBuilder.CreateValidProviderConfigWithMerchant(merchantId, providerId);
        config.IsActive = true;

        Context.SurchargeProviders.Add(provider);
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        // Act
        var result = await Repository.HasActiveConfigAsync(merchantId, providerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLastUsedAsync_ShouldUpdateUsageMetrics()
    {
        // Arrange
        var config = TestDataBuilder.CreateValidProviderConfig();
        Context.SurchargeProviderConfigs.Add(config);
        await Context.SaveChangesAsync();

        // Act
        await Repository.UpdateLastUsedAsync(config.Id, success: true, responseTime: 150.5);

        // Assert
        var updatedConfig = await Context.SurchargeProviderConfigs.FindAsync(config.Id);
        updatedConfig!.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedConfig.LastSuccessAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        updatedConfig.SuccessCount.Should().Be(1);
    }
}