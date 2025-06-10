DROP INDEX IF EXISTS fee_nominal.idx_merchants_status;
DROP INDEX IF EXISTS fee_nominal.idx_merchants_created_at;

DROP INDEX IF EXISTS fee_nominal.idx_surcharge_providers_code;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_providers_created_at;

DROP INDEX IF EXISTS fee_nominal.idx_provider_configs_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_provider_configs_provider;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_is_active;

DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_config_history_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_config_history_config_id;

DROP INDEX IF EXISTS fee_nominal.idx_api_keys_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_expires_at;

DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_last_used_at;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_is_active;

DROP INDEX IF EXISTS fee_nominal.idx_transactions_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_provider;

DROP INDEX IF EXISTS fee_nominal.idx_audit_trail_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_created_at_entity_type;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_action;

DROP INDEX IF EXISTS fee_nominal.idx_batch_transactions_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_batch_transactions_provider;
DROP INDEX IF EXISTS fee_nominal.idx_batch_items_batch;
DROP INDEX IF EXISTS fee_nominal.idx_auth_attempts_api_key;  */
