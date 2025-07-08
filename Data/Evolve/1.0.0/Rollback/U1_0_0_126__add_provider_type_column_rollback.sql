-- U1_0_0_126__add_provider_type_column_rollback.sql
-- Rollback script for V1_0_0_26__add_provider_type_column.sql
-- Remove provider_type column from surcharge_providers table if it exists

DO $$
BEGIN
    -- Check if provider_type column exists before attempting to drop it
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_providers' 
        AND column_name = 'provider_type'
    ) THEN
        -- Drop provider_type column
        ALTER TABLE fee_nominal.surcharge_providers 
        DROP COLUMN provider_type;
        
        RAISE NOTICE 'Removed provider_type column from surcharge_providers table';
    ELSE
        RAISE NOTICE 'provider_type column does not exist in surcharge_providers table - nothing to rollback';
    END IF;
END$$;

-- Verify the column was removed successfully
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'surcharge_providers' 
        AND column_name = 'provider_type'
    ) THEN
        RAISE EXCEPTION 'provider_type column was not removed successfully from surcharge_providers table';
    END IF;
    RAISE NOTICE 'Verified provider_type column has been removed from surcharge_providers table';
END$$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_126__add_provider_type_column_rollback successfully';
END$$; 