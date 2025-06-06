# Evolve Database Migrations

This directory contains the setup for running Evolve database migrations locally.

## Prerequisites

- Docker
- Docker Compose
- .NET SDK 6.0 or later

## Directory Structure

```
Data/Evolve/
├── docker-compose.yml    # Docker Compose configuration for PostgreSQL
├── migrations/          # SQL migration files
├── deploy.ps1          # Deployment script for Jenkins
└── README.md           # This file
```

## Getting Started

1. Install Evolve CLI tool:
   ```bash
   dotnet tool install --global Evolve.Tool
   ```

2. Start the PostgreSQL database:
   ```bash
   cd Data/Evolve
   docker-compose up -d
   ```

3. Run migrations:
   ```bash
   evolve migrate -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres" -l migrations
   ```

## Jenkins Deployment

The `deploy.ps1` script handles the deployment process in Jenkins:

1. Installs the Evolve CLI tool
2. Starts the PostgreSQL container
3. Waits for PostgreSQL to be ready
4. Runs the migrations

To use in Jenkins:

1. Add a PowerShell build step:
   ```powershell
   cd Data/Evolve
   .\deploy.ps1
   ```

2. Ensure the Jenkins agent has:
   - Docker installed and running
   - .NET SDK 6.0 or later installed
   - PowerShell execution policy allowing scripts

3. Configure environment variables in Jenkins if needed:
   - `POSTGRES_DB`
   - `POSTGRES_USER`
   - `POSTGRES_PASSWORD`

## Working with Migrations

### Adding New Migrations

1. Create a new SQL file in the `migrations` directory
2. Name it with the format: `V{version}__{description}.sql`
   Example: `V001__create_merchant_tables.sql`

### Running Migrations

To run migrations manually:

```bash
evolve migrate -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres" -l migrations
```

### Checking Migration Status

```bash
evolve info -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres" -l migrations
```

### Rolling Back

To roll back the last migration:

```bash
evolve rollback -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres" -l migrations
```

## Troubleshooting

1. If migrations fail, check the error message in the console

2. To reset the database:
   ```bash
   # Stop and remove containers and volumes
   docker-compose down -v
   
   # Start fresh
   docker-compose up -d
   
   # Run migrations
   evolve migrate -c "Host=localhost;Port=5432;Database=feenominal;Username=postgres;Password=postgres" -l migrations
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
4. Include both up and down migrations in each file
5. Version control all migration scripts
6. Document complex migrations with comments 