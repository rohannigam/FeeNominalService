using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models;
using FeeNominalService.Models.SurchargeProvider;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System;

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
        public DbSet<SurchargeProvider> SurchargeProviders { get; set; } = null!;
        public DbSet<SurchargeProviderConfig> SurchargeProviderConfigs { get; set; } = null!;
        public DbSet<SurchargeProviderConfigHistory> SurchargeProviderConfigHistory { get; set; } = null!;
        public DbSet<SurchargeProviderStatus> SurchargeProviderStatuses { get; set; } = null!;
        public DbSet<ApiKeySecret> ApiKeySecrets { get; set; }
        public DbSet<MerchantAuditTrail> MerchantAuditTrail { get; set; } = null!;
        public DbSet<SupportedProvider> SupportedProviders { get; set; } = null!;
        public DbSet<SurchargeTransaction> SurchargeTransactions { get; set; } = null!;
        public DbSet<AuditLogDetail> AuditLogDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Set schema for all entities
            modelBuilder.HasDefaultSchema("fee_nominal");

            // Configure table names to match SQL schema
            modelBuilder.Entity<MerchantStatus>().ToTable("merchant_statuses");
            modelBuilder.Entity<Merchant>().ToTable("merchants");
            modelBuilder.Entity<ApiKey>().ToTable("api_keys");
            modelBuilder.Entity<ApiKeyUsage>().ToTable("api_key_usage");
            modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
            // modelBuilder.Entity<Transaction>().ToTable("transactions"); // Removed legacy transactions table
            // modelBuilder.Entity<BatchTransaction>().ToTable("batch_transactions"); // Removed legacy batch_transactions table
            modelBuilder.Entity<AuthenticationAttempt>().ToTable("authentication_attempts");
            modelBuilder.Entity<SurchargeProvider>().ToTable("surcharge_providers");
            modelBuilder.Entity<SurchargeProviderConfig>().ToTable("surcharge_provider_configs");
            modelBuilder.Entity<SurchargeProviderConfigHistory>().ToTable("surcharge_provider_config_history");
            modelBuilder.Entity<ApiKeySecret>().ToTable("api_key_secrets");
            modelBuilder.Entity<MerchantAuditTrail>().ToTable("merchant_audit_trail", schema: "fee_nominal");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.MerchantAuditTrailId).HasColumnName("merchant_audit_trail_id");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.MerchantId).HasColumnName("merchant_id");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.Action).HasColumnName("action");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.EntityType).HasColumnName("entity_type");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.PropertyName).HasColumnName("property_name");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.OldValue).HasColumnName("old_value");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.NewValue).HasColumnName("new_value");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<MerchantAuditTrail>().Property(e => e.UpdatedBy).IsRequired().HasMaxLength(50).HasColumnName("updated_by");

            // Configure MerchantAuditTrail
            modelBuilder.Entity<MerchantAuditTrail>(entity =>
            {
                entity.HasKey(e => e.MerchantAuditTrailId);
                entity.Property(e => e.MerchantAuditTrailId).HasColumnName("merchant_audit_trail_id");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id").IsRequired();
                entity.Property(e => e.Action).HasColumnName("action").IsRequired().HasMaxLength(50);
                entity.Property(e => e.EntityType).HasColumnName("entity_type").IsRequired().HasMaxLength(50);
                entity.Property(e => e.PropertyName).HasColumnName("property_name").HasMaxLength(255);
                entity.Property(e => e.OldValue).HasColumnName("old_value");
                entity.Property(e => e.NewValue).HasColumnName("new_value");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").IsRequired().HasMaxLength(50);

                entity.HasOne(e => e.Merchant)
                    .WithMany()
                    .HasForeignKey(e => e.MerchantId)
                    .HasPrincipalKey(e => e.MerchantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure MerchantStatus
            modelBuilder.Entity<MerchantStatus>(entity =>
            {
                entity.ToTable("merchant_statuses", _schema);
                entity.HasKey(e => e.MerchantStatusId);
                entity.Property(e => e.MerchantStatusId)
                    .HasColumnName("merchant_status_id")
                    .ValueGeneratedNever(); // Since we're using predefined integer values
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.Description)
                    .HasMaxLength(255);
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure Merchant
            modelBuilder.Entity<Merchant>(entity =>
            {
                entity.ToTable("merchants", _schema);
                entity.HasKey(e => e.MerchantId);
                entity.Property(e => e.MerchantId)
                    .IsRequired()
                    .HasColumnType("uuid")
                    .HasColumnName("merchant_id");
                entity.Property(e => e.ExternalMerchantId)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("external_merchant_id");
                entity.Property(e => e.ExternalMerchantGuid)
                    .HasColumnName("external_merchant_guid")
                    .HasColumnType("uuid");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(e => e.StatusId)
                    .HasColumnName("status_id")
                    .IsRequired();
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("created_by");
                entity.HasIndex(e => e.ExternalMerchantId).IsUnique();
                entity.HasIndex(e => e.ExternalMerchantGuid).IsUnique();
                entity.HasOne(e => e.Status)
                    .WithMany(e => e.Merchants)
                    .HasForeignKey(e => e.StatusId)
                    .HasPrincipalKey(e => e.MerchantStatusId)
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
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.Status).HasColumnName("status").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.RateLimit).HasColumnName("rate_limit").IsRequired();
                entity.Property(e => e.AllowedEndpoints).HasColumnName("allowed_endpoints").IsRequired();
                entity.Property(e => e.Purpose).HasColumnName("purpose");
                entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
                entity.Property(e => e.OnboardingReference).HasColumnName("onboarding_reference");
                entity.Property(e => e.OnboardingTimestamp).HasColumnName("onboarding_timestamp");
                entity.Property(e => e.LastRotatedAt).HasColumnName("last_rotated_at");
                entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.Property(e => e.ExpirationDays).HasColumnName("expiration_days").IsRequired();
                entity.Property(e => e.IsAdmin).HasColumnName("is_admin").IsRequired();
                entity.Property(e => e.Scope).HasColumnName("scope");
                entity.Property(e => e.IsActiveInDb).HasColumnName("is_active").IsRequired();
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasOne(e => e.Merchant)
                    .WithMany(e => e.ApiKeys)
                    .HasForeignKey(e => e.MerchantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ApiKeyUsage
            modelBuilder.Entity<ApiKeyUsage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("api_key_usage_id");
                entity.Property(e => e.Endpoint).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45).HasColumnName("ip_address");
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.HttpMethod).IsRequired().HasMaxLength(10).HasColumnName("http_method");
                entity.Property(e => e.StatusCode).IsRequired().HasColumnName("status_code");
                entity.Property(e => e.ResponseTimeMs).IsRequired().HasColumnName("response_time_ms");
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
                entity.Property(e => e.Action).IsRequired().HasMaxLength(50).HasColumnName("action");
                entity.Property(e => e.EntityId).IsRequired().HasColumnName("entity_id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
            });

            // Configure AuditLogDetail
            modelBuilder.Entity<AuditLogDetail>().ToTable("audit_log_details");
            modelBuilder.Entity<AuditLogDetail>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("detail_id");
                entity.Property(e => e.AuditLogId).IsRequired().HasColumnName("audit_log_id");
                entity.Property(e => e.FieldName).IsRequired().HasMaxLength(255).HasColumnName("field_name");
                entity.Property(e => e.OldValue).HasColumnName("old_value");
                entity.Property(e => e.NewValue).HasColumnName("new_value");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
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

            // Configure SurchargeProviderStatus
            modelBuilder.Entity<SurchargeProviderStatus>(entity =>
            {
                entity.ToTable("surcharge_provider_statuses", _schema);
                entity.HasKey(e => e.StatusId);
                entity.Property(e => e.StatusId)
                    .HasColumnName("status_id")
                    .ValueGeneratedNever(); // Since we're using predefined integer values
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(20);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.Description)
                    .HasMaxLength(255);
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Configure SurchargeProvider
            modelBuilder.Entity<SurchargeProvider>(entity =>
            {
                entity.ToTable("surcharge_providers", _schema);
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("surcharge_provider_id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Code)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.Description);
                entity.Property(e => e.BaseUrl)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(e => e.AuthenticationType)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.CredentialsSchema)
                    .IsRequired();
                entity.Property(e => e.StatusId)
                    .IsRequired();
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.CreatedBy)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("created_by");
                entity.Property(e => e.UpdatedBy)
                    .IsRequired()
                    .HasMaxLength(50)
                    .HasColumnName("updated_by");

                entity.HasOne(e => e.Status)
                    .WithMany(e => e.Providers)
                    .HasForeignKey(e => e.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Configure the reverse relationship to prevent shadow properties
                entity.HasMany(e => e.Configurations)
                    .WithOne(c => c.Provider)
                    .HasForeignKey(c => c.ProviderId)
                    .HasPrincipalKey(e => e.Id)
                    .OnDelete(DeleteBehavior.Restrict);

                // Create composite unique index on code and created_by (merchant)
                entity.HasIndex(e => new { e.Code, e.CreatedBy }).IsUnique();
                
                // Also create a regular index on code for performance
                entity.HasIndex(e => e.Code);
            });

            // Configure SurchargeProviderConfig
            modelBuilder.Entity<SurchargeProviderConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("surcharge_provider_config_id");
                entity.Property(e => e.MerchantId).IsRequired().HasMaxLength(50).HasColumnName("merchant_id");
                entity.Property(e => e.ProviderId).IsRequired().HasColumnName("surcharge_provider_id");
                entity.Property(e => e.ConfigName).IsRequired().HasMaxLength(100).HasColumnName("config_name");
                entity.Property(e => e.Credentials)
                    .IsRequired()
                    .HasColumnName("credentials")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())!
                    );
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true).HasColumnName("is_active");
                entity.Property(e => e.IsPrimary).IsRequired().HasColumnName("is_primary");
                entity.Property(e => e.RateLimit).HasColumnName("rate_limit");
                entity.Property(e => e.RateLimitPeriod).HasColumnName("rate_limit_period");
                entity.Property(e => e.Timeout).HasColumnName("timeout");
                entity.Property(e => e.RetryCount).HasColumnName("retry_count");
                entity.Property(e => e.RetryDelay).HasColumnName("retry_delay");
                entity.Property(e => e.Metadata)
                    .HasColumnName("metadata")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        v => v == null ? null : JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                        v => v == null ? null : JsonSerializer.Deserialize<JsonDocument>(v, new JsonSerializerOptions())
                    );
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
                entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
                entity.Property(e => e.LastSuccessAt).HasColumnName("last_success_at");
                entity.Property(e => e.LastErrorAt).HasColumnName("last_error_at");
                entity.Property(e => e.LastErrorMessage).HasColumnName("last_error_message");
                entity.Property(e => e.SuccessCount).HasColumnName("success_count");
                entity.Property(e => e.ErrorCount).HasColumnName("error_count");
                entity.Property(e => e.AverageResponseTime).HasColumnName("average_response_time");
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50).HasColumnName("created_by");
                entity.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(50).HasColumnName("updated_by");
                
                // Explicitly ignore any shadow properties that might be created
                entity.Ignore("SurchargeProviderId");
                entity.Ignore("SurchargeProviderId1");
                    
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

            // Configure ApiKeySecret
            modelBuilder.Entity<ApiKeySecret>(entity =>
            {
                entity.ToTable("api_key_secrets");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ApiKey).HasColumnName("api_key").IsRequired();
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id"); // Allow NULL for admin secrets
                entity.Property(e => e.Secret).HasColumnName("secret").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
                entity.Property(e => e.LastRotated).HasColumnName("last_rotated");
                entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
                entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
                entity.Property(e => e.Scope).HasColumnName("scope");

                entity.HasIndex(e => e.ApiKey).IsUnique();
                entity.HasIndex(e => e.MerchantId);
            });

            // Configure SupportedProvider
            modelBuilder.Entity<SupportedProvider>(entity =>
            {
                entity.ToTable("supported_providers", _schema);
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("supported_provider_id")
                    .ValueGeneratedOnAdd();
                entity.Property(e => e.ProviderCode)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.ProviderName)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Description);
                entity.Property(e => e.IsActive)
                    .IsRequired()
                    .HasDefaultValue(true);
                entity.Property(e => e.IntegrationType)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.BaseUrlTemplate)
                    .HasMaxLength(255);
                entity.Property(e => e.AuthenticationType)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProviderCode).IsUnique();
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IntegrationType);
            });

            // Configure SurchargeTransaction
            modelBuilder.Entity<SurchargeTransaction>(entity =>
            {
                entity.ToTable("surcharge_trans", _schema);
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("surcharge_trans_id")
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");
                entity.Property(e => e.MerchantId)
                    .IsRequired()
                    .HasColumnType("uuid")
                    .HasColumnName("merchant_id");
                entity.Property(e => e.ProviderConfigId)
                    .IsRequired()
                    .HasColumnName("provider_config_id");
                entity.Property(e => e.OperationType)
                    .IsRequired()
                    .HasColumnName("operation_type")
                    .HasConversion(
                        v => v.ToString().ToLowerInvariant(),
                        v => (SurchargeOperationType)Enum.Parse(typeof(SurchargeOperationType), v, true)
                    )
                    .HasColumnType("varchar(20)");
                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasColumnName("status")
                    .HasConversion(
                        v => v.ToString().ToLowerInvariant(),
                        v => (SurchargeTransactionStatus)Enum.Parse(typeof(SurchargeTransactionStatus), v, true)
                    )
                    .HasColumnType("varchar(20)");
                entity.Property(e => e.Amount)
                    .IsRequired()
                    .HasColumnName("amount");
                entity.Property(e => e.CorrelationId)
                    .IsRequired()
                    .HasMaxLength(255)
                    .HasColumnName("correlation_id");
                entity.Property(e => e.ProviderTransactionId)
                    .HasMaxLength(255)
                    .HasColumnName("provider_transaction_id");
                entity.Property(e => e.RequestPayload)
                    .IsRequired()
                    .HasColumnType("jsonb")
                    .HasColumnName("request_payload");
                entity.Property(e => e.ResponsePayload)
                    .HasColumnType("jsonb")
                    .HasColumnName("response_payload");
                entity.Property(e => e.ErrorMessage)
                    .HasColumnName("error_message");
                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt)
                    .IsRequired()
                    .HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.ProcessedAt)
                    .HasColumnName("processed_at");

                entity.HasIndex(e => e.MerchantId);
                entity.HasIndex(e => e.ProviderConfigId);
                entity.HasIndex(e => e.OperationType);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.CorrelationId);
                entity.HasIndex(e => e.ProviderTransactionId);
            });
        }
    }
} 