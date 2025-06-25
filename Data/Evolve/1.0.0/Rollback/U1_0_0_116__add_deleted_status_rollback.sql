-- Rollback Migration: Remove DELETED status from surcharge_provider_statuses
-- This reverts the changes made in V1_0_0_16__add_deleted_status.sql

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_116__add_deleted_status_rollback migration';
END $$;

-- Check if any providers are using the DELETED status
DO $$
DECLARE
    deleted_providers_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_providers_count
    FROM fee_nominal.surcharge_providers sp
    JOIN fee_nominal.surcharge_provider_statuses sps ON sp.status_id = sps.status_id
    WHERE sps.code = 'DELETED';
    
    IF deleted_providers_count > 0 THEN
        RAISE EXCEPTION 'Cannot rollback: % providers are currently using DELETED status. Please update them to a different status first.', deleted_providers_count;
    END IF;
    
    RAISE NOTICE 'No providers using DELETED status found, proceeding with rollback';
END $$;

-- Remove the DELETED status
DELETE FROM fee_nominal.surcharge_provider_statuses WHERE code = 'DELETED';

-- Verify the DELETED status was removed
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM fee_nominal.surcharge_provider_statuses WHERE code = 'DELETED') THEN
        RAISE EXCEPTION 'DELETED status was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified DELETED status was removed successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_116__add_deleted_status_rollback migration successfully';
END $$; 