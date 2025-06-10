/*
Migration: V1_0_0_5__create_audit_tables.sql
Description: Creates additional audit-related tables for tracking changes
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchants table)
- V1_0_0_3__create_api_key_tables.sql (requires api_keys table)
- V1_0_0_4__create_transaction_tables.sql (requires transactions table)
Changes:
- Creates audit_logs table for general system audit
- Creates audit_log_details table for detailed audit information
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_5__create_audit_tables migration...';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.audit_logs (
    audit_log_id SERIAL PRIMARY KEY,
    entity_type VARCHAR(50) NOT NULL,
    entity_id INTEGER NOT NULL,
    action VARCHAR(50) NOT NULL,
    user_id VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
RAISE NOTICE 'Created audit_logs table';

-- Verify audit_logs table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'audit_logs') THEN
        RAISE EXCEPTION 'Table audit_logs was not created successfully';
    END IF;
    RAISE NOTICE 'Verified audit_logs table creation';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.audit_log_details (
    detail_id SERIAL PRIMARY KEY,
    audit_log_id INTEGER NOT NULL REFERENCES fee_nominal.audit_logs(audit_log_id),
    field_name VARCHAR(100) NOT NULL,
    old_value TEXT,
    new_value TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
RAISE NOTICE 'Created audit_log_details table';

-- Verify audit_log_details table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'audit_log_details') THEN
        RAISE EXCEPTION 'Table audit_log_details was not created successfully';
    END IF;
    RAISE NOTICE 'Verified audit_log_details table creation';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_5__create_audit_tables migration successfully';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.merchant_audit_trail (
    merchant_audit_trail_id SERIAL PRIMARY KEY,
    merchant_id INTEGER REFERENCES fee_nominal.merchants(merchant_id),
    action VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    property_name VARCHAR(100),
    old_value TEXT,
    new_value TEXT,
    updated_by VARCHAR(50) NOT NULL DEFAULT 'SYSTEM',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_audit_logs_merchant_id ON fee_nominal.audit_logs(merchant_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_api_key_id ON fee_nominal.audit_logs(api_key_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity_type_entity_id ON fee_nominal.audit_logs(entity_type, entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_logs_created_at ON fee_nominal.audit_logs(created_at);
CREATE INDEX IF NOT EXISTS idx_audit_log_details_audit_log_id ON fee_nominal.audit_log_details(audit_log_id);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_merchant_id ON fee_nominal.merchant_audit_trail(merchant_id);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_created_at ON fee_nominal.merchant_audit_trail(created_at);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_action ON fee_nominal.merchant_audit_trail(action);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_entity_type ON fee_nominal.merchant_audit_trail(entity_type);
CREATE INDEX IF NOT EXISTS idx_merchant_audit_trail_property_name ON fee_nominal.merchant_audit_trail(property_name);
