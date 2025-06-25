# Evolve Database Migration - Version 1.0.0

This directory contains the database migration scripts for version 1.0.0 of the Fee Nominal Service. The migrations are managed using Evolve, a database migration tool that provides version control for database schemas.

## Directory Structure

```
1.0.0/
├── setup/                       # Initial database setup migrations
│   ├── M1_0_0_001__enable_dblink.sql
│   └── M1_0_0_002__initial_setup.sql
├── Rollback/                    # Rollback scripts for each migration
│   ├── U1_0_0_101__create_schema_rollback.sql
│   ├── U1_0_0_102__create_merchant_tables_rollback.sql
│   └── ...
└── README.md                    # This file
```

## Setup Strategy

The `setup` directory contains initial database setup migrations that must be run before the main application migrations. These scripts handle the foundational database infrastructure:

### Setup Migrations

1. **M1_0_0_001__enable_dblink.sql** - Enables the dblink extension to allow cross-database operations for automated database creation
2. **M1_0_0_002__initial_setup.sql** - Creates the database, roles, schema, and service users with appropriate privileges

### Running Setup

Setup migrations must be run manually before any application migrations. These are foundational database setup scripts that establish the infrastructure:

```bash
# Run setup migrations manually (not through Evolve)
psql -h <host> -U <user> -d <database> -f Data/Evolve/1.0.0/setup/M1_0_0_001__enable_dblink.sql
psql -h <host> -U <user> -d <database> -f Data/Evolve/1.0.0/setup/M1_0_0_002__initial_setup.sql

# Then run application migrations through Evolve
evolve migrate --location=Data/Evolve/1.0.0
```

### Setup Configuration

The setup scripts use configurable parameters for different environments. Key configurations include:
- Database name and schema
- Service user accounts (deployment and API users)
- Role-based access control
- Environment-specific settings

## Migration Strategy

### Running Migrations

To apply all migrations in this version:

```bash
evolve migrate --location=Data/Evolve/1.0.0
```

To apply a specific migration:

```bash
evolve migrate --location=Data/Evolve/1.0.0 --version=1.0.0.1
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
 1.0.0.11 | Add updated_by to audit trail  | 2024-03-20 10:15:00   | 00:00:05      | true
 1.0.0.10 | Alter API key secrets          | 2024-03-20 10:14:00   | 00:00:03      | true
 1.0.0.9  | Create test data              | 2024-03-20 10:13:00   | 00:00:10      | true
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

## Troubleshooting

If a migration fails:

1. Check the error message in the changelog table
2. Review the rollback script for the failed migration
3. Execute the rollback script if needed
4. Fix any issues in the migration script
5. Retry the migration

## Support

For issues or questions about database migrations, contact the database team or refer to the [Evolve documentation](https://evolve-db.netlify.app/). 