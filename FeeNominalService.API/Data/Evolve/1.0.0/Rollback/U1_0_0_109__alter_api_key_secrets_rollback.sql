/*
Rollback: U1_0_0_109__alter_api_key_secrets_rollback.sql
Description: Reverts changes made in V1_0_0_9__alter_api_key_secrets.sql
Changes:
- Removes updated_at column
- Removes expires_at column
- Reverts merchant_id column type back to VARCHAR(50)
- Removes foreign key constraint
- Removes trigger and function
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_109__alter_api_key_secrets rollback...';
END $$;

-- Drop trigger and function
DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON fee_nominal.api_key_secrets;
DROP FUNCTION IF EXISTS fee_nominal.update_updated_at_column();
DO $$
BEGIN
    RAISE NOTICE 'Dropped trigger and function';
END $$;

-- Remove foreign key constraint
ALTER TABLE fee_nominal.api_key_secrets
    DROP CONSTRAINT IF EXISTS fk_api_key_secrets_merchant;
DO $$
BEGIN
    RAISE NOTICE 'Removed foreign key constraint';
END $$;

-- Revert merchant_id to VARCHAR(50)
ALTER TABLE fee_nominal.api_key_secrets 
    ALTER COLUMN merchant_id TYPE VARCHAR(50);
DO $$
BEGIN
    RAISE NOTICE 'Reverted merchant_id column type to VARCHAR(50)';
END $$;

-- Remove columns
ALTER TABLE fee_nominal.api_key_secrets 
    DROP COLUMN IF EXISTS updated_at,
    DROP COLUMN IF EXISTS expires_at;
DO $$
BEGIN
    RAISE NOTICE 'Removed updated_at and expires_at columns';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_109__alter_api_key_secrets rollback successfully';
END $$; 