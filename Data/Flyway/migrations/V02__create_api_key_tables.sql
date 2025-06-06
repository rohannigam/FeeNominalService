-- Create schema
CREATE SCHEMA IF NOT EXISTS fee_nominal;

-- Set search path
SET search_path TO fee_nominal;

-- Create api_keys table
CREATE TABLE IF NOT EXISTS api_keys (
    api_key_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES merchants(merchant_id),
    key VARCHAR(64) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    rate_limit INTEGER NOT NULL DEFAULT 1000,
    allowed_endpoints TEXT[] NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    expiration_days INTEGER NOT NULL DEFAULT 30,
    expires_at TIMESTAMP WITH TIME ZONE,
    last_used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_rotated_at TIMESTAMP WITH TIME ZONE,
    revoked_at TIMESTAMP WITH TIME ZONE,
    created_by VARCHAR(50) NOT NULL,
    onboarding_reference VARCHAR(50),
    purpose VARCHAR(50)
);

-- Create api_key_usage table for rate limiting
CREATE TABLE IF NOT EXISTS api_key_usage (
    api_key_usage_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES api_keys(api_key_id),
    endpoint VARCHAR(255) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    request_count INTEGER NOT NULL DEFAULT 1,
    window_start TIMESTAMP WITH TIME ZONE NOT NULL,
    window_end TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create api_key_secrets table
CREATE TABLE IF NOT EXISTS api_key_secrets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key VARCHAR(100) UNIQUE NOT NULL,
    merchant_id VARCHAR(50) NOT NULL,
    secret VARCHAR(512) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_rotated TIMESTAMP WITH TIME ZONE,
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMP WITH TIME ZONE,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    expires_at TIMESTAMP WITH TIME ZONE
); 