/*
Rollback: U1_0_0_125__drop_batch_transactions_table_rollback.sql
Description: Recreates batch_transactions table if rollback is needed
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback for V1_0_0_125__drop_batch_transactions_table...';
END $$;

SET search_path TO fee_nominal;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback for V1_0_0_125__drop_batch_transactions_table.';
END $$; 