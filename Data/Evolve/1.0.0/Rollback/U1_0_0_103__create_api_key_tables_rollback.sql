/*
Rollback: U1_0_0_103__create_api_key_tables_rollback.sql
Description: Drops all API key tables created for API key management
Dependencies: None
Changes:
- Drops api_key_statuses table
- Drops api_keys table
- Drops api_key_secrets table
- Drops api_key_audit_logs table
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_103__create_api_key_tables_rollback.sql which is a rollback of V1_0_0_3__create_api_key_tables...';
END $$;

-- Drop api_key_audit_logs table first (due to foreign key dependency)
DROP TABLE IF EXISTS fee_nominal.api_key_audit_logs;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_key_audit_logs table';
END $$;

-- Drop api_key_secrets table
DROP TABLE IF EXISTS fee_nominal.api_key_secrets;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_key_secrets table';
END $$;

-- Drop api_keys table
DROP TABLE IF EXISTS fee_nominal.api_keys;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_keys table';
END $$;

-- Drop api_key_statuses table
DROP TABLE IF EXISTS fee_nominal.api_key_statuses;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_key_statuses table';
END $$;

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'api_key_statuses'
    ) THEN
        RAISE EXCEPTION 'api_key_statuses table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'api_keys'
    ) THEN
        RAISE EXCEPTION 'api_keys table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'api_key_secrets'
    ) THEN
        RAISE EXCEPTION 'api_key_secrets table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'api_key_audit_logs'
    ) THEN
        RAISE EXCEPTION 'api_key_audit_logs table was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all API key tables were dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_103__create_api_key_tables_rollback.sql which is a rollback of V1_0_0_3__create_api_key_tables successfully';
END $$; 