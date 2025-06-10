/*
Migration: V1_0_0_10__alter_api_key_secrets.sql
Description: Adds updated_at and expires_at columns to api_key_secrets table
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_3__create_api_key_tables.sql (requires api_key_secrets table)
Changes:
- Adds updated_at column with default value
- Adds expires_at column with default value
- Updates existing records with default values
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_10__alter_api_key_secrets migration...';
END $$;

-- Add updated_at column
ALTER TABLE fee_nominal.api_key_secrets 
ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;
RAISE NOTICE 'Added updated_at column to api_key_secrets table';

-- Add expires_at column
ALTER TABLE fee_nominal.api_key_secrets 
ADD COLUMN IF NOT EXISTS expires_at TIMESTAMP WITH TIME ZONE DEFAULT (CURRENT_TIMESTAMP + INTERVAL '1 year');
RAISE NOTICE 'Added expires_at column to api_key_secrets table';

-- Update existing records
UPDATE fee_nominal.api_key_secrets 
SET 
    updated_at = CURRENT_TIMESTAMP,
    expires_at = CURRENT_TIMESTAMP + INTERVAL '1 year'
WHERE updated_at IS NULL OR expires_at IS NULL;
RAISE NOTICE 'Updated existing records with default values';

-- Create trigger for updated_at
CREATE OR REPLACE FUNCTION fee_nominal.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
RAISE NOTICE 'Created update_updated_at_column function';

DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON fee_nominal.api_key_secrets;
CREATE TRIGGER update_api_key_secrets_updated_at
    BEFORE UPDATE ON fee_nominal.api_key_secrets
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();
RAISE NOTICE 'Created update_api_key_secrets_updated_at trigger';

-- Verify changes
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'api_key_secrets' 
        AND column_name = 'updated_at'
    ) THEN
        RAISE EXCEPTION 'updated_at column was not added successfully';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'api_key_secrets' 
        AND column_name = 'expires_at'
    ) THEN
        RAISE EXCEPTION 'expires_at column was not added successfully';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_trigger 
        WHERE tgname = 'update_api_key_secrets_updated_at'
    ) THEN
        RAISE EXCEPTION 'update_api_key_secrets_updated_at trigger was not created successfully';
    END IF;
    
    RAISE NOTICE 'Verified all changes were applied successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_10__alter_api_key_secrets migration successfully';
END $$;
