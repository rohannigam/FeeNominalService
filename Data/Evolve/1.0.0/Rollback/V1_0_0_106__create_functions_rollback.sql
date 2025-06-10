/*
Rollback: V1_0_0_106__create_functions_rollback.sql
Description: Drops all functions created for audit logging and retrieval
Dependencies: None
Changes:
- Drops log_audit_event function
- Drops log_audit_detail function
- Drops get_audit_logs function
- Drops additional triggers and functions for updated_at tracking
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of V1_0_0_106__create_functions...';
END $$;

-- Drop triggers first
DROP TRIGGER IF EXISTS audit_transactions ON transactions;
DROP TRIGGER IF EXISTS audit_api_keys ON api_keys;
DROP TRIGGER IF EXISTS audit_merchants ON merchants;
DROP TRIGGER IF EXISTS log_transaction_history ON transactions;
DROP TRIGGER IF EXISTS update_batch_transactions_updated_at ON fee_nominal.batch_transactions;
DROP TRIGGER IF EXISTS update_transactions_updated_at ON fee_nominal.transactions;
DROP TRIGGER IF EXISTS update_transaction_statuses_updated_at ON transaction_statuses;
DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON api_key_secrets;
DROP TRIGGER IF EXISTS update_api_keys_updated_at ON fee_nominal.api_keys;
DROP TRIGGER IF EXISTS update_surcharge_provider_configs_updated_at ON fee_nominal.surcharge_provider_configs;
DROP TRIGGER IF EXISTS update_surcharge_providers_updated_at ON fee_nominal.surcharge_providers;
DROP TRIGGER IF EXISTS update_merchants_updated_at ON fee_nominal.merchants;
DROP TRIGGER IF EXISTS update_merchant_statuses_updated_at ON merchant_statuses;
RAISE NOTICE 'Dropped all triggers';

-- Drop functions
DROP FUNCTION IF EXISTS fee_nominal.log_audit_event(TEXT, INTEGER, TEXT, TEXT);
DROP FUNCTION IF EXISTS fee_nominal.log_audit_detail(INTEGER, TEXT, TEXT, TEXT);
DROP FUNCTION IF EXISTS fee_nominal.get_audit_logs(INTEGER);
DROP FUNCTION IF EXISTS fee_nominal.update_updated_at_column();
DROP FUNCTION IF EXISTS log_transaction_history();
DROP FUNCTION IF EXISTS log_audit_details();
RAISE NOTICE 'Dropped all functions';

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'log_audit_event' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'log_audit_event function was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'log_audit_detail' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'log_audit_detail function was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'get_audit_logs' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'get_audit_logs function was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'update_updated_at_column' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'update_updated_at_column function was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'log_transaction_history' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'log_transaction_history function was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'log_audit_details' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'log_audit_details function was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all functions were dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback of V1_0_0_106__create_functions successfully';
END $$; 