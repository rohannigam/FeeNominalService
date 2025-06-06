-- =============================================
-- SCHEMA ROLLBACK
-- =============================================

-- Drop all triggers first
DROP TRIGGER IF EXISTS update_merchants_updated_at ON merchants;
DROP TRIGGER IF EXISTS update_surcharge_providers_updated_at ON surcharge_providers;
DROP TRIGGER IF EXISTS update_surcharge_provider_configs_updated_at ON surcharge_provider_configs;
DROP TRIGGER IF EXISTS update_api_keys_updated_at ON api_keys;
DROP TRIGGER IF EXISTS update_transactions_updated_at ON transactions;
DROP TRIGGER IF EXISTS update_batch_transactions_updated_at ON batch_transactions;

-- Drop all functions
DROP FUNCTION IF EXISTS update_updated_at_column();

-- Drop all tables in reverse order of dependencies
DROP TABLE IF EXISTS merchant_audit_trail;
DROP TABLE IF EXISTS api_key_secrets;
DROP TABLE IF EXISTS authentication_attempts;
DROP TABLE IF EXISTS batch_transactions;
DROP TABLE IF EXISTS transactions;
DROP TABLE IF EXISTS audit_logs;
DROP TABLE IF EXISTS api_key_usage;
DROP TABLE IF EXISTS api_keys;
DROP TABLE IF EXISTS surcharge_provider_config_history;
DROP TABLE IF EXISTS surcharge_provider_configs;
DROP TABLE IF EXISTS surcharge_providers;
DROP TABLE IF EXISTS merchants;
DROP TABLE IF EXISTS merchant_statuses;

-- Drop schema
DROP SCHEMA IF EXISTS fee_nominal; 