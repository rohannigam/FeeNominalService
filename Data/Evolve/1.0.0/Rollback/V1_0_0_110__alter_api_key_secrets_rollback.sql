/*
Rollback: V1_0_0_110__alter_api_key_secrets_rollback.sql
Description: Removes updated_at and expires_at columns from api_key_secrets table
Dependencies: None
Changes:
- Removes updated_at column
- Removes expires_at column
- Removes trigger and function for updated_at tracking
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of V1_0_0_110__alter_api_key_secrets...';
END $$;

-- Drop trigger first
DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON fee_nominal.api_key_secrets;
RAISE NOTICE 'Dropped update_api_key_secrets_updated_at trigger';

-- Drop function
DROP FUNCTION IF EXISTS fee_nominal.update_updated_at_column();
RAISE NOTICE 'Dropped update_updated_at_column function';

-- Remove columns
ALTER TABLE fee_nominal.api_key_secrets 
DROP COLUMN IF EXISTS updated_at;
RAISE NOTICE 'Dropped updated_at column from api_key_secrets table';

ALTER TABLE fee_nominal.api_key_secrets 
DROP COLUMN IF EXISTS expires_at;
RAISE NOTICE 'Dropped expires_at column from api_key_secrets table';

-- Verify rollback
DO $$ 
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'api_key_secrets' 
        AND column_name = 'updated_at'
    ) THEN
        RAISE EXCEPTION 'updated_at column was not removed successfully';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'api_key_secrets' 
        AND column_name = 'expires_at'
    ) THEN
        RAISE EXCEPTION 'expires_at column was not removed successfully';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_trigger 
        WHERE tgname = 'update_api_key_secrets_updated_at'
    ) THEN
        RAISE EXCEPTION 'update_api_key_secrets_updated_at trigger was not removed successfully';
    END IF;
    
    RAISE NOTICE 'Verified all changes were rolled back successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback of V1_0_0_110__alter_api_key_secrets successfully';
END $$; 