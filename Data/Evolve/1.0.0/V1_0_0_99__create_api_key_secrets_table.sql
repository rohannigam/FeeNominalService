/*
Migration: V1_0_0_99__create_api_key_secrets_table.sql
Description: Creates and manages the api_key_secrets table and all related indexes, constraints, and triggers.
NOTE: This migration is for LOCAL/DEV ONLY. Exclude or comment out for production deployments.
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_99__create_api_key_secrets_table migration (DEV/LOCAL ONLY)...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Create api_key_secrets table
CREATE TABLE IF NOT EXISTS fee_nominal.api_key_secrets (
    id SERIAL PRIMARY KEY,
    api_key VARCHAR(255) NOT NULL,
    secret VARCHAR(255) NOT NULL,
    merchant_id UUID,
    status VARCHAR(50) NOT NULL DEFAULT 'Active',
    is_revoked BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP WITH TIME ZONE DEFAULT (CURRENT_TIMESTAMP + INTERVAL '1 year'),
    revoked_at TIMESTAMP WITH TIME ZONE,
    last_rotated TIMESTAMP WITH TIME ZONE,
    scope VARCHAR(20) NOT NULL DEFAULT 'merchant',
    UNIQUE(api_key)
);
DO $$
BEGIN
    RAISE NOTICE 'Created api_key_secrets table';
END $$;

-- Add foreign key constraint for merchant_id if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE table_schema = 'fee_nominal' AND table_name = 'api_key_secrets' AND constraint_name = 'fk_api_key_secrets_merchant'
    ) THEN
        EXECUTE 'ALTER TABLE fee_nominal.api_key_secrets ADD CONSTRAINT fk_api_key_secrets_merchant FOREIGN KEY (merchant_id) REFERENCES fee_nominal.merchants(merchant_id)';
    END IF;
END $$;
DO $$
BEGIN
    RAISE NOTICE 'Added foreign key constraint for merchant_id';
END $$;

-- Add check constraint for scope/merchant_id logic if not exists
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'chk_api_key_secrets_scope_merchant_id'
    ) THEN
        EXECUTE 'ALTER TABLE fee_nominal.api_key_secrets ADD CONSTRAINT chk_api_key_secrets_scope_merchant_id CHECK ((scope = ''merchant'' AND merchant_id IS NOT NULL) OR (scope = ''admin'' AND merchant_id IS NULL))';
    END IF;
END $$;

-- Add indexes for api_key_secrets
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_merchant ON fee_nominal.api_key_secrets(merchant_id);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_status ON fee_nominal.api_key_secrets(status);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_is_revoked ON fee_nominal.api_key_secrets(is_revoked);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_created_at ON fee_nominal.api_key_secrets(created_at);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_scope ON fee_nominal.api_key_secrets(scope);

-- Add updated_at trigger and function
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'update_updated_at_column' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        EXECUTE $$
        CREATE FUNCTION fee_nominal.update_updated_at_column()
        RETURNS TRIGGER AS $$
        BEGIN
            NEW.updated_at = CURRENT_TIMESTAMP;
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
        $$;
    END IF;
END $$;
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger WHERE tgname = 'update_api_key_secrets_updated_at'
    ) THEN
        CREATE TRIGGER update_api_key_secrets_updated_at
            BEFORE UPDATE ON fee_nominal.api_key_secrets
            FOR EACH ROW
            EXECUTE FUNCTION fee_nominal.update_updated_at_column();
    END IF;
END $$;
DO $$
BEGIN
    RAISE NOTICE 'Created trigger for updated_at column';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_99__create_api_key_secrets_table migration (DEV/LOCAL ONLY)';
END $$; 