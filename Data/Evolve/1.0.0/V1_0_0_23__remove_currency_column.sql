-- Migration: Remove currency column from surcharge_trans table
-- This migration removes the currency column since it's not needed for surcharge operations

-- Remove the currency column
ALTER TABLE fee_nominal.surcharge_trans 
DROP COLUMN IF EXISTS currency;

-- Add a comment to document the change
COMMENT ON TABLE fee_nominal.surcharge_trans IS 'Surcharge transactions table (currency column removed as not needed)'; 