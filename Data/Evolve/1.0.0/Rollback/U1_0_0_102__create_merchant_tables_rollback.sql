/*
Rollback: U1_0_0_102__create_merchant_tables_rollback.sql
Description: Drops all merchant tables created for merchant management
Dependencies: None
Changes:
- Drops merchant_statuses table
- Drops merchants table
- Drops surcharge_providers table
- Drops surcharge_provider_configs table
- Drops surcharge_provider_config_history table
- Drops merchant_audit_logs table
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_102__create_merchant_tables_rollback.sql which is a rollback of V1_0_0_2__create_merchant_tables...';
END $$;

-- Drop merchant_audit_logs table first (due to foreign key dependency)
DROP TABLE IF EXISTS fee_nominal.merchant_audit_logs;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchant_audit_logs table';
END $$;

-- Drop surcharge_provider_config_history table
DROP TABLE IF EXISTS fee_nominal.surcharge_provider_config_history;
DO $$
BEGIN
    RAISE NOTICE 'Dropped surcharge_provider_config_history table';
END $$;

-- Drop surcharge_provider_configs table
DROP TABLE IF EXISTS fee_nominal.surcharge_provider_configs;
DO $$
BEGIN
    RAISE NOTICE 'Dropped surcharge_provider_configs table';
END $$;

-- Drop surcharge_providers table
DROP TABLE IF EXISTS fee_nominal.surcharge_providers;
DO $$
BEGIN
    RAISE NOTICE 'Dropped surcharge_providers table';
END $$;

-- Drop merchants table
DROP TABLE IF EXISTS fee_nominal.merchants;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchants table';
END $$;

-- Drop merchant_statuses table
DROP TABLE IF EXISTS fee_nominal.merchant_statuses;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchant_statuses table';
END $$;

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'merchant_statuses'
    ) THEN
        RAISE EXCEPTION 'merchant_statuses table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'merchants'
    ) THEN
        RAISE EXCEPTION 'merchants table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'surcharge_providers'
    ) THEN
        RAISE EXCEPTION 'surcharge_providers table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'surcharge_provider_configs'
    ) THEN
        RAISE EXCEPTION 'surcharge_provider_configs table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'surcharge_provider_config_history'
    ) THEN
        RAISE EXCEPTION 'surcharge_provider_config_history table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'merchant_audit_logs'
    ) THEN
        RAISE EXCEPTION 'merchant_audit_logs table was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all merchant tables were dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_102__create_merchant_tables_rollback.sql which is a rollback of V1_0_0_2__create_merchant_tables successfully';
END $$; 