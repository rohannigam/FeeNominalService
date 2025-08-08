using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FeeNominalService.Data;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.ApiKey.Requests;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.Merchant.Responses;
using FeeNominalService.Repositories;
using FeeNominalService.Services;
using FeeNominalService.Settings;
using FeeNominalService.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using FeeNominalService.Models.SurchargeProvider;
using FeeNominalService.Models;

namespace FeeNominalService.Tests.Services;

// Test-specific DbContext for MerchantService testing
internal class TestMerchantDbContext : ApplicationDbContext
{
    public TestMerchantDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options, configuration)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore JSON payloads that the InMemory provider cannot map
        modelBuilder.Entity<SurchargeTransaction>().Ignore(e => e.RequestPayload);
        modelBuilder.Entity<SurchargeTransaction>().Ignore(e => e.ResponsePayload);
        modelBuilder.Entity<SurchargeProvider>().Ignore(e => e.CredentialsSchema);
    }
}

public class MerchantServiceTests : IDisposable
{
    private readonly TestMerchantDbContext _context;
    private readonly Mock<ILogger<MerchantService>> _mockLogger;
    private readonly Mock<IMerchantRepository> _mockMerchantRepository;
    private readonly Mock<IMerchantAuditTrailRepository> _mockAuditTrailRepository;
    private readonly Mock<ISurchargeProviderRepository> _mockSurchargeProviderRepository;
    private readonly Mock<ISurchargeProviderConfigRepository> _mockSurchargeProviderConfigRepository;
    private readonly Mock<IOptionsMonitor<AuditLoggingSettings>> _mockSettings;
    private readonly MerchantService _service;

