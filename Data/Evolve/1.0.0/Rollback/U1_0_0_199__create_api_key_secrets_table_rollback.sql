/*
Rollback: U1_0_0_199__create_api_key_secrets_table_rollback.sql
Description: Rolls back the creation of api_key_secrets table, indexes, constraints, triggers, and functions.
NOTE: This rollback is for DEV/LOCAL ONLY. Exclude or comment out for production deployments.
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback for V1_0_0_99__create_api_key_secrets_table (DEV/LOCAL ONLY)...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Drop trigger and function
DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON fee_nominal.api_key_secrets;
DROP FUNCTION IF EXISTS fee_nominal.update_updated_at_column();
DO $$
BEGIN
    RAISE NOTICE 'Dropped trigger and function';
END $$;

-- Drop indexes
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_status;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_is_revoked;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_scope;
DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes on api_key_secrets table';
END $$;

-- Drop check constraint
ALTER TABLE fee_nominal.api_key_secrets DROP CONSTRAINT IF EXISTS chk_api_key_secrets_scope_merchant_id;
ALTER TABLE fee_nominal.api_key_secrets DROP CONSTRAINT IF EXISTS fk_api_key_secrets_merchant;
DO $$
BEGIN
    RAISE NOTICE 'Dropped constraints on api_key_secrets table';
END $$;

-- Drop the table
DROP TABLE IF EXISTS fee_nominal.api_key_secrets;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_key_secrets table';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback for V1_0_0_99__create_api_key_secrets_table (DEV/LOCAL ONLY)';
END $$; 