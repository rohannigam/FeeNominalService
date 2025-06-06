-- Up Migration
CREATE TABLE IF NOT EXISTS fee_nominal.audit_logs (
    audit_log_id SERIAL PRIMARY KEY,
    merchant_id INTEGER REFERENCES fee_nominal.merchants(merchant_id),
    api_key_id INTEGER REFERENCES fee_nominal.api_keys(api_key_id),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id INTEGER NOT NULL,
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS fee_nominal.audit_log_details (
    detail_id SERIAL PRIMARY KEY,
    audit_log_id INTEGER NOT NULL REFERENCES fee_nominal.audit_logs(audit_log_id),
    field_name VARCHAR(100) NOT NULL,
    old_value TEXT,
    new_value TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS fee_nominal.merchant_audit_trail (
    merchant_audit_trail_id SERIAL PRIMARY KEY,
    merchant_id INTEGER REFERENCES fee_nominal.merchants(merchant_id),
    action VARCHAR(100) NOT NULL,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_audit_logs_merchant_id ON fee_nominal.audit_logs(merchant_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_api_key_id ON fee_nominal.audit_logs(api_key_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity_type_entity_id ON fee_nominal.audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON fee_nominal.audit_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_audit_log_details_audit_log_id ON fee_nominal.audit_log_details(audit_log_id);

/* -- Down Migration
DROP TABLE IF EXISTS fee_nominal.audit_log_details;
DROP TABLE IF EXISTS fee_nominal.audit_logs;
DROP TABLE IF EXISTS fee_nominal.merchant_audit_trail;  */