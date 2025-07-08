-- Rollback Migration: Rename correlation_id back to source_transaction_id
-- This rollback script reverts the column rename if needed

-- Drop the new index
DROP INDEX IF EXISTS fee_nominal.IX_surcharge_trans_correlation_id;

-- Rename the column back
ALTER TABLE fee_nominal.surcharge_trans 
RENAME COLUMN correlation_id TO source_transaction_id;

-- Recreate the original index
CREATE INDEX IX_surcharge_trans_source_transaction_id ON fee_nominal.surcharge_trans (source_transaction_id);

-- Remove the comment
COMMENT ON COLUMN fee_nominal.surcharge_trans.source_transaction_id IS NULL; 