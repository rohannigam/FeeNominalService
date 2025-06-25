/*
Migration: V1_0_0_17__add_missing_provider_config_columns.sql
Description: Adds missing columns to surcharge_provider_configs table to match Entity Framework model
Dependencies: 
- V1_0_0_2__create_merchant_tables.sql (requires surcharge_provider_configs table)
Changes:
- Adds missing columns: is_primary, rate_limit, rate_limit_period, timeout, retry_count, retry_delay, last_used_at, last_success_at, last_error_at, last_error_message, success_count, error_count, average_response_time
- Removes api_version column (not in EF model)
- Removes created_by and updated_by columns (not in EF model)
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_17__add_missing_provider_config_columns migration...';
END $$;

-- Add missing columns to surcharge_provider_configs table
ALTER TABLE fee_nominal.surcharge_provider_configs 
ADD COLUMN IF NOT EXISTS is_primary BOOLEAN NOT NULL DEFAULT false,
ADD COLUMN IF NOT EXISTS rate_limit INTEGER,
ADD COLUMN IF NOT EXISTS rate_limit_period INTEGER,
ADD COLUMN IF NOT EXISTS timeout INTEGER,
ADD COLUMN IF NOT EXISTS retry_count INTEGER,
ADD COLUMN IF NOT EXISTS retry_delay INTEGER,
ADD COLUMN IF NOT EXISTS last_used_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS last_success_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS last_error_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS last_error_message TEXT,
ADD COLUMN IF NOT EXISTS success_count INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS error_count INTEGER NOT NULL DEFAULT 0,
ADD COLUMN IF NOT EXISTS average_response_time DOUBLE PRECISION;

DO $$
BEGIN
    RAISE NOTICE 'Added missing columns to surcharge_provider_configs table';
END $$;

-- Remove columns that are not in the EF model
ALTER TABLE fee_nominal.surcharge_provider_configs 
DROP COLUMN IF EXISTS api_version,
DROP COLUMN IF EXISTS created_by,
DROP COLUMN IF EXISTS updated_by;

DO $$
BEGIN
    RAISE NOTICE 'Removed columns not in EF model from surcharge_provider_configs table';
END $$;

-- Verify the table structure
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_provider_configs' 
                   AND column_name = 'average_response_time') THEN
        RAISE EXCEPTION 'Column average_response_time was not added successfully';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_provider_configs' 
                   AND column_name = 'is_primary') THEN
        RAISE EXCEPTION 'Column is_primary was not added successfully';
    END IF;
    
    RAISE NOTICE 'Verified new columns were added successfully';
END $$;

-- Create additional indexes for new columns
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_is_primary ON fee_nominal.surcharge_provider_configs(is_primary);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_last_used_at ON fee_nominal.surcharge_provider_configs(last_used_at);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_last_success_at ON fee_nominal.surcharge_provider_configs(last_success_at);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_last_error_at ON fee_nominal.surcharge_provider_configs(last_error_at);

DO $$
BEGIN
    RAISE NOTICE 'Created additional indexes for new columns';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_17__add_missing_provider_config_columns migration successfully';
END $$; 