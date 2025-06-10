/*
Rollback: V1_0_0_103__create_api_key_tables_rollback.sql
Description: Drops all API key tables created for API key management
Dependencies: None
Changes:
- Drops api_keys table
- Drops api_key_secrets table
- Drops api_key_audit_logs table
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of V1_0_0_103__create_api_key_tables...';
END $$;

-- Drop api_key_audit_logs table first (due to foreign key dependency)
DROP TABLE IF EXISTS fee_nominal.api_key_audit_logs;
RAISE NOTICE 'Dropped api_key_audit_logs table';

-- Drop api_key_secrets table
DROP TABLE IF EXISTS fee_nominal.api_key_secrets;
RAISE NOTICE 'Dropped api_key_secrets table';

-- Drop api_keys table
DROP TABLE IF EXISTS fee_nominal.api_keys;
RAISE NOTICE 'Dropped api_keys table';

-- Verify rollback
DO $$
BEGIN
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
    RAISE NOTICE 'Completed rollback of V1_0_0_103__create_api_key_tables successfully';
END $$; 