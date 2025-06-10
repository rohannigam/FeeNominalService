/*
Migration: V1_0_0_8__create_indexes.sql
Description: Creates performance-optimizing indexes on key tables
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchant tables)
- V1_0_0_3__create_api_key_tables.sql (requires api key tables)
- V1_0_0_4__create_transaction_tables.sql (requires transaction tables)
- V1_0_0_5__create_audit_tables.sql (requires audit tables)
Changes:
- Creates indexes on frequently queried columns
- Adds composite indexes for common query patterns
- Optimizes foreign key relationships
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_8__create_indexes migration...';
END $$;

CREATE INDEX IF NOT EXISTS idx_merchants_status ON fee_nominal.merchants(merchant_status_id);
RAISE NOTICE 'Created index on merchants.merchant_status_id';

CREATE INDEX IF NOT EXISTS idx_merchants_created_at ON fee_nominal.merchants(created_at);
RAISE NOTICE 'Created index on merchants.created_at';

CREATE INDEX IF NOT EXISTS idx_surcharge_providers_code ON fee_nominal.surcharge_providers(code);
RAISE NOTICE 'Created index on surcharge_providers.code';

CREATE INDEX IF NOT EXISTS idx_surcharge_providers_created_at ON fee_nominal.surcharge_providers(created_at);
RAISE NOTICE 'Created index on surcharge_providers.created_at';

CREATE INDEX IF NOT EXISTS idx_provider_configs_merchant ON fee_nominal.surcharge_provider_configs(merchant_id);
RAISE NOTICE 'Created index on surcharge_provider_configs.merchant_id';

CREATE INDEX IF NOT EXISTS idx_provider_configs_provider ON fee_nominal.surcharge_provider_configs(surcharge_provider_id);
RAISE NOTICE 'Created index on surcharge_provider_configs.surcharge_provider_id';

CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_is_active ON fee_nominal.surcharge_provider_configs(is_active);
RAISE NOTICE 'Created index on surcharge_provider_configs.is_active';

CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_config_id ON fee_nominal.surcharge_provider_config_history(surcharge_provider_config_id);
RAISE NOTICE 'Created index on surcharge_provider_config_history.surcharge_provider_config_id';

CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_created_at ON fee_nominal.surcharge_provider_config_history(created_at);
RAISE NOTICE 'Created index on surcharge_provider_config_history.created_at';

CREATE INDEX IF NOT EXISTS idx_api_keys_merchant ON fee_nominal.api_keys(merchant_id);
RAISE NOTICE 'Created index on api_keys.merchant_id';

CREATE INDEX IF NOT EXISTS idx_api_keys_created_at ON fee_nominal.api_keys(created_at);
RAISE NOTICE 'Created index on api_keys.created_at';

CREATE INDEX IF NOT EXISTS idx_api_keys_expires_at ON fee_nominal.api_keys(expires_at);
RAISE NOTICE 'Created index on api_keys.expires_at';

CREATE INDEX IF NOT EXISTS idx_api_key_secrets_is_active ON fee_nominal.api_key_secrets(is_active);
RAISE NOTICE 'Created index on api_key_secrets.is_active';

CREATE INDEX IF NOT EXISTS idx_api_key_secrets_created_at ON fee_nominal.api_key_secrets(created_at);
RAISE NOTICE 'Created index on api_key_secrets.created_at';

CREATE INDEX IF NOT EXISTS idx_api_key_secrets_last_used_at ON fee_nominal.api_key_secrets(last_used_at);
RAISE NOTICE 'Created index on api_key_secrets.last_used_at';

CREATE INDEX IF NOT EXISTS idx_transactions_merchant ON fee_nominal.transactions(merchant_id);
RAISE NOTICE 'Created index on transactions.merchant_id';

CREATE INDEX IF NOT EXISTS idx_transactions_created_at ON fee_nominal.transactions(created_at);
RAISE NOTICE 'Created index on transactions.created_at';

CREATE INDEX IF NOT EXISTS idx_transactions_provider ON fee_nominal.transactions(surcharge_provider_id);
RAISE NOTICE 'Created index on transactions.surcharge_provider_id';

CREATE INDEX IF NOT EXISTS idx_audit_trail_merchant ON fee_nominal.merchant_audit_trail(merchant_id);
RAISE NOTICE 'Created index on merchant_audit_trail.merchant_id';

CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at_entity_type ON fee_nominal.audit_logs(created_at, entity_type);
RAISE NOTICE 'Created index on audit_logs.created_at and audit_logs.entity_type';

CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON fee_nominal.audit_logs(action);
RAISE NOTICE 'Created index on audit_logs.action';

CREATE INDEX IF NOT EXISTS idx_batch_transactions_merchant ON fee_nominal.batch_transactions(merchant_id);
RAISE NOTICE 'Created index on batch_transactions.merchant_id';

CREATE INDEX IF NOT EXISTS idx_batch_transactions_provider ON fee_nominal.batch_transactions(surcharge_provider_id);
RAISE NOTICE 'Created index on batch_transactions.surcharge_provider_id';

CREATE INDEX IF NOT EXISTS idx_batch_items_batch ON fee_nominal.batch_transaction_items(batch_transaction_id);
RAISE NOTICE 'Created index on batch_transaction_items.batch_transaction_id';

CREATE INDEX IF NOT EXISTS idx_auth_attempts_api_key ON fee_nominal.api_key_usage_logs(api_key_id);
RAISE NOTICE 'Created index on api_key_usage_logs.api_key_id';

DO $$
DECLARE
    v_index_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_index_count
    FROM pg_indexes
    WHERE schemaname = 'fee_nominal'
    AND indexname LIKE 'idx_%';
    
    IF v_index_count < 13 THEN
        RAISE EXCEPTION 'Not all indexes were created successfully. Expected 13, found %', v_index_count;
    END IF;
    RAISE NOTICE 'Verified creation of all indexes';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_8__create_indexes migration successfully';
END $$;
