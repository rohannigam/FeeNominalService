using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models;
using FeeNominalService.Models.SurchargeProvider;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace FeeNominalService.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly string _schema;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
            : base(options)
        {
            _schema = configuration.GetValue<string>("Database:Schema") ?? "public";
        }

        public DbSet<Merchant> Merchants { get; set; } = null!;
        public DbSet<MerchantStatus> MerchantStatuses { get; set; } = null!;
        public DbSet<ApiKey> ApiKeys { get; set; } = null!;
        public DbSet<ApiKeyUsage> ApiKeyUsages { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AuthenticationAttempt> AuthenticationAttempts { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<BatchTransaction> BatchTransactions { get; set; }
        public DbSet<SurchargeProvider> SurchargeProviders { get; set; } = null!;
        public DbSet<SurchargeProviderConfig> SurchargeProviderConfigs { get; set; } = null!;
        public DbSet<SurchargeProviderConfigHistory> SurchargeProviderConfigHistory { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure table names to match SQL schema
            modelBuilder.Entity<MerchantStatus>().ToTable("merchant_statuses");
            modelBuilder.Entity<Merchant>().ToTable("merchants");
            modelBuilder.Entity<ApiKey>().ToTable("api_keys");
            modelBuilder.Entity<ApiKeyUsage>().ToTable("api_key_usage");
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
            modelBuilder.Entity<Transaction>().ToTable("transactions");
            modelBuilder.Entity<BatchTransaction>().ToTable("batch_transactions");
            modelBuilder.Entity<AuthenticationAttempt>().ToTable("authentication_attempts");
            modelBuilder.Entity<SurchargeProvider>().ToTable("surcharge_providers");
            modelBuilder.Entity<SurchargeProviderConfig>().ToTable("surcharge_provider_configs");
            modelBuilder.Entity<SurchargeProviderConfigHistory>().ToTable("surcharge_provider_config_history");

            // Configure MerchantStatus
            modelBuilder.Entity<MerchantStatus>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("merchant_status_id");
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure Merchant
            modelBuilder.Entity<Merchant>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("merchant_id");
                entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(50).HasColumnName("external_id");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50).HasColumnName("created_by");
                entity.Property(e => e.StatusId).HasColumnName("status_id").IsRequired();
                entity.HasIndex(e => e.ExternalId).IsUnique();
                entity.HasOne(e => e.Status)
                    .WithMany(e => e.Merchants)
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure ApiKey
            modelBuilder.Entity<ApiKey>(entity =>
            {
                entity.ToTable("api_keys");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("api_key_id");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id");
                entity.Property(e => e.Key).HasColumnName("key").IsRequired();
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Status).HasColumnName("status").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.RateLimit).HasColumnName("rate_limit");
                entity.Property(e => e.AllowedEndpoints).HasColumnName("allowed_endpoints");
                entity.Property(e => e.Purpose).HasColumnName("purpose");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by");
                entity.Property(e => e.OnboardingReference).HasColumnName("onboarding_reference");
                entity.Property(e => e.LastRotatedAt).HasColumnName("last_rotated_at");
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.Property(e => e.ExpirationDays).HasColumnName("expiration_days");
            });

            // Configure ApiKeyUsage
            modelBuilder.Entity<ApiKeyUsage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("api_key_usage_id");
                entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45).HasColumnName("ip_address");
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.ApiKeyId).HasColumnName("api_key_id");
                entity.HasOne(e => e.ApiKey)
                    .WithMany(e => e.UsageRecords)
                    .HasForeignKey(e => e.ApiKeyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure AuditLog
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("audit_log_id");
                entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50).HasColumnName("entity_type");
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PerformedBy).IsRequired().HasMaxLength(50).HasColumnName("performed_by");
                entity.Property(e => e.IpAddress).HasMaxLength(45).HasColumnName("ip_address");
                entity.Property(e => e.OldValues)
                    .HasColumnType("jsonb")
                    .HasColumnName("old_values")
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => v == null ? null : JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())
                    );
                entity.Property(e => e.NewValues)
                    .HasColumnType("jsonb")
                    .HasColumnName("new_values")
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => v == null ? null : JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())
                    );
                entity.Property(e => e.AdditionalInfo)
                    .HasColumnType("jsonb")
                    .HasColumnName("additional_info")
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => v == null ? null : JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())
                    );
            });

            // Configure Transaction
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("transaction_id");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id");
                entity.HasIndex(e => e.MerchantId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure BatchTransaction
            modelBuilder.Entity<BatchTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("batch_transaction_id");
                entity.Property(e => e.BatchId).HasColumnName("batch_reference");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id");
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
                entity.HasIndex(e => e.BatchId).IsUnique();
            });

            // Configure AuthenticationAttempt
            modelBuilder.Entity<AuthenticationAttempt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("authentication_attempt_id");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id");
                entity.Property(e => e.ApiKeyId).HasColumnName("api_key_id");
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45).HasColumnName("ip_address");
                entity.Property(e => e.UserAgent).IsRequired().HasMaxLength(500).HasColumnName("user_agent");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasColumnName("status");
                entity.Property(e => e.AttemptedAt).IsRequired().HasColumnName("attempted_at");
                entity.Property(e => e.FailureReason).HasMaxLength(500).HasColumnName("failure_reason");
                entity.HasOne(e => e.Merchant)
                    .WithMany()
                    .HasForeignKey(e => e.MerchantId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.ApiKeyEntity)
                    .WithMany()
                    .HasForeignKey(e => e.ApiKeyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SurchargeProvider
            modelBuilder.Entity<SurchargeProvider>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("surcharge_provider_id");
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(255);
                entity.Property(e => e.AuthenticationType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CredentialsSchema)
                    .IsRequired()
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())!
                    );
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure SurchargeProviderConfig
            modelBuilder.Entity<SurchargeProviderConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("surcharge_provider_config_id");
                entity.Property(e => e.MerchantId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProviderId).IsRequired();
                entity.Property(e => e.ConfigName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Credentials)
                    .IsRequired()
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())!
                    );
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.IsPrimary).IsRequired();
                entity.Property(e => e.RateLimit);
                entity.Property(e => e.RateLimitPeriod);
                entity.Property(e => e.Timeout);
                entity.Property(e => e.RetryCount);
                entity.Property(e => e.RetryDelay);
                entity.Property(e => e.Metadata)
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => v == null ? null : JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())
                    );
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();
                entity.Property(e => e.LastUsedAt);
                entity.Property(e => e.LastSuccessAt);
                entity.Property(e => e.LastErrorAt);
                entity.Property(e => e.LastErrorMessage);
                entity.Property(e => e.SuccessCount);
                entity.Property(e => e.ErrorCount);
                entity.Property(e => e.AverageResponseTime);
                entity.HasOne(e => e.Provider)
                    .WithMany()
                    .HasForeignKey(e => e.ProviderId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasIndex(e => new { e.MerchantId, e.ProviderId, e.IsPrimary }).IsUnique();
            });

            // Configure SurchargeProviderConfigHistory
            modelBuilder.Entity<SurchargeProviderConfigHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("surcharge_provider_config_history_id");
                entity.Property(e => e.ConfigId).IsRequired();
                entity.Property(e => e.ChangedAt).IsRequired();
                entity.Property(e => e.ChangedBy).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ChangeType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.ChangeReason);
                entity.Property(e => e.PreviousValues)
                    .IsRequired()
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())!
                    );
                entity.Property(e => e.NewValues)
                    .IsRequired()
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())!
                    );
                entity.HasOne(e => e.Config)
                    .WithMany()
                    .HasForeignKey(e => e.ConfigId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 