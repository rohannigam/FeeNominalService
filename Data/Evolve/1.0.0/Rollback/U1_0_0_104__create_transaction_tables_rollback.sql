/*
Rollback: U1_0_0_104__create_transaction_tables_rollback.sql
Description: Rolls back the creation of transaction-related tables
Dependencies: V1_0_0_4__create_transaction_tables.sql
Changes:
- Drops transaction_audit_logs table
- Drops transactions table
- Drops transaction_statuses table
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_104__create_transaction_tables rollback...';
END $$;

-- Drop tables in reverse order of creation
DROP TABLE IF EXISTS fee_nominal.transaction_audit_logs CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped transaction_audit_logs table';
END $$;

DROP TABLE IF EXISTS fee_nominal.transaction_statuses CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped transaction_statuses table';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_104__create_transaction_tables rollback successfully';
END $$; 