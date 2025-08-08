/*
Migration: V1_0_0_20__add_provider_transaction_id.sql
Description: Adds provider_transaction_id column to surcharge_trans table for storing Interpayments stxnId
Dependencies: 
- V1_0_0_13__create_surcharge_trans_table.sql (requires surcharge_trans table)
Changes:
- Adds provider_transaction_id column to store Interpayments transaction ID
- Adds index for performance on provider_transaction_id queries
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_20__add_provider_transaction_id migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Add provider_transaction_id column
ALTER TABLE fee_nominal.surcharge_trans 
ADD COLUMN IF NOT EXISTS provider_transaction_id VARCHAR(255);

DO $$
BEGIN
    RAISE NOTICE 'Added provider_transaction_id column to surcharge_trans table';
END $$;

-- Verify column was added
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_trans' 
        AND column_name = 'provider_transaction_id'
    ) THEN
        RAISE EXCEPTION 'Column provider_transaction_id was not added successfully';
    END IF;
    RAISE NOTICE 'Verified provider_transaction_id column creation';
END $$;

-- Create index for performance
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_provider_transaction_id ON fee_nominal.surcharge_trans(provider_transaction_id);

DO $$
BEGIN
    RAISE NOTICE 'Created index on provider_transaction_id';
END $$;

-- Verify index was created
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_trans' 
        AND indexname = 'idx_surcharge_trans_provider_transaction_id'
    ) THEN
        RAISE EXCEPTION 'Index idx_surcharge_trans_provider_transaction_id was not created successfully';
    END IF;
    RAISE NOTICE 'Verified index creation';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_20__add_provider_transaction_id migration successfully';
END $$; 