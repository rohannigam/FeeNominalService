-- =============================================
-- Migration: Update MerchantAuditTrail Structure
-- =============================================

-- Set search path
SET search_path TO fee_nominal;

-- Add new columns
ALTER TABLE fee_nominal.merchant_audit_trail
    ADD COLUMN IF NOT EXISTS entity_type VARCHAR(50),
    ADD COLUMN IF NOT EXISTS property_name VARCHAR(100),
    ADD COLUMN IF NOT EXISTS old_value TEXT,
    ADD COLUMN IF NOT EXISTS new_value TEXT;

-- Migrate data from details JSONB to new columns
UPDATE fee_nominal.merchant_audit_trail
SET 
    entity_type = COALESCE(details->>'entity_type', 'MERCHANT'),
    property_name = details->>'property_name',
    old_value = details->>'old_value',
    new_value = details->>'new_value'
WHERE details IS NOT NULL;

-- Make entity_type NOT NULL after data migration
ALTER TABLE fee_nominal.merchant_audit_trail
    ALTER COLUMN entity_type SET NOT NULL;

-- Drop the details column
ALTER TABLE fee_nominal.merchant_audit_trail
    DROP COLUMN details;

-- Update action column length to match new schema
ALTER TABLE fee_nominal.merchant_audit_trail
    ALTER COLUMN action TYPE VARCHAR(50);

-- Create new indexes for the updated structure
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_entity_type ON fee_nominal.merchant_audit_trail(entity_type);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_property_name ON fee_nominal.merchant_audit_trail(property_name); 