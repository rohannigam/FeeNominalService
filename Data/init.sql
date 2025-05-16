-- =============================================
-- SCHEMA CREATION
-- =============================================

-- Create schema
CREATE SCHEMA IF NOT EXISTS fee_nominal;

-- Set search path
SET search_path TO fee_nominal;

-- Create merchant_statuses table
CREATE TABLE IF NOT EXISTS merchant_statuses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code VARCHAR(20) UNIQUE NOT NULL,           -- e.g., 'ACTIVE', 'INACTIVE', 'SUSPENDED'
    name VARCHAR(50) NOT NULL,                  -- Display name
    description TEXT,                           -- Detailed description
    is_active BOOLEAN NOT NULL DEFAULT true,    -- Whether this status is currently in use
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create merchants table
CREATE TABLE IF NOT EXISTS merchants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_id VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    status_id UUID NOT NULL REFERENCES merchant_statuses(id),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL
);

-- Create service_providers table
CREATE TABLE IF NOT EXISTS service_providers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
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

-- Create service_provider_configurations table
CREATE TABLE IF NOT EXISTS service_provider_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_provider_id UUID NOT NULL REFERENCES service_providers(id),
    merchant_id UUID NOT NULL REFERENCES merchants(id),
    configuration_name VARCHAR(100) NOT NULL,
    api_version VARCHAR(20) NOT NULL,
    credentials JSONB NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL,
    updated_by VARCHAR(50) NOT NULL,
    UNIQUE(service_provider_id, merchant_id, configuration_name)
);

-- Create service_provider_configuration_history table
CREATE TABLE IF NOT EXISTS service_provider_configuration_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    configuration_id UUID NOT NULL REFERENCES service_provider_configurations(id),
    action VARCHAR(50) NOT NULL,
    previous_values JSONB,
    new_values JSONB,
    changed_by VARCHAR(50) NOT NULL,
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    reason TEXT
);

-- Create api_keys table
CREATE TABLE IF NOT EXISTS api_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES merchants(id),
    key VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL,
    description VARCHAR(255),
    rate_limit INTEGER NOT NULL DEFAULT 1000,
    allowed_endpoints TEXT[] NOT NULL,
    status VARCHAR(20) NOT NULL,
    expiration_days INTEGER NOT NULL DEFAULT 30,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    last_used_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_rotated_at TIMESTAMP WITH TIME ZONE,
    revoked_at TIMESTAMP WITH TIME ZONE,
    created_by VARCHAR(50) NOT NULL,
    onboarding_reference VARCHAR(50)
);

-- Create api_key_usage table for rate limiting
CREATE TABLE IF NOT EXISTS api_key_usage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES api_keys(id),
    endpoint VARCHAR(255) NOT NULL,
    request_count INTEGER NOT NULL DEFAULT 1,
    window_start TIMESTAMP WITH TIME ZONE NOT NULL,
    window_end TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create audit_logs table
CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
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
CREATE TABLE IF NOT EXISTS transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES merchants(id),
    service_provider_id UUID REFERENCES service_providers(id),
    service_provider_configuration_id UUID REFERENCES service_provider_configurations(id),
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
CREATE TABLE IF NOT EXISTS batch_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES merchants(id),
    service_provider_id UUID REFERENCES service_providers(id),
    service_provider_configuration_id UUID REFERENCES service_provider_configurations(id),
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
CREATE TABLE IF NOT EXISTS authentication_attempts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID REFERENCES api_keys(id),
    ip_address VARCHAR(45) NOT NULL,
    success BOOLEAN NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- INDEXES
