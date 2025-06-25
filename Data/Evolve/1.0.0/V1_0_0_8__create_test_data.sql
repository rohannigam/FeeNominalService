/*
Migration: V1_0_0_8__create_test_data.sql
Description: Creates test data for development and testing environments
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchant tables)
- V1_0_0_3__create_api_key_tables.sql (requires api_key tables)
Changes:
- Inserts test merchant data
- Creates test API keys
- Sets up test configurations
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_8__create_test_data migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Temporarily disable audit triggers
ALTER TABLE fee_nominal.merchants DISABLE TRIGGER audit_merchants;
ALTER TABLE fee_nominal.api_keys DISABLE TRIGGER audit_api_keys;
ALTER TABLE fee_nominal.transactions DISABLE TRIGGER audit_transactions;

DO $$
BEGIN
    RAISE NOTICE 'Disabled audit triggers for test data insertion';
END $$;

-- Insert test merchant statuses
INSERT INTO fee_nominal.merchant_statuses (code, name, description)
VALUES 
    ('TEST_ACTIVE', 'Test Active', 'Test merchant is active and can process transactions'),
    ('TEST_INACTIVE', 'Test Inactive', 'Test merchant is inactive and cannot process transactions'),
    ('TEST_SUSPENDED', 'Test Suspended', 'Test merchant is temporarily suspended')
ON CONFLICT (code) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test merchant statuses';
END $$;

-- Insert test merchants
INSERT INTO fee_nominal.merchants (name, external_merchant_id, status_id, created_by)
VALUES 
    ('Test Merchant 1', 'TEST_MERCHANT_1', (SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'TEST_ACTIVE'), 'SYSTEM'),
    ('Test Merchant 2', 'TEST_MERCHANT_2', (SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'TEST_ACTIVE'), 'SYSTEM'),
    ('Test Merchant 3', 'TEST_MERCHANT_3', (SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'TEST_INACTIVE'), 'SYSTEM')
ON CONFLICT (external_merchant_id) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test merchants';
END $$;

-- Insert test surcharge providers
INSERT INTO fee_nominal.surcharge_providers (
    code, 
    name, 
    base_url, 
    credentials_schema, 
    status_id,
    created_by,
    updated_by
)
VALUES 
    ('TEST_PROVIDER1', 'Test Provider 1', 'https://test-provider1.example.com', '{"api_key": "string", "secret": "string"}', 
     (SELECT status_id FROM fee_nominal.surcharge_provider_statuses WHERE code = 'ACTIVE'),
     'SYSTEM', 'SYSTEM'),
    ('TEST_PROVIDER2', 'Test Provider 2', 'https://test-provider2.example.com', '{"api_key": "string", "secret": "string"}',
     (SELECT status_id FROM fee_nominal.surcharge_provider_statuses WHERE code = 'ACTIVE'),
     'SYSTEM', 'SYSTEM');

DO $$
BEGIN
    RAISE NOTICE 'Inserted test surcharge providers';
END $$;

-- Insert test provider configurations
INSERT INTO fee_nominal.surcharge_provider_configs (
    merchant_id, 
    surcharge_provider_id, 
    config_name,
    api_version,
    is_active, 
    credentials,
    created_by,
    updated_by
)
VALUES 
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_1'), 
     (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'TEST_PROVIDER1'),
     'Default Config',
     '1.0',
     true, 
     '{"api_key": "test_key_1", "secret": "test_secret_1"}',
     'SYSTEM',
     'SYSTEM'),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_1'), 
     (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'TEST_PROVIDER2'),
     'Default Config',
     '1.0',
     true, 
     '{"api_key": "test_key_2", "secret": "test_secret_2"}',
     'SYSTEM',
     'SYSTEM'),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_2'), 
     (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'TEST_PROVIDER1'),
     'Default Config',
     '1.0',
     true, 
     '{"api_key": "test_key_3", "secret": "test_secret_3"}',
     'SYSTEM',
     'SYSTEM');

DO $$
BEGIN
    RAISE NOTICE 'Inserted test provider configurations';
END $$;

-- Insert test API keys
INSERT INTO fee_nominal.api_keys (
    merchant_id,
    name,
    key,
    status,
    is_active,
    created_by
)
VALUES 
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_1'), 'Test Key 1', 'test_key_1', 'Active', true, 'SYSTEM'),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_1'), 'Test Key 2', 'test_key_2', 'Active', true, 'SYSTEM'),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_2'), 'Test Key 3', 'test_key_3', 'Active', true, 'SYSTEM')
ON CONFLICT (merchant_id, name) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test API keys';
END $$;

-- Insert test API key secrets
INSERT INTO fee_nominal.api_key_secrets (
    api_key,
    secret,
    merchant_id,
    status,
    is_revoked,
    created_at
)
VALUES 
    ('test_key_1', 'test_secret_1', (SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_1'), 'Active', false, CURRENT_TIMESTAMP),
    ('test_key_2', 'test_secret_2', (SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_1'), 'Active', false, CURRENT_TIMESTAMP),
    ('test_key_3', 'test_secret_3', (SELECT merchant_id FROM fee_nominal.merchants WHERE external_merchant_id = 'TEST_MERCHANT_2'), 'Active', false, CURRENT_TIMESTAMP)
ON CONFLICT (api_key) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test API key secrets';
END $$;

-- Re-enable audit triggers
ALTER TABLE fee_nominal.merchants ENABLE TRIGGER audit_merchants;
ALTER TABLE fee_nominal.api_keys ENABLE TRIGGER audit_api_keys;
ALTER TABLE fee_nominal.transactions ENABLE TRIGGER audit_transactions;

DO $$
BEGIN
    RAISE NOTICE 'Re-enabled audit triggers';
END $$;

-- Verify test data
DO $$ 
DECLARE
    v_merchant_count INTEGER;
    v_api_key_count INTEGER;
    v_provider_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_merchant_count FROM fee_nominal.merchants WHERE external_merchant_id LIKE 'TEST_%';
    SELECT COUNT(*) INTO v_api_key_count FROM fee_nominal.api_keys WHERE name LIKE 'Test Key%';
    SELECT COUNT(*) INTO v_provider_count FROM fee_nominal.surcharge_providers WHERE code LIKE 'TEST_%';
    
    IF v_merchant_count < 3 THEN
        RAISE EXCEPTION 'Not all test merchants were created. Expected 3, found %', v_merchant_count;
    END IF;
    
    IF v_api_key_count < 3 THEN
        RAISE EXCEPTION 'Not all test API keys were created. Expected 3, found %', v_api_key_count;
    END IF;
    
    IF v_provider_count < 2 THEN
        RAISE EXCEPTION 'Not all test providers were created. Expected 2, found %', v_provider_count;
    END IF;
    
    RAISE NOTICE 'Verified creation of all test data';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_8__create_test_data migration successfully';
END $$;
