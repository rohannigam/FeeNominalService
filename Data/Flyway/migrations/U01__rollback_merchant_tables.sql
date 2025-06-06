-- Rollback: Drop merchant tables
DROP TABLE IF EXISTS surcharge_provider_config_history CASCADE;
DROP TABLE IF EXISTS surcharge_provider_configs CASCADE;
DROP TABLE IF EXISTS surcharge_providers CASCADE;
DROP TABLE IF EXISTS merchants CASCADE;
DROP TABLE IF EXISTS merchant_statuses CASCADE; 