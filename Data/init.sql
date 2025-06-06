-- =============================================
-- SCHEMA CREATION
-- =============================================

-- Drop existing schema if it exists (for clean start)
DROP SCHEMA IF EXISTS fee_nominal CASCADE;

-- Create schema explicitly
CREATE SCHEMA fee_nominal;

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

-- Insert default merchant statuses
INSERT INTO fee_nominal.merchant_statuses (merchant_status_id, code, name, description, is_active) VALUES
    (-2, 'SUSPENDED', 'Suspended', 'Merchant account is temporarily suspended', false),
    (-1, 'INACTIVE', 'Inactive', 'Merchant account is inactive', false),
    (0, 'UNKNOWN', 'Unknown', 'Merchant status is unknown', false),
    (1, 'ACTIVE', 'Active', 'Merchant account is active and operational', true),
    (2, 'PENDING', 'Pending', 'Merchant account is pending activation', true),
    (3, 'VERIFIED', 'Verified', 'Merchant account is verified and active', true)
ON CONFLICT (merchant_status_id) DO NOTHING;

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
    last_rotated TIMESTAMP WITH TIME ZONE,
    is_revoked BOOLEAN DEFAULT FALSE,
    revoked_at TIMESTAMP WITH TIME ZONE
);

-- Create merchant_audit_trail table
CREATE TABLE IF NOT EXISTS fee_nominal.merchant_audit_trail (
    merchant_audit_trail_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    action VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    property_name VARCHAR(100),
    old_value TEXT,
    new_value TEXT,
    updated_by VARCHAR(50) NOT NULL DEFAULT 'SYSTEM',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_merchant_audit_trail_merchant FOREIGN KEY (merchant_id) REFERENCES fee_nominal.merchants(merchant_id)
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
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_created_at ON fee_nominal.merchant_audit_trail(created_at);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_action ON fee_nominal.merchant_audit_trail(action);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_entity_type ON fee_nominal.merchant_audit_trail(entity_type);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_property_name ON fee_nominal.merchant_audit_trail(property_name);
CREATE INDEX IF NOT EXISTS idx_merchants_external_merchant_guid ON fee_nominal.merchants(external_merchant_guid);

-- =============================================
-- FUNCTIONS AND TRIGGERS
-- =============================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION fee_nominal.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Create triggers
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

-- =============================================
-- TEST DATA
-- =============================================

-- Note: The following section contains test data for development purposes only.
-- DO NOT run these inserts in production environments.

-- Insert merchant statuses
INSERT INTO fee_nominal.merchant_statuses (code, name, description) VALUES
    ('ACTIVE', 'Active', 'Merchant is active and can process transactions'),
    ('INACTIVE', 'Inactive', 'Merchant is inactive and cannot process transactions'),
    ('SUSPENDED', 'Suspended', 'Merchant is temporarily suspended'),
    ('PENDING', 'Pending', 'Merchant is pending approval'),
    ('REJECTED', 'Rejected', 'Merchant application was rejected'),
    ('TERMINATED', 'Terminated', 'Merchant account has been terminated')
ON CONFLICT (code) DO NOTHING;

-- Insert test merchant (with explicit status_id)
WITH active_status AS (
    SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'ACTIVE' LIMIT 1
)
INSERT INTO fee_nominal.merchants (external_merchant_id, name, status_id, created_by)
SELECT 
    'DEV001',
    'Development Merchant',
    merchant_status_id,
    'admin'
FROM active_status
ON CONFLICT (external_merchant_id) DO NOTHING;

-- Insert test surcharge provider
INSERT INTO fee_nominal.surcharge_providers (name, code, description, base_url, authentication_type, credentials_schema, status) VALUES
('InterPayments', 'INTERPAY', 
 'InterPayments Surcharge Service Provider TEST',
 'https://api-test.interpayments.com',
 'API_KEY',
 '{
   "required_fields": [
     {
       "name": "api_token",
       "type": "string",
       "description": "API Token for authentication",
       "format": "jwt"
     },
     {
       "name": "merchant_id",
       "type": "string",
       "description": "Merchant ID in InterPayments system",
       "format": "alphanumeric"
     }
   ]
 }',
 'ACTIVE')
ON CONFLICT (code) DO NOTHING;

-- Insert test provider configurations
WITH merchant AS (
    SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'DEV001' LIMIT 1
),
provider AS (
    SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'INTERPAY' LIMIT 1
)
INSERT INTO fee_nominal.surcharge_provider_configs (
    provider_id,
    merchant_id,
    config_name,
    api_version,
    credentials,
    is_active,
    metadata,
    created_by,
    updated_by
)
SELECT 
    surcharge_provider_id,
    merchant_id,
    'Primary',
    'v1',
    '{
        "api_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MzcwNjU3ODAsIm5hbWUiOiJwYXltZXRyaWMtdGVzdCIsImlkIjoiNmoyY3hjZ21qNGd5NnQyMWo2bnRobmw2byIsImRhdGEiOlsicGF5bWV0cmljLXRlc3QiXSwiZSI6InRlc3QifQ.YtftW6Ev0WlMVfjwqJFZLJUWyL0UnCiSdCyqic64qTs",
        "merchant_id": "IP123456"
    }',
    true,
    '{
        "rate_limit": 1000,
        "timeout": 30,
        "retry_attempts": 3,
        "webhook_url": "https://merchant.com/webhooks/interpayments"
    }',
    'admin',
    'admin'
