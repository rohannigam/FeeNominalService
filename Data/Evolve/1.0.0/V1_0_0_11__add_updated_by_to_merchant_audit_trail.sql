/*
Migration: V1_0_0_11__add_updated_by_to_merchant_audit_trail.sql
Description: Adds user tracking to merchant audit trail
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchant_audit_logs table)
Changes:
- Adds updated_by column to merchant_audit_logs table
- Updates existing audit log entries with system user
- Adds trigger to automatically update the updated_by column
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_11__add_updated_by_to_merchant_audit_trail migration...';
END $$;

-- Add updated_by column if it doesn't exist
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'merchant_audit_trail' 
        AND column_name = 'updated_by'
    ) THEN
        ALTER TABLE fee_nominal.merchant_audit_trail 
        ADD COLUMN updated_by VARCHAR(50) NOT NULL DEFAULT 'system';
        RAISE NOTICE 'Added updated_by column to merchant_audit_trail table';
    END IF;
END $$;

-- Update existing records
UPDATE fee_nominal.merchant_audit_trail 
SET updated_by = 'system'
WHERE updated_by IS NULL;
DO $$
BEGIN
    RAISE NOTICE 'Updated existing records with default value';
END $$;

-- Create trigger for updated_by
CREATE OR REPLACE FUNCTION fee_nominal.update_merchant_audit_updated_by()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_by = COALESCE(current_setting('app.current_user', TRUE), 'system');
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
DO $$
BEGIN
    RAISE NOTICE 'Created update_merchant_audit_updated_by function';
END $$;

DROP TRIGGER IF EXISTS update_merchant_audit_updated_by ON fee_nominal.merchant_audit_trail;
CREATE TRIGGER update_merchant_audit_updated_by
    BEFORE INSERT OR UPDATE ON fee_nominal.merchant_audit_trail
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_merchant_audit_updated_by();
DO $$
BEGIN
    RAISE NOTICE 'Created update_merchant_audit_updated_by trigger';
END $$;

-- Verify changes
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'merchant_audit_trail' 
        AND column_name = 'updated_by'
    ) THEN
        RAISE EXCEPTION 'updated_by column was not added successfully';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_trigger 
        WHERE tgname = 'update_merchant_audit_updated_by'
    ) THEN
        RAISE EXCEPTION 'update_merchant_audit_updated_by trigger was not created successfully';
    END IF;
    
    RAISE NOTICE 'Verified all changes were applied successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_11__add_updated_by_to_merchant_audit_trail migration successfully';
END $$;
