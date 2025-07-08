/*
Rollback: U1_0_0_102__create_merchant_tables_rollback.sql
Description: Rolls back the creation of merchant-related tables
Dependencies: V1_0_0_2__create_merchant_tables.sql
Changes:
- Drops merchant_audit_logs table
- Drops surcharge_provider_config_history table
- Drops surcharge_provider_configs table
- Drops surcharge_providers table
- Drops merchants table
- Drops merchant_statuses table
- Drops uuid-ossp extension
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_102__create_merchant_tables rollback...';
END $$;

-- Drop tables in reverse order of creation
DROP TABLE IF EXISTS fee_nominal.merchant_audit_logs CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchant_audit_logs table';
END $$;

DROP TABLE IF EXISTS fee_nominal.surcharge_provider_config_history CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped surcharge_provider_config_history table';
END $$;

DROP TABLE IF EXISTS fee_nominal.surcharge_provider_configs CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped surcharge_provider_configs table';
END $$;

-- Drop provider_type column if exists (for rollback of ProviderType addition)
ALTER TABLE IF EXISTS fee_nominal.surcharge_providers DROP COLUMN IF EXISTS provider_type;

DROP TABLE IF EXISTS fee_nominal.surcharge_providers CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped surcharge_providers table';
END $$;

DROP TABLE IF EXISTS fee_nominal.merchants CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchants table';
END $$;

DROP TABLE IF EXISTS fee_nominal.merchant_statuses CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchant_statuses table';
END $$;

-- Drop extension if it exists
DROP EXTENSION IF EXISTS "uuid-ossp";
DO $$
BEGIN
    RAISE NOTICE 'Dropped uuid-ossp extension';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_102__create_merchant_tables rollback successfully';
END $$; 