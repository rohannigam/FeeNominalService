-- Migration: Add DELETED status for soft delete functionality
-- This allows providers to be marked as deleted instead of being physically removed

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_16__add_deleted_status migration';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Add DELETED status to surcharge_provider_statuses
INSERT INTO fee_nominal.surcharge_provider_statuses (code, name, description) VALUES
    ('DELETED', 'Deleted', 'Provider has been permanently removed (soft delete)')
ON CONFLICT (code) DO NOTHING;

-- Verify the DELETED status was added
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM fee_nominal.surcharge_provider_statuses WHERE code = 'DELETED') THEN
        RAISE EXCEPTION 'DELETED status was not added successfully';
    END IF;
    RAISE NOTICE 'Verified DELETED status was added successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_16__add_deleted_status migration successfully';
END $$; 