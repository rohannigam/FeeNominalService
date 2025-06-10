/*
Rollback: V1_0_0_105__create_audit_tables_rollback.sql
Description: Drops all audit tables created for logging
Dependencies: None
Changes:
- Drops audit_logs table
- Drops audit_log_details table
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of V1_0_0_105__create_audit_tables...';
END $$;

-- Drop audit_log_details table first (due to foreign key dependency)
DROP TABLE IF EXISTS fee_nominal.audit_log_details;
RAISE NOTICE 'Dropped audit_log_details table';

-- Drop audit_logs table
DROP TABLE IF EXISTS fee_nominal.audit_logs;
RAISE NOTICE 'Dropped audit_logs table';

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'audit_logs'
    ) THEN
        RAISE EXCEPTION 'audit_logs table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'audit_log_details'
    ) THEN
        RAISE EXCEPTION 'audit_log_details table was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all audit tables were dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback of V1_0_0_105__create_audit_tables successfully';
END $$; 