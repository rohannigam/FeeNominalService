/*
Migration: V1_0_0_8__create_test_data.sql
Description: Placeholder migration - test data creation moved to manual setup
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchant tables)
- V1_0_0_3__create_api_key_tables.sql (requires api_key tables)
Changes:
- No changes - test data creation moved to manual setup step
- See: Data/Evolve/1.0.0/setup/M1_0_0_004__create_test_data_DevOnly.sql
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_8__create_test_data migration (placeholder)...';
    RAISE NOTICE 'Test data creation has been moved to manual setup step.';
    RAISE NOTICE 'For development environments, run: Data/Evolve/1.0.0/setup/M1_0_0_004__create_test_data_DevOnly.sql';
END $$;

-- Set search path
SET search_path TO fee_nominal;

DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- This migration is intentionally empty
-- Test data creation has been moved to manual setup step for development environments
-- See: Data/Evolve/1.0.0/setup/M1_0_0_004__create_test_data_DevOnly.sql

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_8__create_test_data migration (placeholder)';
    RAISE NOTICE 'No test data was created - this is intentional for production environments';
END $$; 