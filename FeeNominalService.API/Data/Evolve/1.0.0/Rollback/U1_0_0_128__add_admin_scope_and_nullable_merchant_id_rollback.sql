-- U1_0_0_128__add_admin_scope_and_nullable_merchant_id_rollback.sql
-- Rollback for admin/cross-merchant support migration

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback for admin/cross-merchant support migration...';
END $$;

SET search_path TO fee_nominal;

-- Drop indexes
DROP INDEX IF EXISTS idx_api_keys_scope;
DROP INDEX IF EXISTS idx_api_key_secrets_scope;
DROP INDEX IF EXISTS idx_surcharge_provider_configs_scope;
DROP INDEX IF EXISTS idx_surcharge_trans_scope;

-- Drop check constraints
ALTER TABLE api_keys DROP CONSTRAINT IF EXISTS chk_api_keys_scope_merchant_id;
ALTER TABLE api_key_secrets DROP CONSTRAINT IF EXISTS chk_api_key_secrets_scope_merchant_id;
ALTER TABLE surcharge_provider_configs DROP CONSTRAINT IF EXISTS chk_surcharge_provider_configs_scope_merchant_id;
ALTER TABLE surcharge_trans DROP CONSTRAINT IF EXISTS chk_surcharge_trans_scope_merchant_id;

-- Remove 'scope' column and make merchant_id NOT NULL
ALTER TABLE api_keys DROP COLUMN IF EXISTS scope, ALTER COLUMN merchant_id SET NOT NULL;
ALTER TABLE api_key_secrets DROP COLUMN IF EXISTS scope, ALTER COLUMN merchant_id SET NOT NULL;
ALTER TABLE surcharge_provider_configs DROP COLUMN IF EXISTS scope, ALTER COLUMN merchant_id SET NOT NULL;
ALTER TABLE surcharge_trans DROP COLUMN IF EXISTS scope, ALTER COLUMN merchant_id SET NOT NULL;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback for admin/cross-merchant support migration.';
END $$; 