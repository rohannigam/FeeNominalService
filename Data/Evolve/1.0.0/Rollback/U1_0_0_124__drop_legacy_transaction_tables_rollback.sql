/*
Rollback: U1_0_0_124__drop_legacy_transaction_tables_rollback.sql
Description: Recreates legacy transaction tables if rollback is needed
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback for V1_0_0_124__drop_legacy_transaction_tables...';
END $$;

SET search_path TO fee_nominal;

-- Recreate transaction_statuses
CREATE TABLE IF NOT EXISTS transaction_statuses (
    transaction_status_id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$ BEGIN RAISE NOTICE 'Recreated transaction_statuses table'; END $$;

-- Recreate transactions
CREATE TABLE IF NOT EXISTS transactions (
    transaction_id SERIAL PRIMARY KEY,
    merchant_id UUID NOT NULL,
    transaction_status_id INTEGER NOT NULL,
    amount DECIMAL(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    reference_id VARCHAR(255) NOT NULL,
    description TEXT,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$ BEGIN RAISE NOTICE 'Recreated transactions table'; END $$;

-- Recreate transaction_audit_logs
CREATE TABLE IF NOT EXISTS transaction_audit_logs (
    audit_log_id SERIAL PRIMARY KEY,
    transaction_id INTEGER NOT NULL,
    action VARCHAR(50) NOT NULL,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255)
);
DO $$ BEGIN RAISE NOTICE 'Recreated transaction_audit_logs table'; END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback for V1_0_0_124__drop_legacy_transaction_tables.';
END $$; 