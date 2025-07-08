/*
Migration: V1_0_0_18__update_provider_code_length.sql
Description: Updates provider code length from VARCHAR(20) to VARCHAR(100) to support new naming convention
Dependencies: 
- V1_0_0_14__create_supported_providers_table.sql
- V1_0_0_2__create_merchant_tables.sql
Changes:
- Updates provider_code column in supported_providers table from VARCHAR(50) to VARCHAR(100)
- Updates code column in surcharge_providers table from VARCHAR(20) to VARCHAR(100)
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_18__update_provider_code_length migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Update supported_providers table provider_code column
ALTER TABLE fee_nominal.supported_providers 
ALTER COLUMN provider_code TYPE VARCHAR(100);

DO $$
BEGIN
    RAISE NOTICE 'Updated supported_providers.provider_code column to VARCHAR(100)';
END $$;

-- Update surcharge_providers table code column
ALTER TABLE fee_nominal.surcharge_providers 
ALTER COLUMN code TYPE VARCHAR(100);

DO $$
BEGIN
    RAISE NOTICE 'Updated surcharge_providers.code column to VARCHAR(100)';
END $$;

-- Verify the changes
DO $$ 
BEGIN
    -- Check supported_providers table
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'supported_providers' 
                   AND column_name = 'provider_code'
                   AND character_maximum_length = 100) THEN
        RAISE EXCEPTION 'Column supported_providers.provider_code was not updated to VARCHAR(100)';
    END IF;
    
    -- Check surcharge_providers table
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_providers' 
                   AND column_name = 'code'
                   AND character_maximum_length = 100) THEN
        RAISE EXCEPTION 'Column surcharge_providers.code was not updated to VARCHAR(100)';
    END IF;
    
    RAISE NOTICE 'Verified all provider code columns were updated to VARCHAR(100) successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_18__update_provider_code_length migration successfully';
END $$; 