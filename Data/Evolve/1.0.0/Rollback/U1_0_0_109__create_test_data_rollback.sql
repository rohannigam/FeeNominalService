/*
Rollback: U1_0_0_109__create_test_data_rollback.sql
Description: Removes test data from the database
Dependencies: None
Changes:
- Removes test API key secrets
- Removes test API keys
- Removes test provider configurations
- Removes test providers
- Removes test merchants
- Removes test merchant statuses
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_109__create_test_data_rollback.sql which is a rollback of V1_0_0_9__create_test_data...';
END $$;

-- Remove test API key secrets
DELETE FROM fee_nominal.api_key_secrets WHERE api_key_id IN (SELECT api_key_id FROM fee_nominal.api_keys WHERE key_name LIKE 'Test Key%');
DO $$
BEGIN
    RAISE NOTICE 'Removed test API key secrets';
END $$;

-- Remove test API keys
DELETE FROM fee_nominal.api_keys WHERE key_name LIKE 'Test Key%';
DO $$
BEGIN
    RAISE NOTICE 'Removed test API keys';
END $$;

-- Remove test provider configurations
DELETE FROM fee_nominal.surcharge_provider_configs 
WHERE merchant_id IN (SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code LIKE 'TEST_%')
AND surcharge_provider_id IN (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code LIKE 'TEST_%');
DO $$
BEGIN
    RAISE NOTICE 'Removed test provider configurations';
END $$;

-- Remove test providers
DELETE FROM fee_nominal.surcharge_providers WHERE code LIKE 'TEST_%';
DO $$
BEGIN
    RAISE NOTICE 'Removed test providers';
END $$;

-- Remove test merchants
DELETE FROM fee_nominal.merchants WHERE merchant_code LIKE 'TEST_%';
DO $$
BEGIN
    RAISE NOTICE 'Removed test merchants';
END $$;

-- Remove test merchant statuses
DELETE FROM fee_nominal.merchant_statuses WHERE code LIKE 'TEST_%';
DO $$
BEGIN
    RAISE NOTICE 'Removed test merchant statuses';
END $$;

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM fee_nominal.merchant_statuses WHERE code LIKE 'TEST_%'
    ) THEN
        RAISE EXCEPTION 'Some test merchant statuses were not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM fee_nominal.merchants WHERE merchant_code LIKE 'TEST_%'
    ) THEN
        RAISE EXCEPTION 'Some test merchants were not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM fee_nominal.surcharge_providers WHERE code LIKE 'TEST_%'
    ) THEN
        RAISE EXCEPTION 'Some test providers were not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM fee_nominal.surcharge_provider_configs 
        WHERE merchant_id IN (SELECT merchant_id FROM fee_nominal.merchants WHERE merchant_code LIKE 'TEST_%')
        AND surcharge_provider_id IN (SELECT surcharge_provider_id FROM fee_nominal.surcharge_providers WHERE code LIKE 'TEST_%')
    ) THEN
        RAISE EXCEPTION 'Some test provider configurations were not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM fee_nominal.api_keys WHERE key_name LIKE 'Test Key%'
    ) THEN
        RAISE EXCEPTION 'Some test API keys were not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM fee_nominal.api_key_secrets 
        WHERE api_key_id IN (SELECT api_key_id FROM fee_nominal.api_keys WHERE key_name LIKE 'Test Key%')
    ) THEN
        RAISE EXCEPTION 'Some test API key secrets were not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all test data was removed successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_109__create_test_data_rollback.sql which is a rollback of V1_0_0_9__create_test_data successfully';
END $$; 