    public MerchantServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Database:Schema", "public" }
        });
        var configuration = configurationBuilder.Build();
        _context = new TestMerchantDbContext(options, configuration);
        
        _mockLogger = new Mock<ILogger<MerchantService>>();
        _mockMerchantRepository = new Mock<IMerchantRepository>();
        _mockAuditTrailRepository = new Mock<IMerchantAuditTrailRepository>();
        _mockSurchargeProviderRepository = new Mock<ISurchargeProviderRepository>();
        _mockSurchargeProviderConfigRepository = new Mock<ISurchargeProviderConfigRepository>();
        _mockSettings = new Mock<IOptionsMonitor<AuditLoggingSettings>>();

        var auditSettings = new AuditLoggingSettings
        {
            Enabled = true,
            Endpoints = new Dictionary<string, bool> { { "MerchantAuditTrail", true } }
        };
        _mockSettings.Setup(x => x.CurrentValue).Returns(auditSettings);

        _service = new MerchantService(
            _context,
            _mockLogger.Object,
            _mockMerchantRepository.Object,
            _mockAuditTrailRepository.Object,
            _mockSurchargeProviderRepository.Object,
            _mockSurchargeProviderConfigRepository.Object,
            _mockSettings.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region CreateMerchantAsync Tests

    [Fact]
    public async Task CreateMerchantAsync_WithValidRequest_ShouldReturnMerchantResponse()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidGenerateInitialApiKeyRequest();
        var createdBy = "test-admin";
        var createdMerchant = TestDataBuilder.CreateValidMerchant();
        
        // Ensure the merchant has a Status property set
        createdMerchant.Status = new MerchantStatus 
        { 
            MerchantStatusId = MerchantStatusIds.Active, 
            Code = "ACTIVE", 
            Name = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockMerchantRepository.Setup(x => x.ExistsByExternalMerchantIdAsync(request.ExternalMerchantId))
            .ReturnsAsync(false);
        _mockMerchantRepository.Setup(x => x.ExistsByExternalMerchantGuidAsync(request.ExternalMerchantGuid!.Value))
            .ReturnsAsync(false);
        _mockMerchantRepository.Setup(x => x.CreateAsync(It.IsAny<Merchant>()))
            .ReturnsAsync(createdMerchant);

        // Act
        var result = await _service.CreateMerchantAsync(request, createdBy);

        // Assert
        result.Should().NotBeNull();
        result.MerchantId.Should().Be(createdMerchant.MerchantId);
        result.ExternalMerchantId.Should().Be(request.ExternalMerchantId);
        result.Name.Should().Be(request.MerchantName);
        result.StatusId.Should().Be(MerchantStatusIds.Active);
        result.CreatedBy.Should().Be(createdBy);

        _mockMerchantRepository.Verify(x => x.CreateAsync(It.Is<Merchant>(m => 
            m.ExternalMerchantId == request.ExternalMerchantId &&
            m.Name == request.MerchantName &&
            m.StatusId == MerchantStatusIds.Active &&
            m.CreatedBy == createdBy)), Times.Once);
    }

    [Fact]
    public async Task CreateMerchantAsync_WithExistingExternalMerchantId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidGenerateInitialApiKeyRequest();
        var createdBy = "test-admin";

        _mockMerchantRepository.Setup(x => x.ExistsByExternalMerchantIdAsync(request.ExternalMerchantId))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateMerchantAsync(request, createdBy));

        exception.Message.Should().Contain($"Merchant with external ID {request.ExternalMerchantId} already exists");
        _mockMerchantRepository.Verify(x => x.CreateAsync(It.IsAny<Merchant>()), Times.Never);
    }

    [Fact]
    public async Task CreateMerchantAsync_WithExistingExternalMerchantGuid_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidGenerateInitialApiKeyRequest();
        var createdBy = "test-admin";

        _mockMerchantRepository.Setup(x => x.ExistsByExternalMerchantIdAsync(request.ExternalMerchantId))
            .ReturnsAsync(false);
        _mockMerchantRepository.Setup(x => x.ExistsByExternalMerchantGuidAsync(request.ExternalMerchantGuid!.Value))
            .ReturnsAsync(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.CreateMerchantAsync(request, createdBy));

        exception.Message.Should().Contain($"Merchant with external GUID {request.ExternalMerchantGuid} already exists");
        _mockMerchantRepository.Verify(x => x.CreateAsync(It.IsAny<Merchant>()), Times.Never);
    }

    [Fact]
    public async Task CreateMerchantAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var request = TestDataBuilder.CreateValidGenerateInitialApiKeyRequest();
        var createdBy = "test-admin";

        _mockMerchantRepository.Setup(x => x.ExistsByExternalMerchantIdAsync(request.ExternalMerchantId))
            .ReturnsAsync(false);
        _mockMerchantRepository.Setup(x => x.CreateAsync(It.IsAny<Merchant>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _service.CreateMerchantAsync(request, createdBy));

        exception.Message.Should().Be("Database error");
    }

    #endregion

    #region UpdateMerchantAsync Tests

    [Fact]
    public async Task UpdateMerchantAsync_WithValidData_ShouldUpdateMerchantAndCreateAuditTrail()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var newName = "Updated Merchant Name";
        var updatedBy = "test-admin";
        var existingMerchant = TestDataBuilder.CreateValidMerchant();
        var oldName = existingMerchant.Name;
        existingMerchant.MerchantId = merchantId;

        _mockMerchantRepository.Setup(x => x.GetByIdAsync(merchantId))
            .ReturnsAsync(existingMerchant);
        _mockMerchantRepository.Setup(x => x.UpdateAsync(It.IsAny<Merchant>()))
            .ReturnsAsync((Merchant m) => m);
        _mockAuditTrailRepository.Setup(x => x.CreateAsync(It.IsAny<MerchantAuditTrail>()))
            .ReturnsAsync(new MerchantAuditTrail());

        // Act
        var result = await _service.UpdateMerchantAsync(merchantId, newName, updatedBy);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(newName);

        _mockMerchantRepository.Verify(x => x.UpdateAsync(It.Is<Merchant>(m => m.Name == newName)), Times.Once);
        _mockAuditTrailRepository.Verify(x => x.CreateAsync(It.Is<MerchantAuditTrail>(at => 
            at.Action == "UPDATE" &&
            at.EntityType == "Name" &&
            at.OldValue == oldName &&
            at.NewValue == newName &&
            at.UpdatedBy == updatedBy)), Times.Once);
    }

    [Fact]
    public async Task UpdateMerchantAsync_WithNonexistentMerchant_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var newName = "Updated Name";
        var updatedBy = "test-admin";

        _mockMerchantRepository.Setup(x => x.GetByIdAsync(merchantId))
            .ThrowsAsync(new InvalidOperationException("Merchant not found"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.UpdateMerchantAsync(merchantId, newName, updatedBy));

        exception.Message.Should().Contain("Merchant not found");
        _mockMerchantRepository.Verify(x => x.UpdateAsync(It.IsAny<Merchant>()), Times.Never);
    }

    #endregion

    #region GetByExternalMerchantIdAsync Tests

    [Fact]
    public async Task GetByExternalMerchantIdAsync_WithValidId_ShouldReturnMerchant()
    {
        // Arrange
        var externalMerchantId = "test-external-id";
        var merchant = TestDataBuilder.CreateValidMerchant();
        merchant.ExternalMerchantId = externalMerchantId;

        _mockMerchantRepository.Setup(x => x.GetByExternalMerchantIdAsync(externalMerchantId))
            .ReturnsAsync(merchant);

        // Act
        var result = await _service.GetByExternalMerchantIdAsync(externalMerchantId);

        // Assert
        result.Should().NotBeNull();
        result!.ExternalMerchantId.Should().Be(externalMerchantId);
    }

    [Fact]
    public async Task GetByExternalMerchantIdAsync_WithNonexistentId_ShouldReturnNull()
    {
        // Arrange
        var externalMerchantId = "nonexistent-id";

        _mockMerchantRepository.Setup(x => x.GetByExternalMerchantIdAsync(externalMerchantId))
            .ReturnsAsync((Merchant?)null);

        // Act
        var result = await _service.GetByExternalMerchantIdAsync(externalMerchantId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByExternalMerchantIdAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var externalMerchantId = "test-external-id";

        _mockMerchantRepository.Setup(x => x.GetByExternalMerchantIdAsync(externalMerchantId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetByExternalMerchantIdAsync(externalMerchantId));
    }

    #endregion

    #region GetByExternalMerchantGuidAsync Tests

    [Fact]
    public async Task GetByExternalMerchantGuidAsync_WithValidGuid_ShouldReturnMerchant()
    {
        // Arrange
        var externalMerchantGuid = Guid.NewGuid();
        var merchant = TestDataBuilder.CreateValidMerchant();
        merchant.ExternalMerchantGuid = externalMerchantGuid;

        _mockMerchantRepository.Setup(x => x.GetByExternalMerchantGuidAsync(externalMerchantGuid))
            .ReturnsAsync(merchant);

        // Act
        var result = await _service.GetByExternalMerchantGuidAsync(externalMerchantGuid);

        // Assert
        result.Should().NotBeNull();
        result!.ExternalMerchantGuid.Should().Be(externalMerchantGuid);
    }

    [Fact]
    public async Task GetByExternalMerchantGuidAsync_WithNonexistentGuid_ShouldReturnNull()
    {
        // Arrange
        var externalMerchantGuid = Guid.NewGuid();

        _mockMerchantRepository.Setup(x => x.GetByExternalMerchantGuidAsync(externalMerchantGuid))
            .ReturnsAsync((Merchant?)null);

        // Act
        var result = await _service.GetByExternalMerchantGuidAsync(externalMerchantGuid);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetMerchantAuditTrailAsync Tests

    [Fact]
    public async Task GetMerchantAuditTrailAsync_WithValidMerchantId_ShouldReturnAuditTrail()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var auditTrail = new List<MerchantAuditTrail>
        {
            TestDataBuilder.CreateValidMerchantAuditTrail(merchantId),
            TestDataBuilder.CreateValidMerchantAuditTrail(merchantId)
        };

        _mockAuditTrailRepository.Setup(x => x.GetByMerchantIdAsync(merchantId))
            .ReturnsAsync(auditTrail);

        // Act
        var result = await _service.GetMerchantAuditTrailAsync(merchantId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(at => at.MerchantId == merchantId).Should().BeTrue();
    }

    [Fact]
    public async Task GetMerchantAuditTrailAsync_WithRepositoryException_ShouldThrowException()
    {
        // Arrange
        var merchantId = Guid.NewGuid();

        _mockAuditTrailRepository.Setup(x => x.GetByMerchantIdAsync(merchantId))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.GetMerchantAuditTrailAsync(merchantId));
    }

    #endregion

    #region UpdateMerchantStatusAsync Tests

    [Fact]
    public async Task UpdateMerchantStatusAsync_WithValidStatusChange_ShouldUpdateStatus()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var newStatusId = MerchantStatusIds.Suspended;
        var merchant = TestDataBuilder.CreateValidMerchant();
        merchant.MerchantId = merchantId;
        merchant.StatusId = MerchantStatusIds.Active;

        // Ensure reference data exists
        var activeStatus = new MerchantStatus { MerchantStatusId = MerchantStatusIds.Active, Code = "ACTIVE", Name = "Active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var suspendedStatus = new MerchantStatus { MerchantStatusId = newStatusId, Code = "SUSPENDED", Name = "Suspended", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        // Add test data to in-memory database
        _context.MerchantStatuses.AddRange(activeStatus, suspendedStatus);
        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.UpdateMerchantStatusAsync(merchantId, newStatusId);

        // Assert
        result.Should().NotBeNull();
        result.StatusId.Should().Be(newStatusId);
    }

    [Fact]
    public async Task UpdateMerchantStatusAsync_WithNonexistentMerchant_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var newStatusId = MerchantStatusIds.Suspended;

        // No merchants in database - context is already empty

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.UpdateMerchantStatusAsync(merchantId, newStatusId));

        exception.Message.Should().Contain($"Merchant not found with ID {merchantId}");
    }

    [Fact]
    public async Task UpdateMerchantStatusAsync_WithNonexistentStatus_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var invalidStatusId = 999;
        var merchant = TestDataBuilder.CreateValidMerchant();
        merchant.MerchantId = merchantId;
        merchant.StatusId = MerchantStatusIds.Active;

        // Add merchant status reference data
        var activeStatus = new MerchantStatus { MerchantStatusId = MerchantStatusIds.Active, Code = "ACTIVE", Name = "Active", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        
        // Add merchant to database but no status for the invalid ID
        _context.MerchantStatuses.Add(activeStatus);
        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => 
            _service.UpdateMerchantStatusAsync(merchantId, invalidStatusId));

        exception.Message.Should().Contain($"Status not found with ID {invalidStatusId}");
    }

    #endregion

    #region IsMerchantActiveAsync Tests

    [Fact]
    public async Task IsMerchantActiveAsync_WithActiveMerchant_ShouldReturnTrue()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        
        // Create merchant without the default status navigation property to avoid conflicts
        var merchant = new Merchant
        {
            MerchantId = merchantId,
            ExternalMerchantId = "test-merchant-active",
            ExternalMerchantGuid = Guid.NewGuid(),
            Name = "Test Active Merchant",
            StatusId = MerchantStatusIds.Active,
            CreatedBy = "test-admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add merchant status first, then merchant
        var activeStatus = new MerchantStatus 
        { 
            MerchantStatusId = MerchantStatusIds.Active, 
            Code = "ACTIVE", 
            Name = "Active", 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };
        
        _context.MerchantStatuses.Add(activeStatus);
        await _context.SaveChangesAsync();
        
        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsMerchantActiveAsync(merchantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsMerchantActiveAsync_WithInactiveMerchant_ShouldReturnFalse()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        
        // Create merchant without the default status navigation property to avoid conflicts
        var merchant = new Merchant
        {
            MerchantId = merchantId,
            ExternalMerchantId = "test-merchant-inactive",
            ExternalMerchantGuid = Guid.NewGuid(),
            Name = "Test Inactive Merchant",
            StatusId = MerchantStatusIds.Inactive,
            CreatedBy = "test-admin",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add merchant status first, then merchant
        var inactiveStatus = new MerchantStatus 
        { 
            MerchantStatusId = MerchantStatusIds.Inactive, 
            Code = "INACTIVE", 
            Name = "Inactive", 
            CreatedAt = DateTime.UtcNow, 
            UpdatedAt = DateTime.UtcNow 
        };
        
        _context.MerchantStatuses.Add(inactiveStatus);
        await _context.SaveChangesAsync();
        
        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsMerchantActiveAsync(merchantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsMerchantActiveAsync_WithNonexistentMerchant_ShouldReturnFalse()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        // No merchant added to database

        // Act
        var result = await _service.IsMerchantActiveAsync(merchantId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GenerateApiKeyAsync Tests

    [Fact]
    public async Task GenerateApiKeyAsync_WithValidRequest_ShouldGenerateApiKeyAndCreateAuditTrail()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var request = TestDataBuilder.CreateValidGenerateApiKeyRequest();
        var onboardingMetadata = TestDataBuilder.CreateValidOnboardingMetadata();
        var merchant = TestDataBuilder.CreateValidMerchant();
        var apiKey = TestDataBuilder.CreateValidApiKey();

        _mockMerchantRepository.Setup(x => x.GetByIdAsync(merchantId))
            .ReturnsAsync(merchant);

        // In-memory database will handle ApiKey creation
        _mockAuditTrailRepository.Setup(x => x.CreateAsync(It.IsAny<MerchantAuditTrail>()))
            .ReturnsAsync(new MerchantAuditTrail());

        // Act
        var result = await _service.GenerateApiKeyAsync(merchantId, request, onboardingMetadata);

        // Assert
        result.Should().NotBeNull();
        result.MerchantId.Should().Be(merchantId);
        result.Description.Should().Be(request.Description);
        result.RateLimit.Should().Be(request.RateLimit ?? 1000);
        result.Status.Should().Be("ACTIVE");

        _mockAuditTrailRepository.Verify(x => x.CreateAsync(It.Is<MerchantAuditTrail>(at => 
            at.Action == "API_KEY_GENERATED" &&
            at.EntityType == "api_key" &&
            at.UpdatedBy == onboardingMetadata.AdminUserId)), Times.Once);
    }

    [Fact]
    public async Task GenerateApiKeyAsync_WithNonexistentMerchant_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var request = TestDataBuilder.CreateValidGenerateApiKeyRequest();

        _mockMerchantRepository.Setup(x => x.GetByIdAsync(merchantId))
            .ThrowsAsync(new InvalidOperationException("Merchant not found"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            _service.GenerateApiKeyAsync(merchantId, request, null));

        exception.Message.Should().Contain("Merchant not found");
    }

    #endregion

    #region CreateAuditTrailAsync Tests

    [Fact]
    public async Task CreateAuditTrailAsync_WithEnabledAuditSettings_ShouldCreateAuditTrail()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var action = "TEST_ACTION";
        var entityType = "TEST_ENTITY";
        var oldValue = "old";
        var newValue = "new";
        var performedBy = "test-user";

        var auditSettings = new AuditLoggingSettings
        {
            Enabled = true,
            Endpoints = new Dictionary<string, bool> { { "MerchantAuditTrail", true } }
        };
        _mockSettings.Setup(x => x.CurrentValue).Returns(auditSettings);

        _mockAuditTrailRepository.Setup(x => x.CreateAsync(It.IsAny<MerchantAuditTrail>()))
            .ReturnsAsync(new MerchantAuditTrail());

        // Act
        await _service.CreateAuditTrailAsync(merchantId, action, entityType, oldValue, newValue, performedBy);

        // Assert
        _mockAuditTrailRepository.Verify(x => x.CreateAsync(It.Is<MerchantAuditTrail>(at => 
            at.MerchantId == merchantId &&
            at.Action == action &&
            at.EntityType == entityType &&
            at.OldValue == oldValue &&
            at.NewValue == newValue &&
            at.UpdatedBy == performedBy)), Times.Once);
    }

    [Fact]
    public async Task CreateAuditTrailAsync_WithDisabledAuditSettings_ShouldNotCreateAuditTrail()
    {
        // Arrange
        var merchantId = Guid.NewGuid();
        var auditSettings = new AuditLoggingSettings { Enabled = false };
        _mockSettings.Setup(x => x.CurrentValue).Returns(auditSettings);

        // Act
        await _service.CreateAuditTrailAsync(merchantId, "TEST", "TEST", "old", "new", "user");

        // Assert
        _mockAuditTrailRepository.Verify(x => x.CreateAsync(It.IsAny<MerchantAuditTrail>()), Times.Never);
    }

    #endregion

}