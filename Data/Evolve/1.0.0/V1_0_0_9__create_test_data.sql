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

-- Insert test merchant statuses
INSERT INTO fee_nominal.merchant_statuses (status_id, status_name, description)
VALUES 
    (1, 'Active', 'Merchant is active and can process transactions'),
    (2, 'Inactive', 'Merchant is inactive and cannot process transactions'),
    (3, 'Suspended', 'Merchant is temporarily suspended')
ON CONFLICT (status_id) DO NOTHING;
RAISE NOTICE 'Inserted test merchant statuses';

-- Insert test merchants
INSERT INTO fee_nominal.merchants (merchant_id, merchant_name, status_id)
VALUES 
    (1, 'Test Merchant 1', 1),
    (2, 'Test Merchant 2', 1),
    (3, 'Test Merchant 3', 2)
ON CONFLICT (merchant_id) DO NOTHING;
RAISE NOTICE 'Inserted test merchants';

-- Insert test surcharge providers
INSERT INTO fee_nominal.surcharge_providers (provider_id, code, name, description)
VALUES 
    (1, 'PROVIDER1', 'Test Provider 1', 'Test provider for development'),
    (2, 'PROVIDER2', 'Test Provider 2', 'Another test provider')
ON CONFLICT (provider_id) DO NOTHING;
RAISE NOTICE 'Inserted test surcharge providers';

-- Insert test provider configurations
INSERT INTO fee_nominal.surcharge_provider_configs (
    merchant_id, 
    surcharge_provider_id, 
    is_active, 
    config_data
)
VALUES 
    (1, 1, true, '{"fee": 0.02, "min_amount": 10.00}'),
    (1, 2, true, '{"fee": 0.015, "min_amount": 5.00}'),
    (2, 1, true, '{"fee": 0.025, "min_amount": 15.00}')
ON CONFLICT (merchant_id, surcharge_provider_id) DO NOTHING;
RAISE NOTICE 'Inserted test provider configurations';

-- Insert test API keys
INSERT INTO fee_nominal.api_keys (
    api_key_id,
    merchant_id,
    key_name,
    status_id,
    expires_at
)
VALUES 
    (1, 1, 'Test Key 1', 1, CURRENT_TIMESTAMP + INTERVAL '1 year'),
    (2, 1, 'Test Key 2', 1, CURRENT_TIMESTAMP + INTERVAL '1 year'),
    (3, 2, 'Test Key 3', 1, CURRENT_TIMESTAMP + INTERVAL '1 year')
ON CONFLICT (api_key_id) DO NOTHING;
RAISE NOTICE 'Inserted test API keys';

-- Insert test API key secrets
INSERT INTO fee_nominal.api_key_secrets (
    api_key_id,
    secret_key,
    is_active,
    created_at,
    expires_at
)
VALUES 
    (1, 'test_secret_1', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 year'),
    (2, 'test_secret_2', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 year'),
    (3, 'test_secret_3', true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL '1 year')
ON CONFLICT (api_key_id) DO NOTHING;
RAISE NOTICE 'Inserted test API key secrets';

-- Verify test data
DO $$ 
DECLARE
    v_merchant_count INTEGER;
    v_api_key_count INTEGER;
    v_provider_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_merchant_count FROM fee_nominal.merchants;
    SELECT COUNT(*) INTO v_api_key_count FROM fee_nominal.api_keys;
    SELECT COUNT(*) INTO v_provider_count FROM fee_nominal.surcharge_providers;
    
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
