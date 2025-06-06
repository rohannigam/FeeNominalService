# Local Database Testing with Evolve

This guide explains how to run and test database migrations locally using Docker and Evolve.

## Prerequisites

1. Docker Desktop installed and running
2. PowerShell (for Windows)
3. PostgreSQL client tools (for health checks)
4. .NET SDK

## Setup and Running

1. **Start the PostgreSQL Container**

   ```powershell
   docker-compose -f docker-compose.local.yml up -d
   ```

   This will:
   - Start a PostgreSQL 15 container
   - Create a database named `fee_nominal`
   - Create a user `feenominal_user`
   - Set up a persistent volume for data

2. **Run Migrations**

   ```powershell
   .\run-local-migrations.ps1
   ```

   This script will:
   - Check for and install Evolve CLI if needed
   - Add dotnet tools to PATH if necessary
   - Wait for PostgreSQL to be ready
   - Run all pending migrations
   - Save migration logs

## Configuration

The local setup uses these default values:

- Database: `fee_nominal`
- User: `feenominal_user`
- Password: `feenominal_password`
- Port: `5432`
- Schema: `fee_nominal`

You can modify these values in:
- `docker-compose.local.yml` for database settings
- `run-local-migrations.ps1` for migration settings

## Useful Commands

1. **View Container Logs**
   ```powershell
   docker logs feenominal-db-local
   ```

2. **Connect to Database**
   ```powershell
   psql -h localhost -U feenominal_user -d fee_nominal
   ```

3. **Stop Container**
   ```powershell
   docker-compose -f docker-compose.local.yml down
   ```

4. **Remove Volume (Clean Start)**
   ```powershell
   docker-compose -f docker-compose.local.yml down -v
   ```

## Troubleshooting

1. **Port Already in Use**
   - Check if PostgreSQL is running locally
   - Change the port in `docker-compose.local.yml`

2. **Migration Failures**
   - Check the migration log file
   - Verify database connection
   - Ensure Evolve CLI is installed
   - Check if dotnet tools path is in system PATH

3. **Container Won't Start**
   - Check Docker logs
   - Verify no other container is using the same name
   - Check port availability

4. **Evolve Command Not Found**
   - Ensure .NET SDK is installed
   - Check if dotnet tools path is in system PATH
   - Try running `dotnet tool install --global Evolve.Cli` manually
   - Verify the tool is installed in `%USERPROFILE%\.dotnet\tools`

## Best Practices

1. **Before Testing**
   - Always start with a clean database
   - Remove volumes if needed
   - Check for pending migrations
   - Ensure PATH includes dotnet tools directory

2. **During Development**
   - Test migrations in order
   - Verify rollback scripts
   - Keep migration files in version control
   - Check migration logs for errors

3. **After Testing**
   - Clean up containers
   - Archive migration logs
   - Document any issues
   - Verify database state

## Additional Resources

- [Evolve Documentation](https://evolve-db.netlify.app/)
- [PostgreSQL Docker Image](https://hub.docker.com/_/postgres)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) 