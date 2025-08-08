-- V1_0_0_27__add_bulk_operations_support.sql
-- Adds support for bulk operations and sale/refund/cancel operations
-- Combines multiple small migrations into one efficient migration

DO $$
BEGIN
    RAISE NOTICE 'Starting bulk operations support migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;

-- Add provider_type column to surcharge_providers (for provider-agnostic operations)
ALTER TABLE surcharge_providers 
ADD COLUMN IF NOT EXISTS provider_type VARCHAR(50) NOT NULL DEFAULT 'INTERPAYMENTS';

-- Add bulk operations columns to surcharge_trans
ALTER TABLE surcharge_trans 
ADD COLUMN IF NOT EXISTS batch_id VARCHAR(100),
ADD COLUMN IF NOT EXISTS merchant_transaction_id VARCHAR(255);

-- Add sale/refund/cancel support columns to surcharge_trans
ALTER TABLE surcharge_trans 
ADD COLUMN IF NOT EXISTS original_surcharge_trans_id UUID REFERENCES surcharge_trans(surcharge_trans_id),
ADD COLUMN IF NOT EXISTS created_by VARCHAR(50) NOT NULL DEFAULT 'system',
ADD COLUMN IF NOT EXISTS updated_by VARCHAR(50) NOT NULL DEFAULT 'system';

-- Add admin support to api_keys
ALTER TABLE api_keys
ADD COLUMN IF NOT EXISTS is_admin BOOLEAN NOT NULL DEFAULT FALSE;

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_provider_type ON surcharge_providers(provider_type);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_batch_id ON surcharge_trans(batch_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_original_surcharge_trans_id ON surcharge_trans(original_surcharge_trans_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_created_by ON surcharge_trans(created_by);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_updated_by ON surcharge_trans(updated_by);
CREATE INDEX IF NOT EXISTS idx_api_keys_is_admin ON api_keys(is_admin);

DO $$
BEGIN
    RAISE NOTICE 'Completed bulk operations support migration successfully';
END $$; 