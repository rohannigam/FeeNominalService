-- Up Migration

-- Temporarily disable audit triggers
ALTER TABLE fee_nominal.merchants DISABLE TRIGGER audit_merchants;
ALTER TABLE fee_nominal.api_keys DISABLE TRIGGER audit_api_keys;
ALTER TABLE fee_nominal.transactions DISABLE TRIGGER audit_transactions;

-- Insert merchant statuses
INSERT INTO fee_nominal.merchant_statuses (code, name, description) VALUES
    ('ACTIVE', 'Active', 'Merchant is active and can process transactions'),
    ('INACTIVE', 'Inactive', 'Merchant is inactive and cannot process transactions'),
    ('SUSPENDED', 'Suspended', 'Merchant is temporarily suspended'),
    ('TERMINATED', 'Terminated', 'Merchant account has been terminated')
ON CONFLICT (code) DO NOTHING;

-- Insert test merchant
WITH active_status AS (
    SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'ACTIVE' LIMIT 1
)
INSERT INTO fee_nominal.merchants (merchant_name, merchant_code, merchant_status_id, description, is_active)
SELECT 
    'Development Merchant',
    'DEV001',
    merchant_status_id,
    'Test merchant for development',
    true
FROM active_status
ON CONFLICT (merchant_code) DO NOTHING;

-- Insert test surcharge provider
INSERT INTO fee_nominal.surcharge_providers (name, code, base_url, credentials_schema) VALUES
('InterPayments', 'INTERPAY', 
 'https://api-test.interpayments.com',
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
 }')
ON CONFLICT (code) DO NOTHING;

-- Insert test provider configurations
WITH merchant AS (
    SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'DEV001' LIMIT 1
),
provider AS (
    SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'INTERPAY' LIMIT 1
)
INSERT INTO fee_nominal.surcharge_provider_configs (
    surcharge_provider_id,
    merchant_id,
    credentials,
    is_active
)
SELECT 
    surcharge_provider_id,
    merchant_id,
    '{
        "api_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MzcwNjU3ODAsIm5hbWUiOiJwYXltZXRyaWMtdGVzdCIsImlkIjoiNmoyY3hjZ21qNGd5NnQyMWo2bnRobmw2byIsImRhdGEiOlsicGF5bWV0cmljLXRlc3QiXSwiZSI6InRlc3QifQ.YtftW6Ev0WlMVfjwqJFZLJUWyL0UnCiSdCyqic64qTs",
        "merchant_id": "IP123456"
    }',
    true
FROM merchant m, provider p
ON CONFLICT (surcharge_provider_id, merchant_id) DO NOTHING;

-- Insert test configuration history
WITH config AS (
    SELECT surcharge_provider_config_id FROM fee_nominal.surcharge_provider_configs 
    WHERE merchant_id IN (SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'DEV001')
    LIMIT 1
)
INSERT INTO fee_nominal.surcharge_provider_config_history (
    surcharge_provider_config_id,
    action,
    previous_value,
    new_value
)
SELECT 
    surcharge_provider_config_id,
    'CREATED',
    NULL,
    '{
        "api_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE3MzcwNjU3ODAsIm5hbWUiOiJpbnRlcnBheS1wcmltYXJ5IiwiaWQiOiI2ajJjeGNnbWo0Z3k2dDIxajZudGhvbG42byIsImRhdGEiOlsiaW50ZXJwYXktcHJpbWFyeSJdLCJlIjoicHJpbWFyeSJ9.YtftW6Ev0WlMVfjwqJFZLJUWyL0UnCiSdCyqic64qTs",
        "merchant_id": "IP123456"
    }'
FROM config c
ON CONFLICT DO NOTHING;

-- Insert test merchant audit log
WITH merchant AS (
    SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'DEV001' LIMIT 1
)
INSERT INTO fee_nominal.merchant_audit_logs (
    merchant_id,
    action,
    details,
    created_by
)
SELECT 
    merchant_id,
    'CREATED',
    '{
        "merchant_name": "Development Merchant",
        "merchant_code": "DEV001",
        "status": "ACTIVE"
    }',
    'admin'
FROM merchant
ON CONFLICT DO NOTHING;

-- Re-enable audit triggers
ALTER TABLE fee_nominal.merchants ENABLE TRIGGER audit_merchants;
ALTER TABLE fee_nominal.api_keys ENABLE TRIGGER audit_api_keys;
ALTER TABLE fee_nominal.transactions ENABLE TRIGGER audit_transactions;

/* -- Down Migration
DELETE FROM fee_nominal.merchant_audit_logs;
DELETE FROM fee_nominal.surcharge_provider_config_history;
DELETE FROM fee_nominal.surcharge_provider_configs;
DELETE FROM fee_nominal.surcharge_providers;
DELETE FROM fee_nominal.merchants;
DELETE FROM fee_nominal.merchant_statuses;  */