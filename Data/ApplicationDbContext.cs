using Microsoft.EntityFrameworkCore;
using FeeNominalService.Models.ApiKey;
using FeeNominalService.Models.Merchant;
using FeeNominalService.Models;
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

            // Configure MerchantStatus
            modelBuilder.Entity<MerchantStatus>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
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
                entity.Property(e => e.Id).HasColumnName("id");
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
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Key).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnName("name");
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
                entity.Property(e => e.ExpiresAt).IsRequired().HasColumnName("expires_at");
                entity.Property(e => e.AllowedEndpoints)
                    .IsRequired()
                    .HasColumnType("text[]")
                    .HasColumnName("allowed_endpoints");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id");
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasColumnName("status");
                entity.HasIndex(e => e.Key).IsUnique();
                entity.HasOne(e => e.Merchant)
                    .WithMany(m => m.ApiKeys)
                    .HasForeignKey(e => e.MerchantId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(e => e.UsageRecords)
                    .WithOne(e => e.ApiKey)
                    .HasForeignKey(e => e.ApiKeyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ApiKeyUsage
            modelBuilder.Entity<ApiKeyUsage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
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
                entity.Property(e => e.Id).HasColumnName("id");
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
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id");
                entity.HasIndex(e => e.MerchantId);
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configure BatchTransaction
            modelBuilder.Entity<BatchTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.BatchId).HasColumnName("batch_reference");
                entity.Property(e => e.MerchantId).HasColumnName("merchant_id");
                entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).IsRequired().HasColumnName("updated_at");
                entity.HasIndex(e => e.BatchId).IsUnique();
            });

            modelBuilder.Entity<AuthenticationAttempt>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Timestamp).IsRequired();
            });
        }
    }
} 