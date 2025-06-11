/*
Rollback: U1_0_0_108__create_indexes_rollback.sql
Description: Removes all indexes created for performance optimization
Dependencies: None
Changes:
- Removes all indexes from merchant tables
- Removes all indexes from surcharge provider tables
- Removes all indexes from API key tables
- Removes all indexes from transaction tables
- Removes all indexes from audit tables
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_108__create_indexes_rollback.sql which is a rollback of V1_0_0_8__create_indexes...';
END $$;

-- Drop indexes on merchant table
DROP INDEX IF EXISTS fee_nominal.idx_merchants_status;
DROP INDEX IF EXISTS fee_nominal.idx_merchants_created_at;
DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes on merchant table';
END $$;

-- Drop indexes on surcharge provider tables
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_providers_code;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_providers_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_provider_configs_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_provider_configs_provider;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_configs_is_active;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_config_history_config_id;
DROP INDEX IF EXISTS fee_nominal.idx_surcharge_provider_config_history_created_at;
DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes on surcharge provider tables';
END $$;

-- Drop indexes on api_key tables
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_is_active;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_is_active;
DROP INDEX IF EXISTS fee_nominal.idx_api_key_secrets_created_at;
DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes on api_key tables';
END $$;

-- Drop indexes on transaction table
DROP INDEX IF EXISTS fee_nominal.idx_transactions_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_created_at;
DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes on transaction table';
END $$;

-- Drop indexes on audit tables
DROP INDEX IF EXISTS fee_nominal.idx_audit_trail_merchant;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_created_at_entity_type;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_action;
DO $$
BEGIN
    RAISE NOTICE 'Dropped indexes on audit tables';
END $$;

-- Verify rollback
DO $$ 
DECLARE
    v_index_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO v_index_count
    FROM pg_indexes
    WHERE schemaname = 'fee_nominal'
    AND indexname LIKE 'idx_%';
    
    IF v_index_count > 0 THEN
        RAISE EXCEPTION 'Not all indexes were removed. Found % remaining', v_index_count;
    END IF;
    
    RAISE NOTICE 'Verified all indexes were removed successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_108__create_indexes_rollback.sql which is a rollback of V1_0_0_8__create_indexes successfully';
END $$; 