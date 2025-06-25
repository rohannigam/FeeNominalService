/*
Rollback: U1_0_0_117__add_missing_provider_config_columns_rollback.sql
Description: Rolls back changes from V1_0_0_17__add_missing_provider_config_columns.sql
Dependencies: V1_0_0_17__add_missing_provider_config_columns.sql
Changes:
- Removes added columns: is_primary, rate_limit, rate_limit_period, timeout, retry_count, retry_delay, last_used_at, last_success_at, last_error_at, last_error_message, success_count, error_count, average_response_time
- Restores removed columns: api_version, created_by, updated_by
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_117__add_missing_provider_config_columns_rollback...';
END $$;

-- Drop indexes for columns being removed
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_is_primary;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_last_used_at;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_last_success_at;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_last_error_at;

DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes for columns being removed';
END $$;

-- Remove added columns
ALTER TABLE fee_nominal.surcharge_provider_configs 
DROP COLUMN IF EXISTS is_primary,
DROP COLUMN IF EXISTS rate_limit,
DROP COLUMN IF EXISTS rate_limit_period,
DROP COLUMN IF EXISTS timeout,
DROP COLUMN IF EXISTS retry_count,
DROP COLUMN IF EXISTS retry_delay,
DROP COLUMN IF EXISTS last_used_at,
DROP COLUMN IF EXISTS last_success_at,
DROP COLUMN IF EXISTS last_error_at,
DROP COLUMN IF EXISTS last_error_message,
DROP COLUMN IF EXISTS success_count,
DROP COLUMN IF EXISTS error_count,
DROP COLUMN IF EXISTS average_response_time;

DO $$
BEGIN
    RAISE NOTICE 'Removed added columns from surcharge_provider_configs table';
END $$;

-- Restore removed columns
ALTER TABLE fee_nominal.surcharge_provider_configs 
ADD COLUMN IF NOT EXISTS api_version VARCHAR(20) NOT NULL DEFAULT '1.0',
ADD COLUMN IF NOT EXISTS created_by VARCHAR(50) NOT NULL DEFAULT 'system',
ADD COLUMN IF NOT EXISTS updated_by VARCHAR(50) NOT NULL DEFAULT 'system';

DO $$
BEGIN
    RAISE NOTICE 'Restored removed columns to surcharge_provider_configs table';
END $$;

-- Verify rollback
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_schema = 'fee_nominal' 
               AND table_name = 'surcharge_provider_configs' 
               AND column_name = 'average_response_time') THEN
        RAISE EXCEPTION 'Column average_response_time was not removed successfully';
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_provider_configs' 
                   AND column_name = 'api_version') THEN
        RAISE EXCEPTION 'Column api_version was not restored successfully';
    END IF;
    
    RAISE NOTICE 'Verified rollback was successful';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_117__add_missing_provider_config_columns_rollback successfully';
END $$; 