/*
Rollback: U1_0_0_120__add_provider_transaction_id_rollback.sql
Description: Removes provider_transaction_id column from surcharge_trans table
Dependencies: 
- V1_0_0_20__add_provider_transaction_id.sql (rolls back this migration)
Changes:
- Removes provider_transaction_id column
- Removes associated index
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_120__add_provider_transaction_id_rollback...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Drop index first
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_trans_provider_transaction_id;

DO $$
BEGIN
    RAISE NOTICE 'Dropped index on provider_transaction_id';
END $$;

-- Remove provider_transaction_id column
ALTER TABLE fee_nominal.surcharge_trans 
DROP COLUMN IF EXISTS provider_transaction_id;

DO $$
BEGIN
    RAISE NOTICE 'Removed provider_transaction_id column from surcharge_trans table';
END $$;

-- Verify column was removed
DO $$ 
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_trans' 
        AND column_name = 'provider_transaction_id'
    ) THEN
        RAISE EXCEPTION 'Column provider_transaction_id was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified provider_transaction_id column removal';
END $$;

-- Verify index was removed
DO $$ 
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_trans' 
        AND indexname = 'idx_surcharge_trans_provider_transaction_id'
    ) THEN
        RAISE EXCEPTION 'Index idx_surcharge_trans_provider_transaction_id was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified index removal';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_120__add_provider_transaction_id_rollback successfully';
END $$; 