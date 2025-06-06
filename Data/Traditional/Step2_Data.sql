-- =============================================
-- INITIAL DATA
-- =============================================

-- Insert default merchant statuses
INSERT INTO fee_nominal.merchant_statuses (merchant_status_id, code, name, description, is_active) VALUES
    (-2, 'SUSPENDED', 'Suspended', 'Merchant account is temporarily suspended', false),
    (-1, 'INACTIVE', 'Inactive', 'Merchant account is inactive', false),
    (0, 'UNKNOWN', 'Unknown', 'Merchant status is unknown', false),
    (1, 'ACTIVE', 'Active', 'Merchant account is active and operational', true),
    (2, 'PENDING', 'Pending', 'Merchant account is pending activation', true),
    (3, 'VERIFIED', 'Verified', 'Merchant account is verified and active', true)
ON CONFLICT (merchant_status_id) DO NOTHING;

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