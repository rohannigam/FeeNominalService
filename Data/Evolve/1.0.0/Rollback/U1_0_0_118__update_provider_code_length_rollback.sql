/*
Rollback Migration: U1_0_0_118__update_provider_code_length_rollback.sql
Description: Reverts provider code length from VARCHAR(100) back to VARCHAR(50)
Dependencies: 
- V1_0_0_18__update_provider_code_length.sql
Changes:
- Reverts provider_code column in supported_providers table from VARCHAR(100) to VARCHAR(50)
- Reverts code column in surcharge_providers table from VARCHAR(100) to VARCHAR(50)
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_118__update_provider_code_length_rollback...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Check if any codes exceed 50 characters before rollback
DO $$
DECLARE
    long_codes_count INTEGER;
BEGIN
    -- Check supported_providers table
    SELECT COUNT(*) INTO long_codes_count 
    FROM fee_nominal.supported_providers 
    WHERE LENGTH(provider_code) > 50;
    
    IF long_codes_count > 0 THEN
        RAISE EXCEPTION 'Cannot rollback: % codes in supported_providers table exceed 50 characters', long_codes_count;
    END IF;
    
    -- Check surcharge_providers table
    SELECT COUNT(*) INTO long_codes_count 
    FROM fee_nominal.surcharge_providers 
    WHERE LENGTH(code) > 50;
    
    IF long_codes_count > 0 THEN
        RAISE EXCEPTION 'Cannot rollback: % codes in surcharge_providers table exceed 50 characters', long_codes_count;
    END IF;
    
    RAISE NOTICE 'All provider codes are within 50 character limit, proceeding with rollback';
END $$;

-- Revert supported_providers table provider_code column
ALTER TABLE fee_nominal.supported_providers 
ALTER COLUMN provider_code TYPE VARCHAR(50);

DO $$
BEGIN
    RAISE NOTICE 'Reverted supported_providers.provider_code column to VARCHAR(50)';
END $$;

-- Revert surcharge_providers table code column
ALTER TABLE fee_nominal.surcharge_providers 
ALTER COLUMN code TYPE VARCHAR(50);

DO $$
BEGIN
    RAISE NOTICE 'Reverted surcharge_providers.code column to VARCHAR(50)';
END $$;

-- Verify the rollback
DO $$ 
BEGIN
    -- Check supported_providers table
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'supported_providers' 
                   AND column_name = 'provider_code'
                   AND character_maximum_length = 50) THEN
        RAISE EXCEPTION 'Column supported_providers.provider_code was not reverted to VARCHAR(50)';
    END IF;
    
    -- Check surcharge_providers table
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_providers' 
                   AND column_name = 'code'
                   AND character_maximum_length = 50) THEN
        RAISE EXCEPTION 'Column surcharge_providers.code was not reverted to VARCHAR(50)';
    END IF;
    
    RAISE NOTICE 'All provider code columns were reverted to VARCHAR(50) successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_118__update_provider_code_length_rollback successfully';
END $$; 