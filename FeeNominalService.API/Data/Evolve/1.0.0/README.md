# Evolve Database Migration - Version 1.0.0

This directory contains the database migration scripts for version 1.0.0 of the Fee Nominal Service. The migrations are managed using Evolve, a database migration tool that provides version control for database schemas.

## Directory Structure

```
1.0.0/
├── setup/                       # Manual database setup scripts
│   ├── 01-run-admin-setup.sh
│   ├── 02-run-deploy-permissions.sh
│   ├── 03-run-api-permissions.sh
│   ├── M1_0_0_000__admin_setup.sql
│   ├── M1_0_0_001__grant_deploy_permissions.sql
│   └── M1_0_0_002__grant_api_permissions.sql
├── Rollback/                    # Rollback scripts for each migration
│   ├── U1_0_0_101__create_schema_rollback.sql
│   ├── U1_0_0_102__create_merchant_tables_rollback.sql
│   └── ...
└── README.md                    # This file
```

## Setup Strategy

The `setup` directory contains manual database setup scripts that must be run before the main application migrations. These scripts handle the foundational database infrastructure and user permissions.

### Setup Scripts

The setup process is automated through shell scripts that run in sequence:

1. **01-run-admin-setup.sh** - Runs `M1_0_0_000__admin_setup.sql` to create database roles and service users
2. **02-run-deploy-permissions.sh** - Runs `M1_0_0_001__grant_deploy_permissions.sql` to grant deployment user permissions
3. **03-run-api-permissions.sh** - Runs `M1_0_0_002__grant_api_permissions.sql` to grant API user permissions

### Running Setup

Setup scripts are designed to run during Docker container initialization. They execute automatically when the PostgreSQL container starts:

```bash
# The scripts run automatically in Docker environment
# For manual execution (if needed):
chmod +x Data/Evolve/1.0.0/setup/*.sh
./Data/Evolve/1.0.0/setup/01-run-admin-setup.sh
./Data/Evolve/1.0.0/setup/02-run-deploy-permissions.sh
./Data/Evolve/1.0.0/setup/03-run-api-permissions.sh

# Then run application migrations through Evolve
evolve migrate --location=Data/Evolve/1.0.0
```

### Setup Configuration

The setup scripts use environment variables for configuration:
- `POSTGRES_USER` - PostgreSQL superuser
- `POSTGRES_DB` - Target database name
- Service user accounts (deployment and API users) are created with appropriate privileges
- Role-based access control is established for security

## Migration Strategy

### Recent Changes

**Version 1.0.0.26**: Added comprehensive support for bulk operations and provider-agnostic functionality:
- Added `provider_type` column to `surcharge_providers` for provider-agnostic operations
- Added bulk operations support (`batch_id`, `merchant_transaction_id`, `sequence_number`) to `surcharge_trans`
- Added sale/refund/cancel support (`original_surcharge_trans_id`, `created_by`, `updated_by`) to `surcharge_trans`
- Added admin API key support (`is_admin`) to `api_keys`
- Created performance indexes for all new columns

**Version 1.0.0.24+**: Legacy transaction tables have been removed to clean up the database schema:
- Removed unused legacy transaction tables
- Cleaned up related indexes and constraints
- Updated schema to reflect current application state

### Migration Simplification

Recent migrations have been simplified by combining multiple small migrations into single, efficient migrations:
- **Before**: 5 separate migrations (V1_0_0_26 through V1_0_0_30) for bulk operations support
- **After**: 1 combined migration (V1_0_0_26) that adds all necessary columns and indexes
- **Benefits**: Reduced complexity, faster deployment, easier maintenance

### Running Migrations

To apply all migrations in this version:

```bash
evolve migrate --location=Data/Evolve/1.0.0
```

To apply a specific migration:

```bash
evolve migrate --location=Data/Evolve/1.0.0 --version=1.0.0.26
```

### Rollback Strategy

The `Rollback` directory contains scripts to reverse each migration. These scripts are numbered from 101 onwards to maintain a clear separation from forward migrations. Each rollback script:

1. Drops objects created in the corresponding migration
2. Includes verification steps to ensure complete rollback
3. Uses RAISE NOTICE statements for tracking progress

To execute a rollback:
Please refer to how Jenkins pipeline intends to do this via a script.

## Verification Commands

### Check Migration Status

To view the current state of migrations:

```bash
evolve info --location=Data/Evolve/1.0.0
```

This will show:
- Applied migrations
- Pending migrations
- Last applied version
- Current schema version

### View Changelog

The database maintains a changelog table that tracks all applied migrations. You can query it directly:

```sql
SELECT * FROM evolve.changelog ORDER BY installed_on DESC;
```

Example output:
```
 version  | description                    | installed_on           | execution_time | success
----------|--------------------------------|------------------------|----------------|---------
 1.0.0.26 | Add bulk operations support    | 2024-03-20 10:16:00   | 00:00:02      | true
 1.0.0.25 | Drop batch transactions table  | 2024-03-20 10:15:00   | 00:00:03      | true
 1.0.0.24 | Drop legacy transaction tables | 2024-03-20 10:14:00   | 00:00:01      | true
```

### Validate Migrations

To validate migrations without applying them:

```bash
evolve validate --location=Data/Evolve/1.0.0
```

This checks for:
- Syntax errors
- Missing dependencies
- Version conflicts

## Best Practices

1. **Always Test Rollbacks**: Before applying migrations in production, test the rollback scripts in a staging environment.

2. **Version Control**: Keep all migration and rollback scripts in version control. Never modify existing migrations.

3. **Verification**: Use the verification blocks in rollback scripts to ensure complete cleanup.

4. **Documentation**: Update this README when adding new migrations or changing the migration strategy.

5. **Setup Scripts**: Ensure setup scripts have proper permissions and are tested in the target environment.

6. **Migration Consolidation**: Combine related schema changes into single migrations to reduce complexity and improve deployment speed.

## Troubleshooting

If a migration fails:

1. Check the error message in the changelog table
2. Review the rollback script for the failed migration
3. Execute the rollback script if needed
4. Fix any issues in the migration script
5. Retry the migration

If setup scripts fail:

1. Verify environment variables are set correctly
2. Check PostgreSQL user permissions
3. Ensure scripts have execute permissions
4. Review PostgreSQL logs for detailed error messages

## Support

For issues or questions about database migrations, contact the database team or refer to the [Evolve documentation](https://evolve-db.netlify.app/). 