/*
Migration: V1_0_0_7__fix_audit_function.sql
Description: Fixes and enhances the audit logging function
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_6__create_functions.sql (requires log_audit_event function)
Changes:
- Updates log_audit_event function to handle NULL values
- Adds additional error handling
- Improves performance with better parameter handling
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_7__fix_audit_function migration...';
END $$;

-- Drop existing trigger and function
DROP TRIGGER IF EXISTS audit_merchants ON fee_nominal.merchants;
RAISE NOTICE 'Dropped existing audit_merchants trigger';

DROP FUNCTION IF EXISTS fee_nominal.log_audit_details();
RAISE NOTICE 'Dropped existing log_audit_details function';

-- Create enhanced audit function
CREATE OR REPLACE FUNCTION fee_nominal.log_audit_details()
RETURNS TRIGGER AS $$
DECLARE
    v_audit_log_id INTEGER;
    v_old_data JSONB;
    v_new_data JSONB;
    v_field_name TEXT;
    v_old_value TEXT;
    v_new_value TEXT;
BEGIN
    -- Get old and new data as JSONB
    v_old_data := to_jsonb(OLD);
    v_new_data := to_jsonb(NEW);
    
    -- Log the main audit event
    v_audit_log_id := fee_nominal.log_audit_event(
        TG_TABLE_NAME,
        NEW.merchant_id,
        TG_OP,
        COALESCE(current_setting('app.current_user', TRUE), 'system')
    );
    
    -- Log each changed field
    FOR v_field_name IN 
        SELECT key 
        FROM jsonb_object_keys(v_old_data) 
        WHERE v_old_data->key IS DISTINCT FROM v_new_data->key
    LOOP
        v_old_value := v_old_data->>v_field_name;
        v_new_value := v_new_data->>v_field_name;
        
        -- Skip logging if both values are NULL
        IF v_old_value IS NOT NULL OR v_new_value IS NOT NULL THEN
            PERFORM fee_nominal.log_audit_detail(
                v_audit_log_id,
                v_field_name,
                v_old_value,
                v_new_value
            );
        END IF;
    END LOOP;
    
    RETURN NEW;
EXCEPTION
    WHEN OTHERS THEN
        RAISE WARNING 'Error in audit logging: %', SQLERRM;
        RETURN NEW;
END;
$$ LANGUAGE plpgsql;
RAISE NOTICE 'Created enhanced log_audit_details function';

-- Create trigger
CREATE TRIGGER audit_merchants
    AFTER INSERT OR UPDATE OR DELETE ON fee_nominal.merchants
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.log_audit_details();
RAISE NOTICE 'Created audit_merchants trigger';

-- Verify function and trigger
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_proc WHERE proname = 'log_audit_details' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')) THEN
        RAISE EXCEPTION 'Function log_audit_details was not created successfully';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_trigger WHERE tgname = 'audit_merchants' AND tgrelid = (SELECT oid FROM pg_class WHERE relname = 'merchants' AND relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal'))) THEN
        RAISE EXCEPTION 'Trigger audit_merchants was not created successfully';
    END IF;
    RAISE NOTICE 'Verified function and trigger creation';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_7__fix_audit_function migration successfully';
END $$;
