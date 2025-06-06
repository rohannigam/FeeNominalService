-- Create indexes for merchant tables
CREATE INDEX IF NOT EXISTS idx_merchant_statuses_code ON merchant_statuses(code);
CREATE INDEX IF NOT EXISTS idx_merchants_external_merchant_id ON merchants(external_merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchants_status_id ON merchants(status_id);
CREATE INDEX IF NOT EXISTS idx_merchants_external_merchant_guid ON merchants(external_merchant_guid);

-- Create indexes for surcharge provider tables
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_code ON surcharge_providers(code);
CREATE INDEX IF NOT EXISTS idx_surcharge_providers_status ON surcharge_providers(status);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_provider_id ON surcharge_provider_configs(provider_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_merchant_id ON surcharge_provider_configs(merchant_id);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_is_active ON surcharge_provider_configs(is_active);
CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_config_id ON surcharge_provider_config_history(config_id);

-- Create indexes for API key tables
CREATE INDEX IF NOT EXISTS idx_api_keys_merchant_id ON api_keys(merchant_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_key ON api_keys(key);
CREATE INDEX IF NOT EXISTS idx_api_keys_status ON api_keys(status);
CREATE INDEX IF NOT EXISTS idx_api_keys_expires_at ON api_keys(expires_at);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_api_key_id ON api_key_usage(api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_window ON api_key_usage(window_start, window_end);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_api_key ON api_key_secrets(api_key);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_merchant_id ON api_key_secrets(merchant_id);

-- Create indexes for transaction tables
CREATE INDEX IF NOT EXISTS idx_transactions_merchant_id ON transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_transactions_surcharge_provider_id ON transactions(surcharge_provider_id);
CREATE INDEX IF NOT EXISTS idx_transactions_surcharge_provider_config_id ON transactions(surcharge_provider_config_id);
CREATE INDEX IF NOT EXISTS idx_transactions_created_at ON transactions(created_at);
CREATE INDEX IF NOT EXISTS idx_transactions_external_transaction_id ON transactions(external_transaction_id);
CREATE INDEX IF NOT EXISTS idx_transactions_external_source ON transactions(external_source);

CREATE INDEX IF NOT EXISTS idx_batch_transactions_merchant_id ON batch_transactions(merchant_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_surcharge_provider_id ON batch_transactions(surcharge_provider_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_surcharge_provider_config_id ON batch_transactions(surcharge_provider_config_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_batch_reference ON batch_transactions(batch_reference);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_external_batch_id ON batch_transactions(external_batch_id);
CREATE INDEX IF NOT EXISTS idx_batch_transactions_external_source ON batch_transactions(external_source);

-- Create indexes for audit tables
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_performed_at ON audit_logs(performed_at);
CREATE INDEX IF NOT EXISTS idx_authentication_attempts_api_key_id ON authentication_attempts(api_key_id);
CREATE INDEX IF NOT EXISTS idx_authentication_attempts_timestamp ON authentication_attempts(timestamp);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_merchant_id ON merchant_audit_trail(merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_external_merchant_id ON merchant_audit_trail(external_merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_timestamp ON merchant_audit_trail(timestamp);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_action ON merchant_audit_trail(action); 