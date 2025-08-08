/*
Migration: V1_0_0_4__create_transaction_tables.sql
Description: (Removed transaction_statuses and related logic; see step 24 for drops)
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchants table)
Changes:
- (All transaction_statuses logic removed)
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_4__create_transaction_tables migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- (transaction_statuses table creation, inserts, and verification removed)

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_4__create_transaction_tables migration successfully';
END $$;
