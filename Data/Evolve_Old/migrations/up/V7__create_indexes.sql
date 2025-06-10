CREATE INDEX IF NOT EXISTS idx_merchants_status ON fee_nominal.merchants(merchant_status_id);
CREATE INDEX IF NOT EXISTS idx_merchants_created_at ON fee_nominal.merchants(created_at);

CREATE INDEX IF NOT EXISTS idx_surcharge_providers_code ON fee_nominal.surcharge_providers(code);
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_created_at ON fee_nominal.surcharge_providers(created_at);

CREATE INDEX IF NOT EXISTS idx_provider_configs_merchant ON fee_nominal.surcharge_provider_configs(merchant_id);
CREATE INDEX IF NOT EXISTS idx_provider_configs_provider ON fee_nominal.surcharge_provider_configs(surcharge_provider_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_is_active ON fee_nominal.surcharge_provider_configs(is_active);

CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_config_id ON fee_nominal.surcharge_provider_config_history(surcharge_provider_config_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_created_at ON fee_nominal.surcharge_provider_config_history(created_at);

CREATE INDEX IF NOT EXISTS idx_api_keys_merchant ON fee_nominal.api_keys(merchant_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_created_at ON fee_nominal.api_keys(created_at);
CREATE INDEX IF NOT EXISTS idx_api_keys_expires_at ON fee_nominal.api_keys(expires_at);

CREATE INDEX IF NOT EXISTS idx_api_key_secrets_is_active ON fee_nominal.api_key_secrets(is_active);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_created_at ON fee_nominal.api_key_secrets(created_at);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_last_used_at ON fee_nominal.api_key_secrets(last_used_at);

CREATE INDEX IF NOT EXISTS idx_transactions_merchant ON fee_nominal.transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_transactions_created_at ON fee_nominal.transactions(created_at);
CREATE INDEX IF NOT EXISTS idx_transactions_provider ON fee_nominal.transactions(surcharge_provider_id);

CREATE INDEX IF NOT EXISTS idx_audit_trail_merchant ON fee_nominal.merchant_audit_trail(merchant_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at_entity_type ON fee_nominal.audit_logs(created_at, entity_type);
CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON fee_nominal.audit_logs(action);

CREATE INDEX IF NOT EXISTS idx_batch_transactions_merchant ON fee_nominal.batch_transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_provider ON fee_nominal.batch_transactions(surcharge_provider_id);
CREATE INDEX IF NOT EXISTS idx_batch_items_batch ON fee_nominal.batch_transaction_items(batch_transaction_id);
CREATE INDEX IF NOT EXISTS idx_auth_attempts_api_key ON fee_nominal.api_key_usage_logs(api_key_id);
