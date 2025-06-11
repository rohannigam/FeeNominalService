/*
Rollback: U1_0_0_104__create_transaction_tables_rollback.sql
Description: Drops all transaction tables created for transaction management
Dependencies: None
Changes:
- Drops transaction_statuses table
- Drops transactions table
- Drops transaction_audit_logs table
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_104__create_transaction_tables_rollback.sql which is a rollback of V1_0_0_4__create_transaction_tables...';
END $$;

-- Drop transaction_audit_logs table first (due to foreign key dependency)
DROP TABLE IF EXISTS fee_nominal.transaction_audit_logs;
DO $$
BEGIN
    RAISE NOTICE 'Dropped transaction_audit_logs table';
END $$;

-- Drop transactions table
DROP TABLE IF EXISTS fee_nominal.transactions;
DO $$
BEGIN
    RAISE NOTICE 'Dropped transactions table';
END $$;

-- Drop transaction_statuses table
DROP TABLE IF EXISTS fee_nominal.transaction_statuses;
DO $$
BEGIN
    RAISE NOTICE 'Dropped transaction_statuses table';
END $$;

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'transaction_statuses'
    ) THEN
        RAISE EXCEPTION 'transaction_statuses table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'transactions'
    ) THEN
        RAISE EXCEPTION 'transactions table was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM information_schema.tables WHERE table_schema = 'fee_nominal' AND table_name = 'transaction_audit_logs'
    ) THEN
        RAISE EXCEPTION 'transaction_audit_logs table was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all transaction tables were dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_104__create_transaction_tables_rollback.sql which is a rollback of V1_0_0_4__create_transaction_tables successfully';
END $$; 