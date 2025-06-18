/*
Rollback: V1_0_0_12__create_api_key_usage_table_rollback.sql
Description: Drops the api_key_usage table and its indexes
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_12__create_api_key_usage_table rollback...';
END $$;

-- Drop indexes first
DROP INDEX IF EXISTS fee_nominal.idx_api_key_usage_api_key_id;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_usage_window;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_usage_endpoint;

DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes from api_key_usage table';
END $$;

-- Drop the table
DROP TABLE IF EXISTS fee_nominal.api_key_usage;

DO $$
BEGIN
    RAISE NOTICE 'Dropped api_key_usage table';
END $$;

-- Verify table is dropped
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_key_usage') THEN
        RAISE EXCEPTION 'Table api_key_usage was not dropped successfully';
    END IF;
    RAISE NOTICE 'Verified api_key_usage table removal';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_12__create_api_key_usage_table rollback successfully';
END $$; 