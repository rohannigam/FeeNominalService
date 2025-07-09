-- V1_0_0_26__add_provider_type_column.sql
-- Add provider_type column to surcharge_providers table if it doesn't exist
-- This migration ensures the provider_type column is present for existing tables

DO $$
BEGIN
    -- Check if provider_type column exists
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_providers' 
        AND column_name = 'provider_type'
    ) THEN
        -- Add provider_type column
        ALTER TABLE fee_nominal.surcharge_providers 
        ADD COLUMN provider_type VARCHAR(50) NOT NULL DEFAULT 'INTERPAYMENTS';
        
        RAISE NOTICE 'Added provider_type column to surcharge_providers table';
    ELSE
        RAISE NOTICE 'provider_type column already exists in surcharge_providers table';
    END IF;
END$$;

-- Add index on provider_type column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_providers' 
        AND indexname = 'idx_surcharge_providers_provider_type'
    ) THEN
        -- Create index on provider_type column
        CREATE INDEX idx_surcharge_providers_provider_type ON fee_nominal.surcharge_providers(provider_type);
        
        RAISE NOTICE 'Added index on provider_type column';
    ELSE
        RAISE NOTICE 'Index on provider_type column already exists';
    END IF;
END$$;

-- Verify the column was added successfully
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_providers' 
        AND column_name = 'provider_type'
    ) THEN
        RAISE EXCEPTION 'provider_type column was not added successfully to surcharge_providers table';
    END IF;
    RAISE NOTICE 'Verified provider_type column exists in surcharge_providers table';
END$$;

-- Verify the index was created successfully
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_indexes 
        WHERE schemaname = 'fee_nominal' 
        AND tablename = 'surcharge_providers' 
        AND indexname = 'idx_surcharge_providers_provider_type'
    ) THEN
        RAISE EXCEPTION 'Index on provider_type column was not created successfully';
    END IF;
    RAISE NOTICE 'Verified index on provider_type column exists';
END$$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_26__add_provider_type_column migration successfully';
END$$; 