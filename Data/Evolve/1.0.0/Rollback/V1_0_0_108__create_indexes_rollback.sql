/*
Rollback: V1_0_0_108__create_indexes_rollback.sql
Description: Removes all indexes created for performance optimization
Dependencies: None
Changes:
- Removes all indexes from merchant tables
- Removes all indexes from API key tables
- Removes all indexes from transaction tables
- Removes all indexes from audit tables
- Removes all indexes from batch tables
- Removes all indexes from usage logs
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of V1_0_0_108__create_indexes...';
END $$;

-- Drop indexes on merchant table
DROP INDEX IF EXISTS fee_nominal.idx_merchants_merchant_id;
DROP INDEX IF EXISTS fee_nominal.idx_merchants_merchant_code;
DROP INDEX IF EXISTS fee_nominal.idx_merchants_merchant_name;
DROP INDEX IF EXISTS fee_nominal.idx_merchants_merchant_status_id;
DROP INDEX IF EXISTS fee_nominal.idx_merchants_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_merchants_updated_at;
RAISE NOTICE 'Dropped indexes on merchant table';

-- Drop indexes on api_key table
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_api_key_id;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_merchant_id;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_api_key;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_api_key_status_id;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_api_keys_updated_at;
RAISE NOTICE 'Dropped indexes on api_key table';

-- Drop indexes on transaction table
DROP INDEX IF EXISTS fee_nominal.idx_transactions_transaction_id;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_merchant_id;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_api_key_id;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_transaction_status_id;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_transactions_updated_at;
RAISE NOTICE 'Dropped indexes on transaction table';

-- Drop indexes on audit_logs table
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_audit_log_id;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_entity_type;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_entity_id;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_user_id;
DROP INDEX IF EXISTS fee_nominal.idx_audit_logs_created_at;
RAISE NOTICE 'Dropped indexes on audit_logs table';

-- Drop indexes on batch_processing table
DROP INDEX IF EXISTS fee_nominal.idx_batch_processing_batch_id;
DROP INDEX IF EXISTS fee_nominal.idx_batch_processing_merchant_id;
DROP INDEX IF EXISTS fee_nominal.idx_batch_processing_batch_status_id;
DROP INDEX IF EXISTS fee_nominal.idx_batch_processing_created_at;
DROP INDEX IF EXISTS fee_nominal.idx_batch_processing_updated_at;
RAISE NOTICE 'Dropped indexes on batch_processing table';

-- Drop indexes on usage_logs table
DROP INDEX IF EXISTS fee_nominal.idx_usage_logs_usage_log_id;
DROP INDEX IF EXISTS fee_nominal.idx_usage_logs_merchant_id;
DROP INDEX IF EXISTS fee_nominal.idx_usage_logs_api_key_id;
DROP INDEX IF EXISTS fee_nominal.idx_usage_logs_created_at;
RAISE NOTICE 'Dropped indexes on usage_logs table';

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
    RAISE NOTICE 'Completed rollback of V1_0_0_108__create_indexes successfully';
END $$; 