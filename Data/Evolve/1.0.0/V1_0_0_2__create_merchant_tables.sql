/*
Migration: V1_0_0_2__create_merchant_tables.sql
Description: Creates the core merchant-related tables and initial data
Dependencies: V1_0_0_1__create_schema.sql (requires fee_nominal schema)
Changes:
- Creates merchant_statuses table with initial status data
- Creates merchants table
- Creates surcharge_providers table
- Creates surcharge_provider_configs table
- Creates surcharge_provider_config_history table
- Creates merchant_audit_logs table
- Inserts default merchant statuses
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_2__create_merchant_tables migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Add extension if not exists
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create merchant_statuses table
CREATE TABLE IF NOT EXISTS fee_nominal.merchant_statuses (
    merchant_status_id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$
BEGIN
    RAISE NOTICE 'Created merchant_statuses table';
END $$;

-- Verify merchant_statuses table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'merchant_statuses') THEN
        RAISE EXCEPTION 'Table merchant_statuses was not created successfully';
    END IF;
    RAISE NOTICE 'Verified merchant_statuses table creation';
END $$;

-- Create surcharge_provider_statuses table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_provider_statuses (
    status_id SERIAL PRIMARY KEY,
    code VARCHAR(20) NOT NULL UNIQUE,
    name VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Insert default surcharge provider statuses
INSERT INTO fee_nominal.surcharge_provider_statuses (code, name, description) VALUES
    ('ACTIVE', 'Active', 'Provider is operational and accepting requests'),
    ('INACTIVE', 'Inactive', 'Provider is temporarily disabled'),
    ('SUSPENDED', 'Suspended', 'Provider is suspended due to issues'),
    ('PENDING', 'Pending', 'Provider is being onboarded/configured'),
    ('DEPRECATED', 'Deprecated', 'Provider is being phased out'),
    ('MAINTENANCE', 'Maintenance', 'Provider is under maintenance')
ON CONFLICT (code) DO NOTHING;

-- Create merchants table
CREATE TABLE IF NOT EXISTS fee_nominal.merchants (
    merchant_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    external_merchant_id VARCHAR(50) UNIQUE,
    external_merchant_guid UUID,
    name VARCHAR(255) NOT NULL,
    status_id INTEGER NOT NULL REFERENCES fee_nominal.merchant_statuses(merchant_status_id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL
);
DO $$
BEGIN
    RAISE NOTICE 'Created merchants table';
END $$;

-- Verify merchants table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'merchants') THEN
        RAISE EXCEPTION 'Table merchants was not created successfully';
    END IF;
    RAISE NOTICE 'Verified merchants table creation';
END $$;

-- Create indexes for merchants table
CREATE INDEX IF NOT EXISTS idx_merchants_external_merchant_id ON fee_nominal.merchants(external_merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchants_external_merchant_guid ON fee_nominal.merchants(external_merchant_guid);
CREATE INDEX IF NOT EXISTS idx_merchants_status_id ON fee_nominal.merchants(status_id);

-- Create surcharge_providers table with updated schema
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_providers (
    surcharge_provider_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(20) UNIQUE NOT NULL,
    description TEXT,
    base_url VARCHAR(255) NOT NULL,
    authentication_type VARCHAR(50) NOT NULL DEFAULT 'API_KEY',
    credentials_schema JSONB NOT NULL,
    status_id INTEGER NOT NULL REFERENCES fee_nominal.surcharge_provider_statuses(status_id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL,
    updated_by VARCHAR(50) NOT NULL
);

-- Create indexes for surcharge_providers
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_code ON fee_nominal.surcharge_providers(code);
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_status ON fee_nominal.surcharge_providers(status_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_created_at ON fee_nominal.surcharge_providers(created_at);
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_updated_at ON fee_nominal.surcharge_providers(updated_at);

-- Create surcharge_provider_configs table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_provider_configs (
    surcharge_provider_config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    surcharge_provider_id UUID NOT NULL REFERENCES fee_nominal.surcharge_providers(surcharge_provider_id),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    config_name VARCHAR(100) NOT NULL,
    api_version VARCHAR(20) NOT NULL,
    credentials JSONB NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL,
    updated_by VARCHAR(50) NOT NULL,
    UNIQUE(surcharge_provider_id, merchant_id, config_name)
);

-- Create surcharge_provider_config_history table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_provider_config_history (
    surcharge_provider_config_history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    surcharge_provider_config_id UUID NOT NULL REFERENCES fee_nominal.surcharge_provider_configs(surcharge_provider_config_id),
    action VARCHAR(50) NOT NULL,
    previous_values JSONB,
    new_values JSONB,
    changed_by VARCHAR(50) NOT NULL,
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reason TEXT
);

-- Verify surcharge_provider_statuses table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'surcharge_provider_statuses') THEN
        RAISE EXCEPTION 'Table surcharge_provider_statuses was not created successfully';
    END IF;
    RAISE NOTICE 'Verified surcharge_provider_statuses table creation';
END $$;

-- Verify surcharge_providers table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'surcharge_providers') THEN
        RAISE EXCEPTION 'Table surcharge_providers was not created successfully';
    END IF;
    RAISE NOTICE 'Verified surcharge_providers table creation';
END $$;

-- Verify surcharge_provider_configs table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'surcharge_provider_configs') THEN
        RAISE EXCEPTION 'Table surcharge_provider_configs was not created successfully';
    END IF;
    RAISE NOTICE 'Verified surcharge_provider_configs table creation';
END $$;

-- Verify surcharge_provider_config_history table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'surcharge_provider_config_history') THEN
        RAISE EXCEPTION 'Table surcharge_provider_config_history was not created successfully';
    END IF;
    RAISE NOTICE 'Verified surcharge_provider_config_history table creation';
END $$;

-- Create merchant_audit_logs table
CREATE TABLE IF NOT EXISTS fee_nominal.merchant_audit_logs (
    audit_log_id SERIAL PRIMARY KEY,
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    action VARCHAR(50) NOT NULL,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50)
);
DO $$
BEGIN
    RAISE NOTICE 'Created merchant_audit_logs table';
END $$;

-- Verify merchant_audit_logs table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'merchant_audit_logs') THEN
        RAISE EXCEPTION 'Table merchant_audit_logs was not created successfully';
    END IF;
    RAISE NOTICE 'Verified merchant_audit_logs table creation';
END $$;

-- Insert default merchant statuses
INSERT INTO fee_nominal.merchant_statuses (code, name, description) VALUES
    ('ACTIVE', 'Active', 'Merchant is active and can process transactions'),
    ('INACTIVE', 'Inactive', 'Merchant is inactive and cannot process transactions'),
    ('SUSPENDED', 'Suspended', 'Merchant is temporarily suspended'),
    ('TERMINATED', 'Terminated', 'Merchant account has been terminated')
ON CONFLICT (code) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted default merchant statuses';
END $$;

-- Verify data insertion
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM fee_nominal.merchant_statuses WHERE code = 'ACTIVE') THEN
        RAISE EXCEPTION 'Default merchant statuses were not inserted successfully';
    END IF;
    RAISE NOTICE 'Verified default merchant statuses insertion';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_2__create_merchant_tables migration successfully';
END $$;
