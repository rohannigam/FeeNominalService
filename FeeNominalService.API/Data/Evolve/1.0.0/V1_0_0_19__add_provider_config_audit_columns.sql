/*
Migration: V1_0_0_19__add_provider_config_audit_columns.sql
Description: Adds created_by and updated_by columns to surcharge_provider_configs table for audit tracking
Dependencies: 
- V1_0_0_17__add_missing_provider_config_columns.sql (requires surcharge_provider_configs table)
Changes:
- Adds created_by and updated_by columns for audit tracking
- Adds indexes for performance on audit columns
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_19__add_provider_config_audit_columns migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Add created_by and updated_by columns
ALTER TABLE fee_nominal.surcharge_provider_configs 
ADD COLUMN IF NOT EXISTS created_by VARCHAR(50) NOT NULL DEFAULT 'system',
ADD COLUMN IF NOT EXISTS updated_by VARCHAR(50) NOT NULL DEFAULT 'system';

DO $$
BEGIN
    RAISE NOTICE 'Added created_by and updated_by columns to surcharge_provider_configs table';
END $$;

-- Verify columns were added
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_provider_configs' 
        AND column_name = 'created_by'
    ) THEN
        RAISE EXCEPTION 'Column created_by was not added successfully';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_provider_configs' 
        AND column_name = 'updated_by'
    ) THEN
        RAISE EXCEPTION 'Column updated_by was not added successfully';
    END IF;
    
    RAISE NOTICE 'Verified created_by and updated_by column creation';
END $$;

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_created_by ON fee_nominal.surcharge_provider_configs(created_by);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_updated_by ON fee_nominal.surcharge_provider_configs(updated_by);

DO $$
BEGIN
    RAISE NOTICE 'Created indexes on audit columns';
END $$;

-- Verify indexes were created
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_provider_configs' 
        AND indexname = 'idx_surcharge_provider_configs_created_by'
    ) THEN
        RAISE EXCEPTION 'Index idx_surcharge_provider_configs_created_by was not created successfully';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_provider_configs' 
        AND indexname = 'idx_surcharge_provider_configs_updated_by'
    ) THEN
        RAISE EXCEPTION 'Index idx_surcharge_provider_configs_updated_by was not created successfully';
    END IF;
    
    RAISE NOTICE 'Verified index creation';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_19__add_provider_config_audit_columns migration successfully';
END $$; 