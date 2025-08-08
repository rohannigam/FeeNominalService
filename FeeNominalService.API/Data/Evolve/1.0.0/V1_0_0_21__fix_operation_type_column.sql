/*
Migration: V1_0_0_21__fix_operation_type_column.sql
Description: Changes operation_type column from PostgreSQL enum to VARCHAR to match EF Core configuration
Dependencies: 
- V1_0_0_13__create_surcharge_trans_table.sql
Changes:
- Changes operation_type column from fee_nominal.surcharge_operation_type to VARCHAR(20)
- Changes status column from fee_nominal.surcharge_transaction_status to VARCHAR(20)
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_21__fix_operation_type_column migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Drop indexes that reference the enum columns first
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_trans_operation_status;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_trans_operation_type;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_trans_status;

DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes that reference enum columns';
END $$;

-- Change operation_type column from enum to VARCHAR
ALTER TABLE fee_nominal.surcharge_trans 
ALTER COLUMN operation_type TYPE VARCHAR(20) USING operation_type::text;

DO $$
BEGIN
    RAISE NOTICE 'Changed operation_type column to VARCHAR(20)';
END $$;

-- Change status column from enum to VARCHAR
ALTER TABLE fee_nominal.surcharge_trans 
ALTER COLUMN status TYPE VARCHAR(20) USING status::text;

DO $$
BEGIN
    RAISE NOTICE 'Changed status column to VARCHAR(20)';
END $$;

-- Recreate the indexes
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_operation_type ON fee_nominal.surcharge_trans(operation_type);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_status ON fee_nominal.surcharge_trans(status);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_operation_status ON fee_nominal.surcharge_trans(operation_type, status);

DO $$
BEGIN
    RAISE NOTICE 'Recreated indexes for VARCHAR columns';
END $$;

-- Verify the changes
DO $$ 
BEGIN
    -- Check operation_type column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_trans' 
                   AND column_name = 'operation_type'
                   AND data_type = 'character varying'
                   AND character_maximum_length = 20) THEN
        RAISE EXCEPTION 'Column operation_type was not changed to VARCHAR(20)';
    END IF;
    
    -- Check status column
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema = 'fee_nominal' 
                   AND table_name = 'surcharge_trans' 
                   AND column_name = 'status'
                   AND data_type = 'character varying'
                   AND character_maximum_length = 20) THEN
        RAISE EXCEPTION 'Column status was not changed to VARCHAR(20)';
    END IF;
    
    RAISE NOTICE 'Verified column type changes successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_21__fix_operation_type_column migration successfully';
END $$; 