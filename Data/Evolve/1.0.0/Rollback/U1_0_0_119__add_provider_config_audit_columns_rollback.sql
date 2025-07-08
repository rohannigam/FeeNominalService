/*
Rollback: U1_0_0_119__add_provider_config_audit_columns_rollback.sql
Description: Removes created_by and updated_by columns from surcharge_provider_configs table
Dependencies: 
- V1_0_0_19__add_provider_config_audit_columns.sql (rolls back this migration)
Changes:
- Removes created_by and updated_by columns
- Removes associated indexes
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_119__add_provider_config_audit_columns_rollback...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Drop indexes first
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_created_by;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_updated_by;

DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes on audit columns';
END $$;

-- Remove created_by and updated_by columns
ALTER TABLE fee_nominal.surcharge_provider_configs 
DROP COLUMN IF EXISTS created_by,
DROP COLUMN IF EXISTS updated_by;

DO $$
BEGIN
    RAISE NOTICE 'Removed created_by and updated_by columns from surcharge_provider_configs table';
END $$;

-- Verify columns were removed
DO $$ 
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_provider_configs' 
        AND column_name = 'created_by'
    ) THEN
        RAISE EXCEPTION 'Column created_by was not removed successfully';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_provider_configs' 
        AND column_name = 'updated_by'
    ) THEN
        RAISE EXCEPTION 'Column updated_by was not removed successfully';
    END IF;
    
    RAISE NOTICE 'Verified created_by and updated_by column removal';
END $$;

-- Verify indexes were removed
DO $$ 
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_provider_configs' 
        AND indexname = 'idx_surcharge_provider_configs_created_by'
    ) THEN
        RAISE EXCEPTION 'Index idx_surcharge_provider_configs_created_by was not removed successfully';
    END IF;
    
    IF EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_provider_configs' 
        AND indexname = 'idx_surcharge_provider_configs_updated_by'
    ) THEN
        RAISE EXCEPTION 'Index idx_surcharge_provider_configs_updated_by was not removed successfully';
    END IF;
    
    RAISE NOTICE 'Verified index removal';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_119__add_provider_config_audit_columns_rollback successfully';
END $$; 