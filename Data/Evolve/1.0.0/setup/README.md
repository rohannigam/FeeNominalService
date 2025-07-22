# Database Setup Scripts

This directory contains setup scripts for initializing the FeeNominalService database in development environments.

## Setup Order

1. **01-run-admin-setup.sh** - Sets up admin API key generation
2. **02-run-deploy-permissions.sh** - Sets up database permissions
3. **03-run-api-permissions.sh** - Sets up API permissions

## System Merchant Pattern

The system uses a **System Merchant Pattern** for admin/system operations. This approach:

- Creates a dedicated "SYSTEM" merchant in the database
- Uses the existing merchant → provider → config architecture
- Provides clean separation between admin and merchant operations
- Follows KISS principles with no additional tables or complexity

### What happens automatically:
- When the application starts in development mode, `DbSeeder.SeedDatabaseAsync()` runs
- It creates a system merchant with ID `00000000-0000-0000-0000-000000000001`
- Creates an InterPayments provider for the system merchant
- Creates an InterPayments provider config with development credentials
- All admin operations use this system merchant's provider config

### Architecture Benefits:
- **No New Tables**: Uses existing `merchants`, `surcharge_providers`, `surcharge_provider_configs`
- **Clear Separation**: System merchant vs real merchants
- **Consistent Patterns**: Same provider config patterns for all operations
- **Easy to Understand**: One merchant = one provider ecosystem
- **Secure**: Admin operations are isolated to system merchant

### Usage:
The system merchant's InterPayments config is automatically used by:
- Bulk sale complete endpoint (`/api/v1/surcharge/bulk-sale-complete`)
- Other admin/system operations that require provider integration

### Production Considerations:
- Replace development credentials with real InterPayments credentials
- Update the baseUrl to point to the production InterPayments API
- Ensure proper security for credential storage
- Consider using AWS Secrets Manager for credential storage in production

## Running Setup

```bash
# Run all setup scripts
./01-run-admin-setup.sh
./02-run-deploy-permissions.sh
./03-run-api-permissions.sh

# The application will automatically create the system merchant on first run
```

## Clean Architecture Benefits

This setup supports the clean architecture design by:
- Separating admin/system operations from merchant-specific operations
- Using existing infrastructure without duplication
- Avoiding hardcoded merchant IDs for admin operations
- Providing clear separation of concerns between merchant and system contexts
- **Automatic setup** - no manual intervention required
- **KISS principle** - simple, maintainable design 