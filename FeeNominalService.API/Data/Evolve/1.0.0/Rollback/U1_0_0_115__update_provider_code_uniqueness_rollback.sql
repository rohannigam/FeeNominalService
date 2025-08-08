-- Rollback Migration: Restore provider code uniqueness to be global instead of per merchant
-- This reverts the changes made in V1_0_0_15__update_provider_code_uniqueness.sql

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_115__update_provider_code_uniqueness_rollback migration';
END $$;

-- Drop the composite unique constraint
ALTER TABLE fee_nominal.surcharge_providers DROP CONSTRAINT IF EXISTS uk_surcharge_providers_code_merchant;

-- Drop the composite index
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_providers_code_merchant;

-- Drop the regular code index (we'll recreate it as unique)
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_providers_code;

-- Restore the original global unique constraint on code
ALTER TABLE fee_nominal.surcharge_providers 
ADD CONSTRAINT surcharge_providers_code_key 
UNIQUE (code);

-- Recreate the original unique index on code
CREATE UNIQUE INDEX idx_surcharge_providers_code 
ON fee_nominal.surcharge_providers(code);

DO $$
BEGIN
    RAISE NOTICE 'Restored surcharge_providers table to global unique constraint on code';
    RAISE NOTICE 'Dropped composite unique constraint on (code, created_by)';
    RAISE NOTICE 'Recreated unique index on code';
END $$;

-- Verify the rollback changes
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'surcharge_providers_code_key' 
        AND conrelid = 'fee_nominal.surcharge_providers'::regclass
    ) THEN
        RAISE EXCEPTION 'Unique constraint surcharge_providers_code_key was not restored successfully';
    END IF;
    RAISE NOTICE 'Verified unique constraint restoration';
END $$;

-- Check for any duplicate codes that would violate the restored constraint
DO $$
DECLARE
    duplicate_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO duplicate_count
    FROM (
        SELECT code, COUNT(*) as cnt
        FROM fee_nominal.surcharge_providers
        GROUP BY code
        HAVING COUNT(*) > 1
    ) duplicates;
    
    IF duplicate_count > 0 THEN
        RAISE EXCEPTION 'Found % duplicate provider codes. Please resolve duplicates before rolling back this migration.', duplicate_count;
    END IF;
    
    RAISE NOTICE 'Verified no duplicate codes exist that would violate the restored constraint';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_115__update_provider_code_uniqueness_rollback migration successfully';
END $$; 