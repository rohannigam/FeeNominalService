-- =============================================
-- SCHEMA CREATION
-- =============================================

-- Create schema
CREATE SCHEMA IF NOT EXISTS fee_nominal;

-- Set search path
SET search_path TO fee_nominal;

-- Create merchant_statuses table
CREATE TABLE IF NOT EXISTS fee_nominal.merchant_statuses (
    merchant_status_id INTEGER PRIMARY KEY,
    code VARCHAR(20) UNIQUE NOT NULL,           -- e.g., 'ACTIVE', 'INACTIVE', 'SUSPENDED'
    name VARCHAR(50) NOT NULL,                  -- Display name
    description TEXT,                           -- Detailed description
    is_active BOOLEAN NOT NULL DEFAULT true,    -- Whether this status is currently in use
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create merchants table
CREATE TABLE IF NOT EXISTS fee_nominal.merchants (
    merchant_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_merchant_id VARCHAR(50) UNIQUE NOT NULL,
    external_merchant_guid UUID UNIQUE,
    name VARCHAR(255) NOT NULL,
    status_id INTEGER NOT NULL REFERENCES fee_nominal.merchant_statuses(merchant_status_id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL
);

-- Create surcharge_providers table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_providers (
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
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_provider_configs (
    surcharge_provider_config_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider_id UUID NOT NULL REFERENCES fee_nominal.surcharge_providers(surcharge_provider_id),
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
    UNIQUE(provider_id, merchant_id, config_name)
);

-- Create surcharge_provider_config_history table
CREATE TABLE IF NOT EXISTS fee_nominal.surcharge_provider_config_history (
    surcharge_provider_config_history_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    config_id UUID NOT NULL REFERENCES fee_nominal.surcharge_provider_configs(surcharge_provider_config_id),
    action VARCHAR(50) NOT NULL,
    previous_values JSONB,
    new_values JSONB,
    changed_by VARCHAR(50) NOT NULL,
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reason TEXT
);

-- Create api_keys table
CREATE TABLE IF NOT EXISTS fee_nominal.api_keys (
    api_key_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
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
CREATE TABLE IF NOT EXISTS fee_nominal.api_key_usage (
    api_key_usage_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES fee_nominal.api_keys(api_key_id),
    endpoint VARCHAR(255) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    request_count INTEGER NOT NULL DEFAULT 1,
    window_start TIMESTAMP WITH TIME ZONE NOT NULL,
    window_end TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create audit_logs table
CREATE TABLE IF NOT EXISTS fee_nominal.audit_logs (
    audit_log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type VARCHAR(50) NOT NULL,  -- 'MERCHANT', 'API_KEY', 'SERVICE_PROVIDER', etc.
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,       -- 'CREATE', 'UPDATE', 'DELETE', 'REVOKE', etc.
    old_values JSONB,                  -- Previous state
    new_values JSONB,                  -- New state
    performed_by VARCHAR(50) NOT NULL, -- Who made the change
    performed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address VARCHAR(45),            -- IP address of the requester
    user_agent TEXT,                   -- User agent of the requester
    additional_info JSONB              -- Any additional context
);

-- Create transaction table
CREATE TABLE IF NOT EXISTS fee_nominal.transactions (
    transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    surcharge_provider_id UUID REFERENCES fee_nominal.surcharge_providers(surcharge_provider_id),
    surcharge_provider_config_id UUID REFERENCES fee_nominal.surcharge_provider_configs(surcharge_provider_config_id),
    amount DECIMAL(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    surcharge_amount DECIMAL(19,4) NOT NULL,
    total_amount DECIMAL(19,4) NOT NULL,
    status VARCHAR(20) NOT NULL,
    external_reference VARCHAR(100),              -- External system's transaction reference
    external_source VARCHAR(50) NOT NULL,         -- Source system (e.g., 'SOAP_API', 'REST_API')
    external_transaction_id VARCHAR(100) NOT NULL, -- External system's transaction ID
    service_provider_response JSONB,
    service_provider_error JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create batch_transaction table
CREATE TABLE IF NOT EXISTS fee_nominal.batch_transactions (
    batch_transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    surcharge_provider_id UUID REFERENCES fee_nominal.surcharge_providers(surcharge_provider_id),
    surcharge_provider_config_id UUID REFERENCES fee_nominal.surcharge_provider_configs(surcharge_provider_config_id),
    batch_reference VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL,
    total_transactions INTEGER NOT NULL,
    successful_transactions INTEGER NOT NULL DEFAULT 0,
    failed_transactions INTEGER NOT NULL DEFAULT 0,
    external_reference VARCHAR(100),              -- External system's batch reference
    external_source VARCHAR(50) NOT NULL,         -- Source system (e.g., 'SOAP_API', 'REST_API')
    external_batch_id VARCHAR(100) NOT NULL,      -- External system's batch ID
    service_provider_response JSONB,
    service_provider_error JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE
);

-- Create authentication_attempt table
CREATE TABLE IF NOT EXISTS fee_nominal.authentication_attempts (
    authentication_attempt_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID REFERENCES fee_nominal.api_keys(api_key_id),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    ip_address VARCHAR(45) NOT NULL,
    user_agent VARCHAR(500) NOT NULL,
    status VARCHAR(20) NOT NULL,
    failure_reason VARCHAR(500),
    attempted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    success BOOLEAN NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create api_key_secrets table for local development
CREATE TABLE IF NOT EXISTS fee_nominal.api_key_secrets (
    id UUID PRIMARY KEY,
    api_key VARCHAR(100) UNIQUE NOT NULL,
    merchant_id VARCHAR(50) NOT NULL,
    secret VARCHAR(255) NOT NULL,  -- Updated to match V08 migration
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    last_rotated TIMESTAMP WITH TIME ZONE,
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE'
);

-- Create merchant_audit_trail table
CREATE TABLE IF NOT EXISTS fee_nominal.merchant_audit_trail (
    audit_trail_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    external_merchant_id VARCHAR(50) NOT NULL,
    action VARCHAR(50) NOT NULL,  -- 'CREATE', 'UPDATE'
    field_name VARCHAR(100) NOT NULL,
    old_value TEXT,
    new_value TEXT,
    changed_by VARCHAR(50) NOT NULL,
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reason TEXT
);

-- =============================================
-- INDEXES
-- =============================================

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_merchant_statuses_code ON fee_nominal.merchant_statuses(code);
CREATE INDEX IF NOT EXISTS idx_merchants_external_merchant_id ON fee_nominal.merchants(external_merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchants_status_id ON fee_nominal.merchants(status_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_code ON fee_nominal.surcharge_providers(code);
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_status ON fee_nominal.surcharge_providers(status);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_provider_id ON fee_nominal.surcharge_provider_configs(provider_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_merchant_id ON fee_nominal.surcharge_provider_configs(merchant_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_is_active ON fee_nominal.surcharge_provider_configs(is_active);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_config_id ON fee_nominal.surcharge_provider_config_history(config_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_merchant_id ON fee_nominal.api_keys(merchant_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_key ON fee_nominal.api_keys(key);
CREATE INDEX IF NOT EXISTS idx_api_keys_status ON fee_nominal.api_keys(status);
CREATE INDEX IF NOT EXISTS idx_api_keys_expires_at ON fee_nominal.api_keys(expires_at);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_api_key_id ON fee_nominal.api_key_usage(api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_window ON fee_nominal.api_key_usage(window_start, window_end);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON fee_nominal.audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_performed_at ON fee_nominal.audit_logs(performed_at);
CREATE INDEX IF NOT EXISTS idx_transactions_merchant_id ON fee_nominal.transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_transactions_surcharge_provider_id ON fee_nominal.transactions(surcharge_provider_id);
CREATE INDEX IF NOT EXISTS idx_transactions_surcharge_provider_config_id ON fee_nominal.transactions(surcharge_provider_config_id);
CREATE INDEX IF NOT EXISTS idx_transactions_created_at ON fee_nominal.transactions(created_at);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_merchant_id ON fee_nominal.batch_transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_surcharge_provider_id ON fee_nominal.batch_transactions(surcharge_provider_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_surcharge_provider_config_id ON fee_nominal.batch_transactions(surcharge_provider_config_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_batch_reference ON fee_nominal.batch_transactions(batch_reference);
CREATE INDEX IF NOT EXISTS idx_authentication_attempts_api_key_id ON fee_nominal.authentication_attempts(api_key_id);
CREATE INDEX IF NOT EXISTS idx_authentication_attempts_timestamp ON fee_nominal.authentication_attempts(timestamp);
CREATE INDEX IF NOT EXISTS idx_transactions_external_transaction_id ON fee_nominal.transactions(external_transaction_id);
CREATE INDEX IF NOT EXISTS idx_transactions_external_source ON fee_nominal.transactions(external_source);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_external_batch_id ON fee_nominal.batch_transactions(external_batch_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_external_source ON fee_nominal.batch_transactions(external_source);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_api_key ON fee_nominal.api_key_secrets(api_key);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_merchant_id ON fee_nominal.api_key_secrets(merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_merchant_id ON fee_nominal.merchant_audit_trail(merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_external_merchant_id ON fee_nominal.merchant_audit_trail(external_merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_changed_at ON fee_nominal.merchant_audit_trail(changed_at);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_action ON fee_nominal.merchant_audit_trail(action);
CREATE INDEX IF NOT EXISTS idx_merchants_external_merchant_guid ON fee_nominal.merchants(external_merchant_guid);

-- =============================================
-- STORED PROCEDURES AND TRIGGERS
-- =============================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION fee_nominal.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at
CREATE TRIGGER update_merchants_updated_at
    BEFORE UPDATE ON fee_nominal.merchants
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_surcharge_providers_updated_at
    BEFORE UPDATE ON fee_nominal.surcharge_providers
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_surcharge_provider_configs_updated_at
    BEFORE UPDATE ON fee_nominal.surcharge_provider_configs
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_api_keys_updated_at
    BEFORE UPDATE ON fee_nominal.api_keys
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_transactions_updated_at
    BEFORE UPDATE ON fee_nominal.transactions
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_batch_transactions_updated_at
    BEFORE UPDATE ON fee_nominal.batch_transactions
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

-- Add trigger for api_key_secrets updated_at
CREATE TRIGGER update_api_key_secrets_updated_at
    BEFORE UPDATE ON fee_nominal.api_key_secrets
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column(); 