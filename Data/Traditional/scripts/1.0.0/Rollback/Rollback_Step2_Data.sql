-- =============================================
-- DATA ROLLBACK
-- =============================================

-- Delete test data in reverse order of dependencies
DELETE FROM api_key_secrets WHERE api_key = 'test-api-key';
DELETE FROM transactions WHERE external_transaction_id = 'SOAP-TXN-001';
DELETE FROM api_keys WHERE key = 'test_api_key';
DELETE FROM surcharge_provider_config_history WHERE action = 'CREATED';
DELETE FROM surcharge_provider_configs WHERE config_name IN ('Primary', 'Backup');
DELETE FROM surcharge_providers WHERE code = 'INTERPAY';
DELETE FROM merchants WHERE external_merchant_id = 'DEV001';
DELETE FROM merchant_statuses WHERE code IN ('SUSPENDED', 'INACTIVE', 'UNKNOWN', 'ACTIVE', 'PENDING', 'VERIFIED'); 