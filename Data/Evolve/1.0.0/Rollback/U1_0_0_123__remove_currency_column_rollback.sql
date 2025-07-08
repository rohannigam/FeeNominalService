-- Rollback: Add back currency column to surcharge_trans table
-- This rollback adds back the currency column that was removed

-- Add the currency column back
ALTER TABLE fee_nominal.surcharge_trans 
ADD COLUMN currency VARCHAR(3) NOT NULL DEFAULT 'USD';

-- Add a comment to document the rollback
COMMENT ON COLUMN fee_nominal.surcharge_trans.currency IS 'Currency code for the transaction (3-letter ISO code)';

-- Update the table comment
COMMENT ON TABLE fee_nominal.surcharge_trans IS 'Surcharge transactions table'; 