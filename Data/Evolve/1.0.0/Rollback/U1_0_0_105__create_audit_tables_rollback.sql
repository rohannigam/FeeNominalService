/*
Rollback: U1_0_0_105__create_audit_tables_rollback.sql
Description: Rolls back the creation of audit-related tables
Dependencies: V1_0_0_5__create_audit_tables.sql
Changes:
- Drops audit_log_details table
- Drops merchant_audit_trail table
- Drops audit_logs table
- Drops all related indexes
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_105__create_audit_tables rollback...';
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

-- Drop tables in reverse order of creation
DROP TABLE IF EXISTS fee_nominal.audit_log_details CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped audit_log_details table';
END $$;

DROP TABLE IF EXISTS fee_nominal.merchant_audit_trail CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped merchant_audit_trail table';
END $$;

DROP TABLE IF EXISTS fee_nominal.audit_logs CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped audit_logs table';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_105__create_audit_tables rollback successfully';
END $$; 