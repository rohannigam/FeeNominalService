/*
Rollback: U1_0_0_107__fix_audit_function_rollback.sql
Description: Rolls back the audit function fix
Dependencies: None
Changes:
- Restores the original log_audit_details function
- Restores the original audit triggers
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_107__fix_audit_function_rollback.sql which is a rollback of V1_0_0_7__fix_audit_function...';
END $$;

-- Drop the fixed function's triggers
DROP TRIGGER IF EXISTS audit_merchants ON fee_nominal.merchants;
DROP TRIGGER IF EXISTS audit_api_keys ON fee_nominal.api_keys;
DROP TRIGGER IF EXISTS audit_transactions ON fee_nominal.transactions;

-- Drop the fixed function
DROP FUNCTION IF EXISTS fee_nominal.log_audit_details();

-- Restore the original function
CREATE OR REPLACE FUNCTION fee_nominal.log_audit_details()
RETURNS TRIGGER AS $$
DECLARE
    v_audit_log_id INTEGER;
    v_merchant_id INTEGER;
    v_api_key_id INTEGER;
    v_entity_id INTEGER;
BEGIN
    -- Get merchant_id if it exists
    BEGIN
        v_merchant_id := NEW.merchant_id;
    EXCEPTION WHEN undefined_column THEN
        v_merchant_id := NULL;
    END;

    -- Get api_key_id if it exists
    BEGIN
        v_api_key_id := NEW.api_key_id;
    EXCEPTION WHEN undefined_column THEN
        v_api_key_id := NULL;
    END;

    -- Get id if it exists
    BEGIN
        v_entity_id := NEW.id;
    EXCEPTION WHEN undefined_column THEN
        v_entity_id := NULL;
    END;

    -- Insert into audit_logs
    INSERT INTO fee_nominal.audit_logs (
        merchant_id,
        api_key_id,
        action,
        entity_type,
        entity_id,
        old_values,
        new_values
    ) VALUES (
        v_merchant_id,
        v_api_key_id,
        TG_OP,
        TG_TABLE_NAME,
        v_entity_id,
        CASE WHEN TG_OP = 'DELETE' THEN row_to_json(OLD) ELSE NULL END,
        CASE WHEN TG_OP = 'DELETE' THEN NULL ELSE row_to_json(NEW) END
    ) RETURNING audit_log_id INTO v_audit_log_id;

    -- Insert into audit_log_details
    INSERT INTO fee_nominal.audit_log_details (
        audit_log_id,
        field_name,
        old_value,
        new_value
    )
    SELECT
        v_audit_log_id,
        key,
        old_value::text,
        new_value::text
    FROM jsonb_each_text(row_to_json(OLD)::jsonb) old_vals
    FULL OUTER JOIN jsonb_each_text(row_to_json(NEW)::jsonb) new_vals
    USING (key)
    WHERE old_value IS DISTINCT FROM new_value;

    RETURN NEW;
END;
$$ language 'plpgsql';

-- Restore the original triggers
CREATE TRIGGER audit_merchants
    AFTER INSERT OR UPDATE OR DELETE ON fee_nominal.merchants
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.log_audit_details();

CREATE TRIGGER audit_api_keys
    AFTER INSERT OR UPDATE OR DELETE ON fee_nominal.api_keys
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.log_audit_details();

CREATE TRIGGER audit_transactions
    AFTER INSERT OR UPDATE OR DELETE ON fee_nominal.transactions
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.log_audit_details();

-- Verify the function was restored
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 
        FROM pg_proc 
        WHERE proname = 'log_audit_details' 
        AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'Function log_audit_details was not restored successfully';
    END IF;
    RAISE NOTICE 'Verified log_audit_details function restoration';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_107__fix_audit_function_rollback.sql which is a rollback of V1_0_0_7__fix_audit_function successfully';
END $$; 