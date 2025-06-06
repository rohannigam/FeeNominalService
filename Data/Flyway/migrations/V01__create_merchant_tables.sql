-- Create schema
CREATE SCHEMA IF NOT EXISTS fee_nominal;

-- Set search path
SET search_path TO fee_nominal;

-- Create merchant_statuses table
CREATE TABLE IF NOT EXISTS merchant_statuses (
    merchant_status_id INTEGER PRIMARY KEY,
    code VARCHAR(20) UNIQUE NOT NULL,           -- e.g., 'ACTIVE', 'INACTIVE', 'SUSPENDED'
    name VARCHAR(50) NOT NULL,                  -- Display name
    description TEXT,                           -- Detailed description
    is_active BOOLEAN NOT NULL DEFAULT true,    -- Whether this status is currently in use
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Insert default merchant statuses
INSERT INTO merchant_statuses (merchant_status_id, code, name, description, is_active) VALUES
    (-2, 'SUSPENDED', 'Suspended', 'Merchant account is temporarily suspended', false),
    (-1, 'INACTIVE', 'Inactive', 'Merchant account is inactive', false),
    (0, 'UNKNOWN', 'Unknown', 'Merchant status is unknown', false),
    (1, 'ACTIVE', 'Active', 'Merchant account is active and operational', true),
    (2, 'PENDING', 'Pending', 'Merchant account is pending activation', true),
    (3, 'VERIFIED', 'Verified', 'Merchant account is verified and active', true)
ON CONFLICT (merchant_status_id) DO NOTHING;

-- Create merchants table
CREATE TABLE IF NOT EXISTS merchants (
    merchant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_merchant_id VARCHAR(50) UNIQUE NOT NULL,
    external_merchant_guid UUID UNIQUE,
    name VARCHAR(255) NOT NULL,
    status_id INTEGER NOT NULL REFERENCES merchant_statuses(merchant_status_id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL
);

-- Create surcharge_providers table
CREATE TABLE IF NOT EXISTS surcharge_providers (
    surcharge_provider_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    code VARCHAR(20) UNIQUE NOT NULL,
    description TEXT,
    base_url VARCHAR(255) NOT NULL,
    authentication_type VARCHAR(50) NOT NULL,
    credentials_schema JSONB NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create surcharge_provider_configs table
CREATE TABLE IF NOT EXISTS surcharge_provider_configs (
    surcharge_provider_config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider_id UUID NOT NULL REFERENCES surcharge_providers(surcharge_provider_id),
    merchant_id UUID NOT NULL REFERENCES merchants(merchant_id),
    config_name VARCHAR(100) NOT NULL,
    api_version VARCHAR(20) NOT NULL,
    credentials JSONB NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL,
    updated_by VARCHAR(50) NOT NULL,
    UNIQUE(provider_id, merchant_id, config_name)
);

-- Create surcharge_provider_config_history table
CREATE TABLE IF NOT EXISTS surcharge_provider_config_history (
    surcharge_provider_config_history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_id UUID NOT NULL REFERENCES surcharge_provider_configs(surcharge_provider_config_id),
    action VARCHAR(50) NOT NULL,
    previous_values JSONB,
    new_values JSONB,
    changed_by VARCHAR(50) NOT NULL,
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reason TEXT
); 