/*
Rollback Migration: U1_0_0_121__fix_operation_type_column_rollback.sql
Description: Reverts operation_type and status columns back to PostgreSQL enums
Dependencies: 
- V1_0_0_21__fix_operation_type_column.sql
Changes:
- Reverts operation_type column from VARCHAR(20) back to fee_nominal.surcharge_operation_type
- Reverts status column from VARCHAR(20) back to fee_nominal.surcharge_transaction_status
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_121__fix_operation_type_column_rollback...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Check if any values are outside the enum range before rollback
DO $$
DECLARE
    invalid_operation_types INTEGER;
    invalid_statuses INTEGER;
BEGIN
    -- Check operation_type values
    SELECT COUNT(*) INTO invalid_operation_types 
    FROM fee_nominal.surcharge_trans 
    WHERE operation_type NOT IN ('auth', 'sale', 'refund', 'cancel');
    
    IF invalid_operation_types > 0 THEN
        RAISE EXCEPTION 'Cannot rollback: % operation_type values are not valid enum values', invalid_operation_types;
    END IF;
    
    -- Check status values
    SELECT COUNT(*) INTO invalid_statuses 
    FROM fee_nominal.surcharge_trans 
    WHERE status NOT IN ('pending', 'processing', 'completed', 'failed', 'cancelled');
    
    IF invalid_statuses > 0 THEN
        RAISE EXCEPTION 'Cannot rollback: % status values are not valid enum values', invalid_statuses;
    END IF;
    
    RAISE NOTICE 'All values are valid for enum rollback';
END $$;

-- Drop indexes that reference the VARCHAR columns first
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_trans_operation_status;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_trans_operation_type;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_trans_status;

DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes that reference VARCHAR columns';
END $$;

-- Revert operation_type column from VARCHAR to enum
ALTER TABLE fee_nominal.surcharge_trans 
ALTER COLUMN operation_type TYPE fee_nominal.surcharge_operation_type USING operation_type::fee_nominal.surcharge_operation_type;

DO $$
BEGIN
    RAISE NOTICE 'Reverted operation_type column to enum';
END $$;

-- Revert status column from VARCHAR to enum
ALTER TABLE fee_nominal.surcharge_trans 
ALTER COLUMN status TYPE fee_nominal.surcharge_transaction_status USING status::fee_nominal.surcharge_transaction_status;

DO $$
BEGIN
    RAISE NOTICE 'Reverted status column to enum';
END $$;

-- Recreate the indexes
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_operation_type ON fee_nominal.surcharge_trans(operation_type);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_status ON fee_nominal.surcharge_trans(status);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_operation_status ON fee_nominal.surcharge_trans(operation_type, status);

DO $$
BEGIN
    RAISE NOTICE 'Recreated indexes for enum columns';
END $$;

-- Verify the rollback
DO $$ 
BEGIN
    -- Check operation_type column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_trans' 
                   AND column_name = 'operation_type'
                   AND udt_name = 'surcharge_operation_type') THEN
        RAISE EXCEPTION 'Column operation_type was not reverted to enum';
    END IF;
    
    -- Check status column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_trans' 
                   AND column_name = 'status'
                   AND udt_name = 'surcharge_transaction_status') THEN
        RAISE EXCEPTION 'Column status was not reverted to enum';
    END IF;
    
    RAISE NOTICE 'Verified enum column rollback successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_121__fix_operation_type_column_rollback successfully';
END $$; 