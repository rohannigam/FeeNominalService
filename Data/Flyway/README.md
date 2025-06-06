# Flyway Database Migrations

This directory contains the Flyway-based database migrations for the Fee Nominal Service. The migrations are managed using Flyway and are executed through a Jenkins pipeline.

## Migration Files

The migrations are organized in the following order:

1. `V1__create_merchant_tables.sql` - Creates merchant-related tables
2. `V2__create_api_key_tables.sql` - Creates API key management tables
3. `V3__create_transaction_tables.sql` - Creates transaction and batch processing tables
4. `V4__create_audit_tables.sql` - Creates audit and logging tables
5. `V5__create_indexes.sql` - Creates all database indexes
6. `V6__create_functions.sql` - Creates database functions and triggers
7. `V7__create_test_data.sql` - Inserts test data (only if tables are empty)

## Running Migrations

### Prerequisites

- PostgreSQL database
- Flyway command-line tool
- Jenkins pipeline access

### Environment Variables

The following environment variables need to be set in Jenkins:

- `DB_HOST` - Database host
- `DB_PORT` - Database port (default: 5432)
- `DB_NAME` - Database name
- `DB_USER` - Database user
- `DB_PASSWORD` - Database password

### Jenkins Pipeline

The Jenkins pipeline will:

1. Download and set up Flyway
2. Validate the migration files
3. Run the migrations
4. Show migration status

### Manual Execution

To run migrations manually:

1. Install Flyway command-line tool
2. Set up the environment variables
3. Run the following command from this directory:
   ```bash
   flyway -configFiles=flyway.conf migrate
   ```

## Adding New Migrations

To add a new migration:

1. Create a new SQL file in the `migrations` directory
2. Name it following the pattern: `V{version}__{description}.sql`
3. Add your SQL statements
4. Commit and push the changes
5. The Jenkins pipeline will automatically run the new migration

## Best Practices

1. Always test migrations in a development environment first
2. Never modify existing migration files
3. Use transactions in migration files
4. Include rollback scripts when possible
5. Keep migrations idempotent
6. Use meaningful names for migration files
7. Document complex migrations with comments

## Troubleshooting

If migrations fail:

1. Check the Jenkins pipeline logs
2. Verify database connectivity
3. Ensure all required environment variables are set
4. Check for syntax errors in migration files
5. Verify database user permissions

## Rollback

To rollback a migration:

1. Create a new migration file with the rollback SQL
2. Name it following the pattern: `V{version}__rollback_{description}.sql`
3. Include the necessary SQL to undo the changes
4. Run the migration through Jenkins

Note: Flyway does not support automatic rollbacks. Always test rollback scripts thoroughly. 