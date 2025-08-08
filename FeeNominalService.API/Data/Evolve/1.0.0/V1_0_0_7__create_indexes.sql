/*
Migration: V1_0_0_7__create_indexes.sql
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
    RAISE NOTICE 'Starting V1_0_0_7__create_indexes migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

CREATE INDEX IF NOT EXISTS idx_merchants_status ON fee_nominal.merchants(status_id);
DO $$
BEGIN
    RAISE NOTICE 'Created index on merchants.status_id';
END $$;

CREATE INDEX IF NOT EXISTS idx_merchants_created_at ON fee_nominal.merchants(created_at);
DO $$
BEGIN
    RAISE NOTICE 'Created index on merchants.created_at';
END $$;

CREATE INDEX IF NOT EXISTS idx_surcharge_providers_code ON fee_nominal.surcharge_providers(code);
DO $$
BEGIN
    RAISE NOTICE 'Created index on surcharge_providers.code';
END $$;

CREATE INDEX IF NOT EXISTS idx_surcharge_providers_created_at ON fee_nominal.surcharge_providers(created_at);
DO $$
BEGIN
    RAISE NOTICE 'Created index on surcharge_providers.created_at';
END $$;

CREATE INDEX IF NOT EXISTS idx_provider_configs_merchant ON fee_nominal.surcharge_provider_configs(merchant_id);
DO $$
BEGIN
    RAISE NOTICE 'Created index on surcharge_provider_configs.merchant_id';
END $$;

CREATE INDEX IF NOT EXISTS idx_provider_configs_provider ON fee_nominal.surcharge_provider_configs(surcharge_provider_id);
DO $$
BEGIN
    RAISE NOTICE 'Created index on surcharge_provider_configs.surcharge_provider_id';
END $$;

CREATE INDEX IF NOT EXISTS idx_surcharge_provider_configs_is_active ON fee_nominal.surcharge_provider_configs(is_active);
DO $$
BEGIN
    RAISE NOTICE 'Created index on surcharge_provider_configs.is_active';
END $$;

CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_config_id ON fee_nominal.surcharge_provider_config_history(surcharge_provider_config_id);
DO $$
BEGIN
    RAISE NOTICE 'Created index on surcharge_provider_config_history.surcharge_provider_config_id';
END $$;

CREATE INDEX IF NOT EXISTS idx_surcharge_provider_config_history_changed_at ON fee_nominal.surcharge_provider_config_history(changed_at);
DO $$
BEGIN
    RAISE NOTICE 'Created index on surcharge_provider_config_history.changed_at';
END $$;

CREATE INDEX IF NOT EXISTS idx_api_keys_merchant ON fee_nominal.api_keys(merchant_id);
DO $$
BEGIN
    RAISE NOTICE 'Created index on api_keys.merchant_id';
END $$;

CREATE INDEX IF NOT EXISTS idx_api_keys_created_at ON fee_nominal.api_keys(created_at);
DO $$
BEGIN
    RAISE NOTICE 'Created index on api_keys.created_at';
END $$;

CREATE INDEX IF NOT EXISTS idx_api_keys_is_active ON fee_nominal.api_keys(is_active);
DO $$
BEGIN
    RAISE NOTICE 'Created index on api_keys.is_active';
END $$;

CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at_entity_type ON fee_nominal.audit_logs(created_at, entity_type);
DO $$
BEGIN
    RAISE NOTICE 'Created index on audit_logs.created_at and audit_logs.entity_type';
END $$;

CREATE INDEX IF NOT EXISTS idx_audit_logs_action ON fee_nominal.audit_logs(action);
DO $$
BEGIN
    RAISE NOTICE 'Created index on audit_logs.action';
END $$;

DO $$
DECLARE
    v_index_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_index_count
    FROM pg_indexes
    WHERE schemaname = 'fee_nominal'
    AND indexname LIKE 'idx_%';
    
    IF v_index_count < 9 THEN
        RAISE EXCEPTION 'Not all indexes were created successfully. Expected 9, found %', v_index_count;
    END IF;
    RAISE NOTICE 'Verified creation of all indexes';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_7__create_indexes migration successfully';
END $$;
