# Evolve Database Migrations

This directory contains the setup for running Evolve database migrations locally.

## Prerequisites

- Docker
- Docker Compose
- .NET SDK 6.0 or later
- PostgreSQL client tools (for health checks)

## Directory Structure

```
Data/Evolve/
├── docker-compose.yml    # Docker Compose configuration for PostgreSQL
├── migrations/          # SQL migration files
│   ├── up/            # Forward migrations
│   ├── down/          # Rollback migrations
│   └── backup/        # Backup of original migrations
├── Documentation/      # Detailed documentation
│   └── EvolveMigrationGuide.md
└── README.md          # This file
```

## Getting Started

1. Install Evolve CLI tool:
   ```bash
   dotnet tool install --global Evolve.Cli
   ```

2. Start the PostgreSQL database:
   ```bash
   cd Data/Evolve
   docker-compose up -d
   ```

3. Run migrations:
   ```bash
   evolve migrate postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" -l migrations/up
   ```

4. Check migration status:
   ```bash
   evolve info postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal"
   ```

## Working with Migrations

### Adding New Migrations

1. Create a new SQL file in the `migrations/up` directory
2. Name it with the format: `V{version}__{description}.sql`
   Example: `V001__create_merchant_tables.sql`
3. Create corresponding rollback file in `migrations/down` directory
   Example: `V001__create_merchant_tables_down.sql`

### Running Migrations

```bash
# Run all pending migrations
evolve migrate postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" -l migrations/up

# Run with verbose output
evolve migrate postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" -l migrations/up --verbose

# Dry run (no actual changes)
evolve migrate postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" -l migrations/up --dry-run
```

### Checking Status

```bash
# Show current migration status
evolve info postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal"

# Show detailed status
evolve info postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" --verbose
```

### Rolling Back

```bash
# Rollback to specific version
.\rollback.ps1 -ConnectionString "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" -TargetVersion "V5"

# Rollback one step
.\rollback.ps1 -ConnectionString "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal"
```

## Troubleshooting

### Common Issues

1. **Connection Issues**:
   ```bash
   # Test connection
   evolve migrate postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" --dry-run
   ```

2. **Migration Failures**:
   ```bash
   # Check migration status
   evolve info postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" --verbose
   
   # Repair changelog if needed
   evolve repair postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal"
   ```

3. **Reset Database**:
   ```bash
   # Stop and remove containers and volumes
   docker-compose down -v

   # Start fresh
   docker-compose up -d

   # Run migrations
   evolve migrate postgresql -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres;SearchPath=fee_nominal" -l migrations/up
   ```

## Notes

- The PostgreSQL database is exposed on port 5432
- Database credentials:
  - Database: feenominal
  - Username: postgres
  - Password: postgres
  - Schema: fee_nominal

## Best Practices

1. Always test migrations in development before applying to production
2. Keep migrations idempotent when possible
3. Use transactions for all migrations
4. Always create corresponding rollback files
5. Version control all migration scripts
6. Document complex migrations with comments

## Additional Resources

For detailed documentation, please refer to:
- [EvolveMigrationGuide.md](Documentation/EvolveMigrationGuide.md) - Comprehensive guide for migrations, rollbacks, and deployment 