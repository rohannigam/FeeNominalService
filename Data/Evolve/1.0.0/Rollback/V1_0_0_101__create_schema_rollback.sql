/*
Rollback: V1_0_0_101__create_schema_rollback.sql
Description: Drops the fee_nominal schema and all its objects
Dependencies: None
Changes:
- Drops the fee_nominal schema and all contained objects
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of V1_0_0_101__create_schema...';
END $$;

-- Drop the fee_nominal schema and all its objects
DROP SCHEMA IF EXISTS fee_nominal CASCADE;
RAISE NOTICE 'Dropped fee_nominal schema and all contained objects';

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_namespace WHERE nspname = 'fee_nominal'
    ) THEN
        RAISE EXCEPTION 'fee_nominal schema was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified fee_nominal schema was dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback of V1_0_0_101__create_schema successfully';
END $$; 