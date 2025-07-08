-- Migration: Rename source_transaction_id to correlation_id
-- This migration renames the column to better reflect its purpose as a correlation identifier

-- Rename the column
ALTER TABLE fee_nominal.surcharge_trans 
RENAME COLUMN source_transaction_id TO correlation_id;

-- Update the index name to match the new column name
DROP INDEX IF EXISTS fee_nominal.IX_surcharge_trans_source_transaction_id;
CREATE INDEX IX_surcharge_trans_correlation_id ON fee_nominal.surcharge_trans (correlation_id);

-- Add a comment to document the change
COMMENT ON COLUMN fee_nominal.surcharge_trans.correlation_id IS 'Correlation identifier for linking related transactions (renamed from source_transaction_id)'; 