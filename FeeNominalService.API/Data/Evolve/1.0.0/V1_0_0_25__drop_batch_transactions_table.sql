/*
Migration: V1_0_0_25__drop_batch_transactions_table.sql
Description: Drops legacy batch_transactions table no longer used by the service
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_25__drop_batch_transactions_table migration...';
END $$;

SET search_path TO fee_nominal;

DROP TABLE IF EXISTS batch_transactions CASCADE;
DO $$ BEGIN RAISE NOTICE 'Dropped batch_transactions table'; END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_25__drop_batch_transactions_table migration successfully';
END $$; 