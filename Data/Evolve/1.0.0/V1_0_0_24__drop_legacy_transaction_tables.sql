/*
Migration: V1_0_0_24__drop_legacy_transaction_tables.sql
Description: Drops legacy transaction tables no longer used by the service
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_24__drop_legacy_transaction_tables migration...';
END $$;

SET search_path TO fee_nominal;

DROP TABLE IF EXISTS transaction_audit_logs CASCADE;
DO $$ BEGIN RAISE NOTICE 'Dropped transaction_audit_logs table'; END $$;

DROP TABLE IF EXISTS transactions CASCADE;
DO $$ BEGIN RAISE NOTICE 'Dropped transactions table'; END $$;

DROP TABLE IF EXISTS transaction_statuses CASCADE;
DO $$ BEGIN RAISE NOTICE 'Dropped transaction_statuses table'; END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_24__drop_legacy_transaction_tables migration successfully';
END $$; 