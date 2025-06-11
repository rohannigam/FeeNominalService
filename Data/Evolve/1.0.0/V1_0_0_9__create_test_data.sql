/*
Migration: V1_0_0_9__create_test_data.sql
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
    RAISE NOTICE 'Starting V1_0_0_9__create_test_data migration...';
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
INSERT INTO fee_nominal.merchants (merchant_name, merchant_code, merchant_status_id, description)
VALUES 
    ('Test Merchant 1', 'TEST_MERCHANT_1', (SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'TEST_ACTIVE'), 'Test merchant 1 for development'),
    ('Test Merchant 2', 'TEST_MERCHANT_2', (SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'TEST_ACTIVE'), 'Test merchant 2 for development'),
    ('Test Merchant 3', 'TEST_MERCHANT_3', (SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'TEST_INACTIVE'), 'Test merchant 3 for development')
ON CONFLICT (merchant_code) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test merchants';
END $$;

-- Insert test surcharge providers
INSERT INTO fee_nominal.surcharge_providers (code, name, base_url, credentials_schema)
VALUES 
    ('TEST_PROVIDER1', 'Test Provider 1', 'https://test-provider1.example.com', '{"api_key": "string", "secret": "string"}'),
    ('TEST_PROVIDER2', 'Test Provider 2', 'https://test-provider2.example.com', '{"api_key": "string", "secret": "string"}')
ON CONFLICT (code) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test surcharge providers';
END $$;

-- Insert test provider configurations
INSERT INTO fee_nominal.surcharge_provider_configs (
    merchant_id, 
    surcharge_provider_id, 
    is_active, 
    credentials
)
VALUES 
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'TEST_MERCHANT_1'), 
     (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'TEST_PROVIDER1'), 
     true, 
     '{"api_key": "test_key_1", "secret": "test_secret_1"}'),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'TEST_MERCHANT_1'), 
     (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'TEST_PROVIDER2'), 
     true, 
     '{"api_key": "test_key_2", "secret": "test_secret_2"}'),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'TEST_MERCHANT_2'), 
     (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code = 'TEST_PROVIDER1'), 
     true, 
     '{"api_key": "test_key_3", "secret": "test_secret_3"}')
ON CONFLICT (surcharge_provider_id, merchant_id) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test provider configurations';
END $$;

-- Insert test API keys
INSERT INTO fee_nominal.api_keys (
    merchant_id,
    name,
    is_active
)
VALUES 
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'TEST_MERCHANT_1'), 'Test Key 1', true),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'TEST_MERCHANT_1'), 'Test Key 2', true),
    ((SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code = 'TEST_MERCHANT_2'), 'Test Key 3', true)
ON CONFLICT (merchant_id, name) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted test API keys';
END $$;

-- Insert test API key secrets
INSERT INTO fee_nominal.api_key_secrets (
    api_key_id,
    secret_key,
    is_active
)
VALUES 
    ((SELECT api_key_id FROM fee_nominal.api_keys WHERE name = 'Test Key 1'), 'test_secret_1', true),
    ((SELECT api_key_id FROM fee_nominal.api_keys WHERE name = 'Test Key 2'), 'test_secret_2', true),
    ((SELECT api_key_id FROM fee_nominal.api_keys WHERE name = 'Test Key 3'), 'test_secret_3', true)
ON CONFLICT (api_key_id) DO NOTHING;
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
    SELECT COUNT(*) INTO v_merchant_count FROM fee_nominal.merchants WHERE merchant_code LIKE 'TEST_%';
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
    RAISE NOTICE 'Completed V1_0_0_9__create_test_data migration successfully';
END $$;
