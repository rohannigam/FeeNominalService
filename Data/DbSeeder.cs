using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models.ApiKey;

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
                    new MerchantStatus { Code = "ACTIVE", Name = "Active", Description = "Merchant is active and can process transactions" },
                    new MerchantStatus { Code = "INACTIVE", Name = "Inactive", Description = "Merchant is inactive and cannot process transactions" },
                    new MerchantStatus { Code = "SUSPENDED", Name = "Suspended", Description = "Merchant is temporarily suspended" },
                    new MerchantStatus { Code = "PENDING", Name = "Pending", Description = "Merchant is pending approval" },
                    new MerchantStatus { Code = "REJECTED", Name = "Rejected", Description = "Merchant application was rejected" },
                    new MerchantStatus { Code = "TERMINATED", Name = "Terminated", Description = "Merchant account has been terminated" }
                };

                await context.MerchantStatuses.AddRangeAsync(statuses);
                await context.SaveChangesAsync();

                // Add test merchant
                var activeStatus = await context.MerchantStatuses.FirstOrDefaultAsync(s => s.Code == "ACTIVE");
                if (activeStatus != null)
                {
                    var merchant = new Merchant
                    {
                        ExternalId = "DEV001",
                        Name = "Development Merchant",
                        StatusId = activeStatus.Id,
                        CreatedBy = "admin"
                    };

                    await context.Merchants.AddAsync(merchant);
                    await context.SaveChangesAsync();

                    // Add test API key
                    var apiKey = new ApiKey
                    {
                        MerchantId = merchant.Id,
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