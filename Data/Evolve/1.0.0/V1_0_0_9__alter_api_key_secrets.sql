/*
Migration: V1_0_0_9__alter_api_key_secrets.sql
Description: Adds updated_at and expires_at columns to api_key_secrets table and changes merchant_id to UUID
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_3__create_api_key_tables.sql (requires api_key_secrets table)
Changes:
- Adds updated_at column with default value
- Adds expires_at column with default value
- Changes merchant_id column type from VARCHAR(50) to UUID
- Adds foreign key constraint to merchants table
- Updates existing records with default values
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_9__alter_api_key_secrets migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Add updated_at column
ALTER TABLE fee_nominal.api_key_secrets 
ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP;
DO $$
BEGIN
    RAISE NOTICE 'Added updated_at column to api_key_secrets table';
END $$;

-- Add expires_at column
ALTER TABLE fee_nominal.api_key_secrets 
ADD COLUMN IF NOT EXISTS expires_at TIMESTAMP WITH TIME ZONE DEFAULT (CURRENT_TIMESTAMP + INTERVAL '1 year');
DO $$
BEGIN
    RAISE NOTICE 'Added expires_at column to api_key_secrets table';
END $$;

-- Change merchant_id to UUID type
ALTER TABLE fee_nominal.api_key_secrets 
    ALTER COLUMN merchant_id TYPE UUID USING merchant_id::uuid;
DO $$
BEGIN
    RAISE NOTICE 'Changed merchant_id column type to UUID';
END $$;

-- Add foreign key constraint
ALTER TABLE fee_nominal.api_key_secrets
    ADD CONSTRAINT fk_api_key_secrets_merchant 
    FOREIGN KEY (merchant_id) 
    REFERENCES fee_nominal.merchants(merchant_id);
DO $$
BEGIN
    RAISE NOTICE 'Added foreign key constraint for merchant_id';
END $$;

-- Update existing records
UPDATE fee_nominal.api_key_secrets 
SET 
    updated_at = CURRENT_TIMESTAMP,
    expires_at = CURRENT_TIMESTAMP + INTERVAL '1 year'
WHERE updated_at IS NULL OR expires_at IS NULL;
DO $$
BEGIN
    RAISE NOTICE 'Updated existing records with default values';
END $$;

-- Create trigger for updated_at
CREATE OR REPLACE FUNCTION fee_nominal.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$
BEGIN
    RAISE NOTICE 'Created update_updated_at_column function';
END $$;

DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON fee_nominal.api_key_secrets;
CREATE TRIGGER update_api_key_secrets_updated_at
    BEFORE UPDATE ON fee_nominal.api_key_secrets
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();
DO $$
BEGIN
    RAISE NOTICE 'Created trigger for updated_at column';
END $$;

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
    RAISE NOTICE 'Completed V1_0_0_9__alter_api_key_secrets migration successfully';
END $$;
