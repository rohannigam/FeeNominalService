# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the application (development mode)
dotnet run

# Run with specific profile
dotnet run --launch-profile https

# Publish for deployment
dotnet publish -c Release
```

### Database Setup
```bash
# Start PostgreSQL database with Docker Compose
cd Data/Evolve
docker-compose up -d

# Run database migrations using Evolve
evolve migrate --location=Data/Evolve/1.0.0

# Check migration status
evolve info --location=Data/Evolve/1.0.0

# Validate migrations without applying
evolve validate --location=Data/Evolve/1.0.0
```

### Development URLs
- **HTTP**: http://localhost:5292
- **HTTPS**: https://localhost:7139
- **Swagger UI**: https://localhost:7139/swagger (or /swagger on HTTP)

## Architecture

### Project Structure
This is a .NET 8 ASP.NET Core Web API for a fee nominal service that handles surcharge transactions for merchants. The architecture follows clean architecture principles with cleaSurhr separation of concerns.

### Core Components

**Controllers** (`Controllers/V1/`):
- `SurchargeController`: Handles surcharge transaction operations (auth, sale, refund, cancel)
- `SurchargeProviderController`: Manages surcharge provider configuration
- `OnboardingController`: Handles merchant onboarding and API key management
- `PingController`: Health check endpoint

**Services** (`Services/`):
- `SurchargeTransactionService`: Business logic for surcharge transactions
- `SurchargeProviderService`: Provider management and configuration
- `ApiKeyService`: API key generation, validation, and management
- `MerchantService`: Merchant account management
- `AuditService`: Audit trail and logging

**Data Layer** (`Data/`, `Repositories/`):
- `ApplicationDbContext`: Entity Framework Core context using PostgreSQL
- Repository pattern implementation for data access
- Database migrations managed by Evolve tool

**Authentication** (`Authentication/`):
- Custom API key authentication handler
- Claims-based authorization with endpoint access control
- Request signing service for secure communication

**Provider Adapters** (`Services/Adapters/`):
- `InterPaymentsAdapter`: Integration with InterPayments provider
- Factory pattern for provider selection and instantiation

### Database Schema
- **Schema**: `fee_nominal`
- **Database**: PostgreSQL 15
- **Migration Tool**: Evolve
- **Key Tables**: merchants, api_keys, surcharge_providers, surcharge_transactions, audit_logs

### Key Features
- **API Versioning**: v1 endpoints with versioned API explorer
- **Provider System**: Pluggable surcharge provider adapters
- **Audit Logging**: Comprehensive audit trail for all operations
- **Request Signing**: Secure request authentication and validation
- **Configuration Management**: Environment-based configuration with AWS Secrets Manager integration

### Dependencies
- **Entity Framework Core**: PostgreSQL data access
- **Serilog**: Structured logging
- **AWS SDK**: Secrets Manager integration
- **Swagger/OpenAPI**: API documentation
- **Dapper**: Additional data access for performance-critical operations

### Security
- API key-based authentication with endpoint-specific access control
- Request signing for tamper-proof communications
- Sensitive data masking in logs
- Role-based database access control

### Testing
No test project currently exists in the solution. When adding tests, consider:
- Unit tests for services and business logic
- Integration tests for API endpoints
- Database tests for repositories