FROM merchant m, provider p
ON CONFLICT (provider_id, merchant_id, config_name) DO NOTHING;

-- Insert backup configuration
WITH merchant AS (
    SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'DEV001' LIMIT 1
),
provider AS (
    SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'INTERPAY' LIMIT 1
)
INSERT INTO fee_nominal.surcharge_provider_configs (
    provider_id,
    merchant_id,
    config_name,
    api_version,
    credentials,
    is_active,
    metadata,
    created_by,
    updated_by
)
SELECT 
    surcharge_provider_id,
    merchant_id,
    'Backup',
    'v1',
    '{
        "api_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MzcwNjU3ODAsIm5hbWUiOiJpbnRlcnBheS1iYWNrdXAiLCJpZCI6IjZqMmN4Y2dtajRneTZ0MjFqNm50aG9sbjZvIiwiZGF0YSI6WyJpbnRlcnBheS1iYWNrdXAiXSwiZSI6ImJhY2t1cCJ9.YtftW6Ev0WlMVfjwqJFZLJUWyL0UnCiSdCyqic64qTs",
        "merchant_id": "IP123456"
    }',
    true,
    '{
        "rate_limit": 1000,
        "timeout": 30,
        "retry_attempts": 3,
        "webhook_url": "https://merchant.com/webhooks/interpayments/backup"
    }',
    'admin',
    'admin'
FROM merchant m, provider p
ON CONFLICT (provider_id, merchant_id, config_name) DO NOTHING;

-- Insert test configuration history
WITH config AS (
    SELECT surcharge_provider_config_id FROM fee_nominal.surcharge_provider_configs 
    WHERE config_name = 'Primary' 
    AND merchant_id IN (SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'DEV001')
    LIMIT 1
)
INSERT INTO fee_nominal.surcharge_provider_config_history (
    config_id,
    action,
    previous_values,
    new_values,
    changed_by,
    reason
)
SELECT 
    surcharge_provider_config_id,
    'CREATED',
    NULL,
    '{
        "api_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MzcwNjU3ODAsIm5hbWUiOiJpbnRlcnBheS1wcmltYXJ5IiwiaWQiOiI2ajJjeGNnbWo0Z3k2dDIxajZudGhvbG42byIsImRhdGEiOlsiaW50ZXJwYXktcHJpbWFyeSJdLCJlIjoicHJpbWFyeSJ9.YtftW6Ev0WlMVfjwqJFZLJUWyL0UnCiSdCyqic64qTs",
        "merchant_id": "IP123456",
        "config_name": "Primary",
        "api_version": "v1",
        "is_active": true
    }',
    'admin',
    'Initial configuration creation'
FROM config c
ON CONFLICT DO NOTHING;

-- Insert test API key
INSERT INTO fee_nominal.api_keys (
    merchant_id,
    key,
    name,
    description,
    rate_limit,
    allowed_endpoints,
    status,
    expiration_days,
    expires_at,
    created_by,
    purpose
)
SELECT 
    merchant_id,
    'test_api_key',
    'Test API Key',
    'Test API Key',
    1000,
    ARRAY['/api/v1/surchargefee/calculate', '/api/v1/surchargefee/calculate-batch', '/api/v1/refunds/process'],
    'ACTIVE',
    30,
    CURRENT_TIMESTAMP + INTERVAL '30 days',
    'admin',
    'GENERAL'
FROM fee_nominal.merchants
WHERE external_merchant_id = 'DEV001'
ON CONFLICT (key) DO NOTHING;

-- Add test data for transactions
WITH merchant AS (
    SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'DEV001' LIMIT 1
),
provider AS (
    SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'INTERPAY' LIMIT 1
),
provider_config AS (
    SELECT surcharge_provider_config_id FROM fee_nominal.surcharge_provider_configs 
    WHERE config_name = 'Primary' 
    AND merchant_id IN (SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'DEV001')
    LIMIT 1
)
INSERT INTO fee_nominal.transactions (
    merchant_id,
    surcharge_provider_id,
    surcharge_provider_config_id,
    amount,
    currency,
    surcharge_amount,
    total_amount,
    status,
    external_reference,
    external_source,
    external_transaction_id
)
SELECT 
    merchant_id,
    surcharge_provider_id,
    surcharge_provider_config_id,
    100.00,
    'USD',
    2.50,
    102.50,
    'COMPLETED',
    'REF123456',
    'SOAP_API',
    'SOAP-TXN-001'
FROM merchant m, provider p, provider_config pc
ON CONFLICT DO NOTHING;

-- Insert test API key secret
INSERT INTO fee_nominal.api_key_secrets (id, api_key, merchant_id, secret, status)
VALUES 
    ('33333333-3333-3333-3333-333333333333', 'test-api-key', 'DEV001', 'test-secret', 'ACTIVE')
ON CONFLICT (api_key) DO NOTHING; 