-- =============================================

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_merchant_statuses_code ON merchant_statuses(code);
CREATE INDEX IF NOT EXISTS idx_merchants_external_id ON merchants(external_id);
CREATE INDEX IF NOT EXISTS idx_merchants_status_id ON merchants(status_id);
CREATE INDEX IF NOT EXISTS idx_service_providers_code ON service_providers(code);
CREATE INDEX IF NOT EXISTS idx_service_providers_status ON service_providers(status);
CREATE INDEX IF NOT EXISTS idx_service_provider_configurations_service_provider_id ON service_provider_configurations(service_provider_id);
CREATE INDEX IF NOT EXISTS idx_service_provider_configurations_merchant_id ON service_provider_configurations(merchant_id);
CREATE INDEX IF NOT EXISTS idx_service_provider_configurations_is_active ON service_provider_configurations(is_active);
CREATE INDEX IF NOT EXISTS idx_service_provider_configuration_history_configuration_id ON service_provider_configuration_history(configuration_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_merchant_id ON api_keys(merchant_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_key ON api_keys(key);
CREATE INDEX IF NOT EXISTS idx_api_keys_status ON api_keys(status);
CREATE INDEX IF NOT EXISTS idx_api_keys_expires_at ON api_keys(expires_at);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_api_key_id ON api_key_usage(api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_window ON api_key_usage(window_start, window_end);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_performed_at ON audit_logs(performed_at);
CREATE INDEX IF NOT EXISTS idx_transactions_merchant_id ON transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_transactions_service_provider_id ON transactions(service_provider_id);
CREATE INDEX IF NOT EXISTS idx_transactions_service_provider_configuration_id ON transactions(service_provider_configuration_id);
CREATE INDEX IF NOT EXISTS idx_transactions_created_at ON transactions(created_at);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_merchant_id ON batch_transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_service_provider_id ON batch_transactions(service_provider_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_service_provider_configuration_id ON batch_transactions(service_provider_configuration_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_batch_reference ON batch_transactions(batch_reference);
CREATE INDEX IF NOT EXISTS idx_authentication_attempts_api_key_id ON authentication_attempts(api_key_id);
CREATE INDEX IF NOT EXISTS idx_authentication_attempts_timestamp ON authentication_attempts(timestamp);
CREATE INDEX IF NOT EXISTS idx_transactions_external_transaction_id ON transactions(external_transaction_id);
CREATE INDEX IF NOT EXISTS idx_transactions_external_source ON transactions(external_source);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_external_batch_id ON batch_transactions(external_batch_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_external_source ON batch_transactions(external_source);

-- =============================================
-- FUNCTIONS AND TRIGGERS
-- =============================================

-- Create function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at
CREATE TRIGGER update_merchants_updated_at
    BEFORE UPDATE ON merchants
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_service_providers_updated_at
    BEFORE UPDATE ON service_providers
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_service_provider_configurations_updated_at
    BEFORE UPDATE ON service_provider_configurations
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_api_keys_updated_at
    BEFORE UPDATE ON api_keys
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_transactions_updated_at
    BEFORE UPDATE ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_batch_transactions_updated_at
    BEFORE UPDATE ON batch_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- =============================================
-- TEST DATA
-- =============================================

-- Note: The following section contains test data for development purposes only.
-- DO NOT run these inserts in production environments.

-- Insert merchant statuses
INSERT INTO merchant_statuses (code, name, description) VALUES
    ('ACTIVE', 'Active', 'Merchant is active and can process transactions'),
    ('INACTIVE', 'Inactive', 'Merchant is inactive and cannot process transactions'),
    ('SUSPENDED', 'Suspended', 'Merchant is temporarily suspended'),
    ('PENDING', 'Pending', 'Merchant is pending approval'),
    ('REJECTED', 'Rejected', 'Merchant application was rejected'),
    ('TERMINATED', 'Terminated', 'Merchant account has been terminated')
ON CONFLICT (code) DO NOTHING;

-- Insert test merchant (with explicit status_id)
WITH active_status AS (
    SELECT id FROM merchant_statuses WHERE code = 'ACTIVE' LIMIT 1
)
INSERT INTO merchants (external_id, name, status_id, created_by)
SELECT 
    'DEV001',
    'Development Merchant',
    id,
    'admin'
FROM active_status
ON CONFLICT (external_id) DO NOTHING;

-- Insert test service provider
INSERT INTO service_providers (name, code, description, base_url, authentication_type, credentials_schema, status) VALUES
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

-- Insert test service provider configurations
WITH merchant AS (
    SELECT id FROM merchants WHERE external_id = 'DEV001' LIMIT 1
),
service_provider AS (
    SELECT id FROM service_providers WHERE code = 'INTERPAY' LIMIT 1
)
INSERT INTO service_provider_configurations (
    service_provider_id,
    merchant_id,
    configuration_name,
    api_version,
    credentials,
    is_active,
    metadata,
    created_by,
    updated_by
)
SELECT 
    sp.id,
    m.id,
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
FROM merchant m, service_provider sp
ON CONFLICT (service_provider_id, merchant_id, configuration_name) DO NOTHING;

-- Insert backup configuration
WITH merchant AS (
    SELECT id FROM merchants WHERE external_id = 'DEV001' LIMIT 1
),
service_provider AS (
    SELECT id FROM service_providers WHERE code = 'INTERPAY' LIMIT 1
)
INSERT INTO service_provider_configurations (
    service_provider_id,
    merchant_id,
    configuration_name,
    api_version,
    credentials,
    is_active,
    metadata,
    created_by,
    updated_by
)
SELECT 
    sp.id,
    m.id,
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
FROM merchant m, service_provider sp
ON CONFLICT (service_provider_id, merchant_id, configuration_name) DO NOTHING;

-- Insert test configuration history
WITH config AS (
    SELECT id FROM service_provider_configurations 
    WHERE configuration_name = 'Primary' 
    AND merchant_id IN (SELECT id FROM merchants WHERE external_id = 'DEV001')
    LIMIT 1
)
INSERT INTO service_provider_configuration_history (
    configuration_id,
    action,
    previous_values,
    new_values,
    changed_by,
    reason
)
SELECT 
    c.id,
    'CREATED',
    NULL,
    '{
        "api_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MzcwNjU3ODAsIm5hbWUiOiJpbnRlcnBheS1wcmltYXJ5IiwiaWQiOiI2ajJjeGNnbWo0Z3k2dDIxajZudGhvbG42byIsImRhdGEiOlsiaW50ZXJwYXktcHJpbWFyeSJdLCJlIjoicHJpbWFyeSJ9.YtftW6Ev0WlMVfjwqJFZLJUWyL0UnCiSdCyqic64qTs",
        "merchant_id": "IP123456",
        "configuration_name": "Primary",
        "api_version": "v1",
        "is_active": true
    }',
    'admin',
    'Initial configuration creation'
FROM config c
ON CONFLICT DO NOTHING;

-- Insert test API key
INSERT INTO api_keys (
    merchant_id,
    key,
    name,
    description,
    rate_limit,
    allowed_endpoints,
    status,
    expiration_days,
    expires_at,
    created_by
)
SELECT 
    m.id,
    'test_api_key',
    'Test API Key',
    'Test API Key',
    1000,
    ARRAY['/api/v1/surchargefee/calculate', '/api/v1/surchargefee/calculate-batch', '/api/v1/refunds/process'],
    'ACTIVE',
    30,
    CURRENT_TIMESTAMP + INTERVAL '30 days',
    'admin'
FROM merchants m
WHERE m.external_id = 'DEV001'
ON CONFLICT (key) DO NOTHING;

-- Add test data for transactions
WITH merchant AS (
    SELECT id FROM merchants WHERE external_id = 'DEV001' LIMIT 1
),
service_provider AS (
    SELECT id FROM service_providers WHERE code = 'INTERPAY' LIMIT 1
),
service_provider_config AS (
    SELECT id FROM service_provider_configurations 
    WHERE configuration_name = 'Primary' 
    AND merchant_id IN (SELECT id FROM merchants WHERE external_id = 'DEV001')
    LIMIT 1
)
INSERT INTO transactions (
    merchant_id,
    service_provider_id,
    service_provider_configuration_id,
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
    m.id,
    sp.id,
    spc.id,
    100.00,
    'USD',
    2.50,
    102.50,
    'COMPLETED',
    'REF123456',
    'SOAP_API',
    'SOAP-TXN-001'
FROM merchant m, service_provider sp, service_provider_config spc
ON CONFLICT DO NOTHING; 