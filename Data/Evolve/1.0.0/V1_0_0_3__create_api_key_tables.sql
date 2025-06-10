/*
Migration: V1_0_0_3__create_api_key_tables.sql
Description: Creates tables for managing API keys and secrets
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchants table)
Changes:
- Creates api_keys table
- Creates api_key_secrets table
- Creates api_key_audit_logs table
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_3__create_api_key_tables migration...';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.api_keys (
    api_key_id SERIAL PRIMARY KEY,
    merchant_id INTEGER NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
RAISE NOTICE 'Created api_keys table';

-- Verify api_keys table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_keys') THEN
        RAISE EXCEPTION 'Table api_keys was not created successfully';
    END IF;
    RAISE NOTICE 'Verified api_keys table creation';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.api_key_secrets (
    api_key_secret_id SERIAL PRIMARY KEY,
    api_key_id INTEGER NOT NULL REFERENCES fee_nominal.api_keys(api_key_id),
    secret_key VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
RAISE NOTICE 'Created api_key_secrets table';

-- Verify api_key_secrets table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_key_secrets') THEN
        RAISE EXCEPTION 'Table api_key_secrets was not created successfully';
    END IF;
    RAISE NOTICE 'Verified api_key_secrets table creation';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.api_key_audit_logs (
    audit_log_id SERIAL PRIMARY KEY,
    api_key_id INTEGER NOT NULL REFERENCES fee_nominal.api_keys(api_key_id),
    action VARCHAR(50) NOT NULL,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100)
);
RAISE NOTICE 'Created api_key_audit_logs table';

-- Verify api_key_audit_logs table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_key_audit_logs') THEN
        RAISE EXCEPTION 'Table api_key_audit_logs was not created successfully';
    END IF;
    RAISE NOTICE 'Verified api_key_audit_logs table creation';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_3__create_api_key_tables migration successfully';
END $$;
