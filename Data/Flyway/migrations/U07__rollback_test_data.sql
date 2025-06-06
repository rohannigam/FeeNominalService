-- Rollback: Remove test data
DELETE FROM merchant_audit_trail;
DELETE FROM api_key_secrets;
DELETE FROM authentication_attempts;
DELETE FROM batch_transactions;
DELETE FROM transactions;
DELETE FROM audit_logs;
DELETE FROM api_key_usage;
DELETE FROM api_keys;
DELETE FROM surcharge_provider_config_history;
DELETE FROM surcharge_provider_configs;
DELETE FROM surcharge_providers;
DELETE FROM merchants;
DELETE FROM merchant_statuses; 