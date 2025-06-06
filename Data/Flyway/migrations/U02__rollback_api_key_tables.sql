-- Rollback: Drop API key tables
DROP TABLE IF EXISTS api_key_secrets CASCADE;
DROP TABLE IF EXISTS authentication_attempts CASCADE;
DROP TABLE IF EXISTS api_key_usage CASCADE;
DROP TABLE IF EXISTS api_keys CASCADE; 