-- V1_0_0_28__add_admin_scope_and_nullable_merchant_id.sql
-- Adds 'scope' column and makes merchant_id nullable for admin/cross-merchant support

DO $$
BEGIN
    RAISE NOTICE 'Starting admin/cross-merchant support migration...';
END $$;

SET search_path TO fee_nominal;

-- Add 'scope' column to api_keys if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns WHERE table_name='api_keys' AND column_name='scope'
    ) THEN
        ALTER TABLE api_keys ADD COLUMN scope VARCHAR(20) DEFAULT 'merchant';
    END IF;
END $$;
UPDATE api_keys SET scope = 'merchant' WHERE scope IS NULL;
ALTER TABLE api_keys ALTER COLUMN scope SET NOT NULL;
ALTER TABLE api_keys ALTER COLUMN merchant_id DROP NOT NULL;

-- Add 'service_name' column to api_keys if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns WHERE table_name='api_keys' AND column_name='service_name'
    ) THEN
        ALTER TABLE api_keys ADD COLUMN service_name VARCHAR(100) NULL;
    END IF;
END $$;

-- Add 'scope' column to surcharge_provider_configs if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns WHERE table_name='surcharge_provider_configs' AND column_name='scope'
    ) THEN
        ALTER TABLE surcharge_provider_configs ADD COLUMN scope VARCHAR(20) DEFAULT 'merchant';
    END IF;
END $$;
UPDATE surcharge_provider_configs SET scope = 'merchant' WHERE scope IS NULL;
ALTER TABLE surcharge_provider_configs ALTER COLUMN scope SET NOT NULL;
ALTER TABLE surcharge_provider_configs ALTER COLUMN merchant_id DROP NOT NULL;

-- Add 'scope' column to surcharge_trans if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns WHERE table_name='surcharge_trans' AND column_name='scope'
    ) THEN
        ALTER TABLE surcharge_trans ADD COLUMN scope VARCHAR(20) DEFAULT 'merchant';
    END IF;
END $$;
UPDATE surcharge_trans SET scope = 'merchant' WHERE scope IS NULL;
ALTER TABLE surcharge_trans ALTER COLUMN scope SET NOT NULL;
ALTER TABLE surcharge_trans ALTER COLUMN merchant_id DROP NOT NULL;

-- Add check constraints for scope/merchant_id logic
DO $$
BEGIN
    RAISE NOTICE 'Adding check constraints for scope/merchant_id logic...';
END $$;
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_api_keys_scope_merchant_id'
    ) THEN
        ALTER TABLE api_keys
            ADD CONSTRAINT chk_api_keys_scope_merchant_id
            CHECK ((scope = 'merchant' AND merchant_id IS NOT NULL) OR (scope = 'admin' AND merchant_id IS NULL));
    END IF;
END $$;
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_surcharge_provider_configs_scope_merchant_id'
    ) THEN
        ALTER TABLE surcharge_provider_configs
            ADD CONSTRAINT chk_surcharge_provider_configs_scope_merchant_id
            CHECK ((scope = 'merchant' AND merchant_id IS NOT NULL) OR (scope = 'admin' AND merchant_id IS NULL));
    END IF;
END $$;
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_surcharge_trans_scope_merchant_id'
    ) THEN
        ALTER TABLE surcharge_trans
            ADD CONSTRAINT chk_surcharge_trans_scope_merchant_id
            CHECK ((scope = 'merchant' AND merchant_id IS NOT NULL) OR (scope = 'admin' AND merchant_id IS NULL));
    END IF;
END $$;

-- Add indexes for scope
DO $$
BEGIN
    RAISE NOTICE 'Creating indexes for scope columns...';
END $$;
CREATE INDEX IF NOT EXISTS idx_api_keys_scope ON api_keys(scope);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_scope ON surcharge_provider_configs(scope);
CREATE INDEX IF NOT EXISTS idx_surcharge_trans_scope ON surcharge_trans(scope);

DO $$
BEGIN
    RAISE NOTICE 'Completed admin/cross-merchant support migration.';
END $$; 