/*
Migration: V1_0_0_6__create_functions.sql
Description: Creates utility functions for audit logging and data management
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_5__create_audit_tables.sql (requires audit_logs and audit_log_details tables)
Changes:
- Creates log_audit_event function for centralized audit logging
- Creates log_audit_detail function for detailed audit logging
- Creates get_audit_logs function for retrieving audit logs
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_6__create_functions migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

CREATE OR REPLACE FUNCTION fee_nominal.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_merchants_updated_at
    BEFORE UPDATE ON fee_nominal.merchants
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_surcharge_providers_updated_at
    BEFORE UPDATE ON fee_nominal.surcharge_providers
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_surcharge_provider_configs_updated_at
    BEFORE UPDATE ON fee_nominal.surcharge_provider_configs
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_api_keys_updated_at
    BEFORE UPDATE ON fee_nominal.api_keys
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

-- Function to log transaction history
CREATE OR REPLACE FUNCTION fee_nominal.log_transaction_history()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO fee_nominal.transaction_history (
        transaction_id,
        status_id,
        surcharge_amount,
        surcharge_currency,
        surcharge_provider_response
    ) VALUES (
        NEW.transaction_id,
        NEW.status_id,
        NEW.surcharge_amount,
        NEW.surcharge_currency,
        NEW.surcharge_provider_response
    );
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create trigger for transaction history
CREATE TRIGGER log_transaction_history
    AFTER INSERT OR UPDATE ON fee_nominal.transactions
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.log_transaction_history();

-- Function to log audit details
CREATE OR REPLACE FUNCTION fee_nominal.log_audit_details()
RETURNS TRIGGER AS $$
DECLARE
    v_audit_log_id UUID;
    v_entity_id UUID;
BEGIN
    -- Get entity_id based on the table
    CASE TG_TABLE_NAME
        WHEN 'merchants' THEN
            v_entity_id := NEW.merchant_id;
        WHEN 'api_keys' THEN
            v_entity_id := NEW.api_key_id;
        WHEN 'transactions' THEN
            v_entity_id := NEW.transaction_id;
        ELSE
            v_entity_id := NULL;
    END CASE;

    -- Insert into audit_logs
    INSERT INTO fee_nominal.audit_logs (
        entity_type,
        entity_id,
        action,
        user_id
    ) VALUES (
        TG_TABLE_NAME,
        v_entity_id,
        TG_OP,
        NULL
    ) RETURNING audit_log_id INTO v_audit_log_id;

    -- Insert into audit_log_details
    WITH old_data AS (
        SELECT key, value FROM jsonb_each_text(row_to_json(OLD)::jsonb)
    ),
    new_data AS (
        SELECT key, value FROM jsonb_each_text(row_to_json(NEW)::jsonb)
    )
    INSERT INTO fee_nominal.audit_log_details (
        audit_log_id,
        field_name,
        old_value,
        new_value
    )
    SELECT
        v_audit_log_id,
        COALESCE(old_data.key, new_data.key),
        old_data.value,
        new_data.value
    FROM old_data
    FULL OUTER JOIN new_data USING (key)
    WHERE old_data.value IS DISTINCT FROM new_data.value;

    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for audit logging
CREATE TRIGGER audit_merchants
    AFTER INSERT OR UPDATE OR DELETE ON fee_nominal.merchants
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.log_audit_details();

CREATE TRIGGER audit_api_keys
    AFTER INSERT OR UPDATE OR DELETE ON fee_nominal.api_keys
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.log_audit_details();

-- Create log_audit_event function
CREATE OR REPLACE FUNCTION fee_nominal.log_audit_event(
    p_entity_type VARCHAR(50),
    p_entity_id UUID,
    p_action VARCHAR(50),
    p_user_id VARCHAR(100) DEFAULT NULL
) RETURNS UUID AS $$
DECLARE
    v_audit_log_id UUID;
BEGIN
    INSERT INTO fee_nominal.audit_logs (
        entity_type,
        entity_id,
        action,
        user_id
    ) VALUES (
        p_entity_type,
        p_entity_id,
        p_action,
        p_user_id
    ) RETURNING audit_log_id INTO v_audit_log_id;
    
    RETURN v_audit_log_id;
END;
$$ LANGUAGE plpgsql;
DO $$
BEGIN
    RAISE NOTICE 'Created log_audit_event function';
END $$;

-- Create log_audit_detail function
CREATE OR REPLACE FUNCTION fee_nominal.log_audit_detail(
    p_audit_log_id UUID,
    p_field_name VARCHAR(100),
    p_old_value TEXT DEFAULT NULL,
    p_new_value TEXT DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    INSERT INTO fee_nominal.audit_log_details (
        audit_log_id,
        field_name,
        old_value,
        new_value
    ) VALUES (
        p_audit_log_id,
        p_field_name,
        p_old_value,
        p_new_value
    );
END;
$$ LANGUAGE plpgsql;
DO $$
BEGIN
    RAISE NOTICE 'Created log_audit_detail function';
END $$;

-- Create get_audit_logs function
CREATE OR REPLACE FUNCTION fee_nominal.get_audit_logs(
    p_entity_type VARCHAR(50),
    p_entity_id UUID,
    p_start_date TIMESTAMP WITH TIME ZONE DEFAULT NULL,
    p_end_date TIMESTAMP WITH TIME ZONE DEFAULT NULL
) RETURNS TABLE (
    audit_log_id UUID,
    action VARCHAR(50),
    user_id VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE,
    field_name VARCHAR(100),
    old_value TEXT,
    new_value TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        al.audit_log_id,
        al.action,
        al.user_id,
        al.created_at,
        ald.field_name,
        ald.old_value,
        ald.new_value
    FROM fee_nominal.audit_logs al
    LEFT JOIN fee_nominal.audit_log_details ald ON al.audit_log_id = ald.audit_log_id
    WHERE al.entity_type = p_entity_type
    AND al.entity_id = p_entity_id
    AND (p_start_date IS NULL OR al.created_at >= p_start_date)
    AND (p_end_date IS NULL OR al.created_at <= p_end_date)
    ORDER BY al.created_at DESC;
END;
$$ LANGUAGE plpgsql;
DO $$
BEGIN
    RAISE NOTICE 'Created get_audit_logs function';
END $$;

-- Verify functions and column types
DO $$ 
BEGIN
    -- Verify functions exist
    IF NOT EXISTS (SELECT 1 FROM pg_proc WHERE proname = 'log_audit_event' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')) THEN
        RAISE EXCEPTION 'Function log_audit_event was not created successfully';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_proc WHERE proname = 'log_audit_detail' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')) THEN
        RAISE EXCEPTION 'Function log_audit_detail was not created successfully';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_proc WHERE proname = 'get_audit_logs' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')) THEN
        RAISE EXCEPTION 'Function get_audit_logs was not created successfully';
    END IF;
    RAISE NOTICE 'Verified all functions were created successfully';

    -- Verify audit_logs table columns
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'audit_logs' 
        AND column_name = 'audit_log_id' 
        AND data_type = 'uuid'
    ) THEN
        RAISE EXCEPTION 'audit_logs.audit_log_id is not UUID type';
    END IF;

    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'audit_logs' 
        AND column_name = 'entity_id' 
        AND data_type = 'uuid'
    ) THEN
        RAISE EXCEPTION 'audit_logs.entity_id is not UUID type';
    END IF;

    -- Verify audit_log_details table columns
    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'audit_log_details' 
        AND column_name = 'detail_id' 
        AND data_type = 'uuid'
    ) THEN
        RAISE EXCEPTION 'audit_log_details.detail_id is not UUID type';
    END IF;

    IF NOT EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'audit_log_details' 
        AND column_name = 'audit_log_id' 
        AND data_type = 'uuid'
    ) THEN
        RAISE EXCEPTION 'audit_log_details.audit_log_id is not UUID type';
    END IF;

    RAISE NOTICE 'Verified all column types successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_6__create_functions migration successfully';
END $$;
