-- Migration: Update provider code uniqueness to be per merchant instead of global
-- This allows different merchants to create providers with the same code

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_15__update_provider_code_uniqueness migration';
END $$;

-- Drop the existing unique constraint on code
ALTER TABLE fee_nominal.surcharge_providers DROP CONSTRAINT IF EXISTS surcharge_providers_code_key;

-- Drop the existing index on code
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_providers_code;

-- Create a new unique constraint that includes both code and created_by (merchant)
ALTER TABLE fee_nominal.surcharge_providers 
ADD CONSTRAINT uk_surcharge_providers_code_merchant 
UNIQUE (code, created_by);

-- Create a new index for the composite unique constraint
CREATE INDEX idx_surcharge_providers_code_merchant 
ON fee_nominal.surcharge_providers(code, created_by);

-- Also create a regular index on code for performance
CREATE INDEX idx_surcharge_providers_code 
ON fee_nominal.surcharge_providers(code);

DO $$
BEGIN
    RAISE NOTICE 'Updated surcharge_providers table to allow same code for different merchants';
    RAISE NOTICE 'Created unique constraint on (code, created_by)';
    RAISE NOTICE 'Created indexes for performance';
END $$;

-- Verify the changes
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'uk_surcharge_providers_code_merchant' 
        AND conrelid = 'fee_nominal.surcharge_providers'::regclass
    ) THEN
        RAISE EXCEPTION 'Unique constraint uk_surcharge_providers_code_merchant was not created successfully';
    END IF;
    RAISE NOTICE 'Verified unique constraint creation';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_15__update_provider_code_uniqueness migration successfully';
END $$; 