DROP TRIGGER IF EXISTS audit_transactions ON transactions;
DROP TRIGGER IF EXISTS audit_api_keys ON api_keys;
DROP TRIGGER IF EXISTS audit_merchants ON merchants;
DROP FUNCTION IF EXISTS log_audit_details();

DROP TRIGGER IF EXISTS log_transaction_history ON transactions;
DROP FUNCTION IF EXISTS log_transaction_history();

DROP TRIGGER IF EXISTS update_batch_transactions_updated_at ON fee_nominal.batch_transactions;
DROP TRIGGER IF EXISTS update_transactions_updated_at ON fee_nominal.transactions;
DROP TRIGGER IF EXISTS update_transaction_statuses_updated_at ON transaction_statuses;
DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON api_key_secrets;
DROP TRIGGER IF EXISTS update_api_keys_updated_at ON fee_nominal.api_keys;
DROP TRIGGER IF EXISTS update_surcharge_provider_configs_updated_at ON fee_nominal.surcharge_provider_configs;
DROP TRIGGER IF EXISTS update_surcharge_providers_updated_at ON fee_nominal.surcharge_providers;
DROP TRIGGER IF EXISTS update_merchants_updated_at ON fee_nominal.merchants;
DROP TRIGGER IF EXISTS update_merchant_statuses_updated_at ON merchant_statuses;
DROP FUNCTION IF EXISTS fee_nominal.update_updated_at_column();  */
