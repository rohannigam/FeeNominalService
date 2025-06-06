-- Up Migration
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

CREATE TRIGGER update_transactions_updated_at
    BEFORE UPDATE ON fee_nominal.transactions
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

CREATE TRIGGER update_batch_transactions_updated_at
    BEFORE UPDATE ON fee_nominal.batch_transactions
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

-- Function to log transaction history
CREATE OR REPLACE FUNCTION log_transaction_history()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO transaction_history (
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
    AFTER INSERT OR UPDATE ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION log_transaction_history();

-- Function to log audit details
CREATE OR REPLACE FUNCTION log_audit_details()
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
    INSERT INTO audit_logs (
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
    INSERT INTO audit_log_details (
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

-- Create triggers for audit logging
CREATE TRIGGER audit_merchants
    AFTER INSERT OR UPDATE OR DELETE ON merchants
    FOR EACH ROW
    EXECUTE FUNCTION log_audit_details();

CREATE TRIGGER audit_api_keys
    AFTER INSERT OR UPDATE OR DELETE ON api_keys
    FOR EACH ROW
    EXECUTE FUNCTION log_audit_details();

CREATE TRIGGER audit_transactions
    AFTER INSERT OR UPDATE OR DELETE ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION log_audit_details();

/* -- Down Migration
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