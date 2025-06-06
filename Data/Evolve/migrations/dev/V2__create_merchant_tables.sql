-- Up Migration
-- Create merchant_statuses table
CREATE TABLE IF NOT EXISTS fee_nominal.merchant_statuses (
    merchant_status_id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Verify merchant_statuses table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'merchant_statuses') THEN
        RAISE EXCEPTION 'Table merchant_statuses was not created successfully';
    END IF;
END $$;

-- Create merchants table
CREATE TABLE IF NOT EXISTS fee_nominal.merchants (
    merchant_id SERIAL PRIMARY KEY,
    merchant_status_id INTEGER NOT NULL REFERENCES fee_nominal.merchant_statuses(merchant_status_id),
    merchant_name VARCHAR(100) NOT NULL,
    merchant_code VARCHAR(50) UNIQUE NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Verify merchants table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'merchants') THEN
        RAISE EXCEPTION 'Table merchants was not created successfully';
    END IF;
END $$;

-- Create surcharge_providers table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_providers (
    surcharge_provider_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(50) NOT NULL UNIQUE,
    base_url VARCHAR(255) NOT NULL,
    credentials_schema JSONB NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Verify surcharge_providers table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'surcharge_providers') THEN
        RAISE EXCEPTION 'Table surcharge_providers was not created successfully';
    END IF;
END $$;

-- Create surcharge_provider_configs table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_provider_configs (
    surcharge_provider_config_id SERIAL PRIMARY KEY,
    surcharge_provider_id INTEGER NOT NULL REFERENCES fee_nominal.surcharge_providers(surcharge_provider_id),
    merchant_id INTEGER NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    credentials JSONB NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(surcharge_provider_id, merchant_id)
);

-- Verify surcharge_provider_configs table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'surcharge_provider_configs') THEN
        RAISE EXCEPTION 'Table surcharge_provider_configs was not created successfully';
    END IF;
END $$;

-- Create surcharge_provider_config_history table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_provider_config_history (
    history_id SERIAL PRIMARY KEY,
    surcharge_provider_config_id INTEGER NOT NULL REFERENCES fee_nominal.surcharge_provider_configs(surcharge_provider_config_id),
    action VARCHAR(50) NOT NULL,
    previous_value JSONB,
    new_value JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Verify surcharge_provider_config_history table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'surcharge_provider_config_history') THEN
        RAISE EXCEPTION 'Table surcharge_provider_config_history was not created successfully';
    END IF;
END $$;

-- Create merchant_audit_logs table
CREATE TABLE IF NOT EXISTS fee_nominal.merchant_audit_logs (
    audit_log_id SERIAL PRIMARY KEY,
    merchant_id INTEGER NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    action VARCHAR(50) NOT NULL,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100)
);

-- Verify merchant_audit_logs table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'merchant_audit_logs') THEN
        RAISE EXCEPTION 'Table merchant_audit_logs was not created successfully';
    END IF;
END $$;

-- Insert default merchant statuses
INSERT INTO fee_nominal.merchant_statuses (code, name, description) VALUES
    ('ACTIVE', 'Active', 'Merchant is active and can process transactions'),
    ('INACTIVE', 'Inactive', 'Merchant is inactive and cannot process transactions'),
    ('SUSPENDED', 'Suspended', 'Merchant is temporarily suspended'),
    ('TERMINATED', 'Terminated', 'Merchant account has been terminated')
ON CONFLICT (code) DO NOTHING;

-- Verify data insertion
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM fee_nominal.merchant_statuses WHERE code = 'ACTIVE') THEN
        RAISE EXCEPTION 'Default merchant statuses were not inserted successfully';
    END IF;
END $$;

/* -- Down Migration
DROP TABLE IF EXISTS fee_nominal.merchant_audit_logs CASCADE;
DROP TABLE IF EXISTS fee_nominal.surcharge_provider_config_history CASCADE;
DROP TABLE IF EXISTS fee_nominal.surcharge_provider_configs CASCADE;
DROP TABLE IF EXISTS fee_nominal.surcharge_providers CASCADE;
DROP TABLE IF EXISTS fee_nominal.merchants CASCADE;
DROP TABLE IF EXISTS fee_nominal.merchant_statuses CASCADE;  */