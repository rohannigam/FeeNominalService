-- U1_0_0_127__add_bulk_operations_support_rollback.sql
-- Rollback script for bulk operations support migration

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of bulk operations support migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;

-- Drop indexes
DROP INDEX IF EXISTS idx_api_keys_is_admin;
DROP INDEX IF EXISTS idx_surcharge_trans_updated_by;
DROP INDEX IF EXISTS idx_surcharge_trans_created_by;
DROP INDEX IF EXISTS idx_surcharge_trans_original_surcharge_trans_id;
DROP INDEX IF EXISTS idx_surcharge_trans_batch_id;
DROP INDEX IF EXISTS idx_surcharge_providers_provider_type;

-- Remove columns from api_keys
ALTER TABLE api_keys DROP COLUMN IF EXISTS is_admin;

-- Remove columns from surcharge_trans
ALTER TABLE surcharge_trans DROP COLUMN IF EXISTS updated_by;
ALTER TABLE surcharge_trans DROP COLUMN IF EXISTS created_by;
ALTER TABLE surcharge_trans DROP COLUMN IF EXISTS original_surcharge_trans_id;
ALTER TABLE surcharge_trans DROP COLUMN IF EXISTS merchant_transaction_id;
ALTER TABLE surcharge_trans DROP COLUMN IF EXISTS batch_id;

-- Remove columns from surcharge_providers
ALTER TABLE surcharge_providers DROP COLUMN IF EXISTS provider_type;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback of bulk operations support migration successfully';
END $$; 