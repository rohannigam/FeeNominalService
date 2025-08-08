/*
Rollback: U1_0_0_104__create_transaction_tables_rollback.sql
Description: (No-op; all transaction-related tables removed from step 4)
Dependencies: V1_0_0_4__create_transaction_tables.sql
Changes:
- (All transaction_audit_logs and transaction_statuses logic removed)
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_104__create_transaction_tables rollback...';
END $$;

-- (No tables to drop; all logic removed)

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_104__create_transaction_tables rollback successfully';
END $$; 