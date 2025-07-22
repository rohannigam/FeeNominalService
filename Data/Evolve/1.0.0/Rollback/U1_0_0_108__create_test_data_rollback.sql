/*
Rollback: U1_0_0_108__create_test_data_rollback.sql
Description: Removes test data created by V1_0_0_8__create_test_data.sql
Dependencies: V1_0_0_8__create_test_data.sql
Changes:
- Removes test merchants
- Removes test API keys
- Removes test configurations
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_108__create_test_data_rollback...';
END $$;

-- Temporarily disable audit triggers
ALTER TABLE fee_nominal.merchants DISABLE TRIGGER audit_merchants;
ALTER TABLE fee_nominal.api_keys DISABLE TRIGGER audit_api_keys;

DO $$
BEGIN
    RAISE NOTICE 'Disabled audit triggers for test data removal';
END $$;

-- Remove test API key secrets
DELETE FROM fee_nominal.api_key_secrets 
WHERE api_key IN ('test_key_1', 'test_key_2', 'test_key_3');
DO $$
BEGIN
    RAISE NOTICE 'Removed test API key secrets';
END $$;

-- Remove test API keys
DELETE FROM fee_nominal.api_keys 
WHERE name IN ('Test Key 1', 'Test Key 2', 'Test Key 3');
DO $$
BEGIN
    RAISE NOTICE 'Removed test API keys';
END $$;

-- Remove test provider configurations
DELETE FROM fee_nominal.surcharge_provider_configs 
WHERE credentials::jsonb->>'api_key' IN ('test_key_1', 'test_key_2', 'test_key_3');
DO $$
BEGIN
    RAISE NOTICE 'Removed test provider configurations';
END $$;

-- Remove test providers
DELETE FROM fee_nominal.surcharge_providers 
WHERE code IN ('TEST_PROVIDER1', 'TEST_PROVIDER2');
DO $$
BEGIN
    RAISE NOTICE 'Removed test providers';
END $$;

-- Remove test merchants
DELETE FROM fee_nominal.merchants 
WHERE external_merchant_id IN ('TEST_MERCHANT_1', 'TEST_MERCHANT_2', 'TEST_MERCHANT_3');
DO $$
BEGIN
    RAISE NOTICE 'Removed test merchants';
END $$;

-- Remove test merchant statuses
DELETE FROM fee_nominal.merchant_statuses 
WHERE code IN ('TEST_ACTIVE', 'TEST_INACTIVE', 'TEST_SUSPENDED');
DO $$
BEGIN
    RAISE NOTICE 'Removed test merchant statuses';
END $$;

-- Re-enable audit triggers
ALTER TABLE fee_nominal.merchants ENABLE TRIGGER audit_merchants;
ALTER TABLE fee_nominal.api_keys ENABLE TRIGGER audit_api_keys;

DO $$
BEGIN
    RAISE NOTICE 'Re-enabled audit triggers';
END $$;

-- Verify rollback
DO $$ 
DECLARE
    v_merchant_count INTEGER;
    v_api_key_count INTEGER;
    v_provider_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_merchant_count FROM fee_nominal.merchants WHERE external_merchant_id LIKE 'TEST_%';
    SELECT COUNT(*) INTO v_api_key_count FROM fee_nominal.api_keys WHERE name LIKE 'Test Key%';
    SELECT COUNT(*) INTO v_provider_count FROM fee_nominal.surcharge_providers WHERE code LIKE 'TEST_%';
    
    IF v_merchant_count > 0 THEN
        RAISE EXCEPTION 'Not all test merchants were removed. Found % remaining', v_merchant_count;
    END IF;
    
    IF v_api_key_count > 0 THEN
        RAISE EXCEPTION 'Not all test API keys were removed. Found % remaining', v_api_key_count;
    END IF;
    
    IF v_provider_count > 0 THEN
        RAISE EXCEPTION 'Not all test providers were removed. Found % remaining', v_provider_count;
    END IF;
    
    RAISE NOTICE 'Verified removal of all test data';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_108__create_test_data_rollback successfully';
END $$; 