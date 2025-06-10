/*
Rollback: V1_0_0_102__create_merchant_tables_rollback.sql
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
    RAISE NOTICE 'Starting rollback of V1_0_0_102__create_merchant_tables...';
END $$;

-- Drop merchant_audit_logs table first (due to foreign key dependency)
DROP TABLE IF EXISTS fee_nominal.merchant_audit_logs;
RAISE NOTICE 'Dropped merchant_audit_logs table';

-- Drop surcharge_provider_config_history table
DROP TABLE IF EXISTS fee_nominal.surcharge_provider_config_history;
RAISE NOTICE 'Dropped surcharge_provider_config_history table';

-- Drop surcharge_provider_configs table
DROP TABLE IF EXISTS fee_nominal.surcharge_provider_configs;
RAISE NOTICE 'Dropped surcharge_provider_configs table';

-- Drop surcharge_providers table
DROP TABLE IF EXISTS fee_nominal.surcharge_providers;
RAISE NOTICE 'Dropped surcharge_providers table';

-- Drop merchants table
DROP TABLE IF EXISTS fee_nominal.merchants;
RAISE NOTICE 'Dropped merchants table';

-- Drop merchant_statuses table
DROP TABLE IF EXISTS fee_nominal.merchant_statuses;
RAISE NOTICE 'Dropped merchant_statuses table';

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
    RAISE NOTICE 'Completed rollback of V1_0_0_102__create_merchant_tables successfully';
END $$; 