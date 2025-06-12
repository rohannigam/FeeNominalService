using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Services;

namespace FeeNominalService.Data
{
    public static class DbSeeder
    {
        public static async Task SeedDatabaseAsync(ApplicationDbContext context)
        {
            try
            {
                // Check if we already have data
                if (await context.MerchantStatuses.AnyAsync())
                {
                    return; // Database already seeded
                }

                // Add merchant statuses
                var statuses = new[]
                {
                    new MerchantStatus { MerchantStatusId = MerchantStatusIds.Suspended, Code = "SUSPENDED", Name = "Suspended", Description = "Merchant is temporarily suspended" },
                    new MerchantStatus { MerchantStatusId = MerchantStatusIds.Inactive, Code = "INACTIVE", Name = "Inactive", Description = "Merchant is inactive and cannot process transactions" },
                    new MerchantStatus { MerchantStatusId = MerchantStatusIds.Unknown, Code = "UNKNOWN", Name = "Unknown", Description = "Merchant status is unknown" },
                    new MerchantStatus { MerchantStatusId = MerchantStatusIds.Active, Code = "ACTIVE", Name = "Active", Description = "Merchant is active and can process transactions" },
                    new MerchantStatus { MerchantStatusId = MerchantStatusIds.Pending, Code = "PENDING", Name = "Pending", Description = "Merchant is pending activation" },
                    new MerchantStatus { MerchantStatusId = MerchantStatusIds.Verified, Code = "VERIFIED", Name = "Verified", Description = "Merchant is verified and active" }
                };

                await context.MerchantStatuses.AddRangeAsync(statuses);
                await context.SaveChangesAsync();

                // Add test merchant
                var activeStatus = await context.MerchantStatuses.FirstOrDefaultAsync(s => s.Code == "ACTIVE");
                if (activeStatus != null)
                {
                    var merchant = new Merchant
                    {
                        ExternalMerchantId = "DEV001",
                        Name = "Development Merchant",
                        StatusId = activeStatus.MerchantStatusId,
                        CreatedBy = "admin"
                    };

                    await context.Merchants.AddAsync(merchant);
                    await context.SaveChangesAsync();

                    // Add test API key
                    var apiKey = new ApiKey
                    {
                        MerchantId = merchant.MerchantId,
                        Key = "test_api_key",
                        Description = "Test API Key",
                        RateLimit = 1000,
                        AllowedEndpoints = new[] { "surchargefee/calculate", "surchargefee/calculate-batch" },
                        Status = "ACTIVE",
                        ExpiresAt = DateTime.UtcNow.AddDays(30),
                        CreatedBy = "admin"
                    };

                    await context.ApiKeys.AddAsync(apiKey);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - we don't want to prevent the application from starting
                Console.WriteLine($"Error seeding database: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
            }
        }
    }
} 