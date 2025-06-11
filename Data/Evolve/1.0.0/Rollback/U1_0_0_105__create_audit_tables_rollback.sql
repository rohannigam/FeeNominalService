/*
Rollback: U1_0_0_105__create_audit_tables_rollback.sql
Description: Drops all audit tables created for audit logging
Dependencies: None
Changes:
- Drops audit_logs table
- Drops audit_log_details table
- Drops merchant_audit_trail table
- Drops all related indexes
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_105__create_audit_tables_rollback.sql which is a rollback of V1_0_0_5__create_audit_tables...';
END $$;

-- Drop indexes first
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_entity_type_entity_id;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_audit_log_details_audit_log_id;
DROP INDEX IF EXISTS fee_nominal.idx_merchant_audit_trail_merchant_id;
DROP INDEX IF EXISTS fee_nominal.idx_merchant_audit_trail_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_merchant_audit_trail_action;
DROP INDEX IF EXISTS fee_nominal.idx_merchant_audit_trail_entity_type;
DROP INDEX IF EXISTS fee_nominal.idx_merchant_audit_trail_property_name;
DO $$
BEGIN
    RAISE NOTICE 'Dropped all audit-related indexes';
END $$;

-- Drop tables in correct order (due to foreign key dependencies)
DROP TABLE IF EXISTS fee_nominal.audit_log_details;
DO $$
BEGIN
    RAISE NOTICE 'Dropped audit_log_details table';
END $$;

DROP TABLE IF EXISTS fee_nominal.merchant_audit_trail;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchant_audit_trail table';
END $$;

DROP TABLE IF EXISTS fee_nominal.audit_logs;
DO $$
BEGIN
    RAISE NOTICE 'Dropped audit_logs table';
END $$;

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
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'merchant_audit_trail'
    ) THEN
        RAISE EXCEPTION 'merchant_audit_trail table was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all audit tables were dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_105__create_audit_tables_rollback.sql which is a rollback of V1_0_0_5__create_audit_tables successfully';
END $$; 