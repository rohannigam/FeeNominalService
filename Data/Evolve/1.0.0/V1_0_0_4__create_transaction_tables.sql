/*
Migration: V1_0_0_4__create_transaction_tables.sql
Description: Creates tables for managing transactions and their statuses
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_2__create_merchant_tables.sql (requires merchants table)
Changes:
- Creates transaction_statuses table with initial status data
- Creates transactions table
- Creates transaction_audit_logs table
- Inserts default transaction statuses
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_4__create_transaction_tables migration...';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.transaction_statuses (
    transaction_status_id SERIAL PRIMARY KEY,
    code VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$
BEGIN
    RAISE NOTICE 'Created transaction_statuses table';
END $$;

-- Verify transaction_statuses table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'transaction_statuses') THEN
        RAISE EXCEPTION 'Table transaction_statuses was not created successfully';
    END IF;
    RAISE NOTICE 'Verified transaction_statuses table creation';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.transactions (
    transaction_id SERIAL PRIMARY KEY,
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    transaction_status_id INTEGER NOT NULL REFERENCES fee_nominal.transaction_statuses(transaction_status_id),
    amount DECIMAL(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    reference_id VARCHAR(255) NOT NULL,
    description TEXT,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);
DO $$
BEGIN
    RAISE NOTICE 'Created transactions table';
END $$;

-- Verify transactions table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'transactions') THEN
        RAISE EXCEPTION 'Table transactions was not created successfully';
    END IF;
    RAISE NOTICE 'Verified transactions table creation';
END $$;

CREATE TABLE IF NOT EXISTS fee_nominal.transaction_audit_logs (
    audit_log_id SERIAL PRIMARY KEY,
    transaction_id INTEGER NOT NULL REFERENCES fee_nominal.transactions(transaction_id),
    action VARCHAR(50) NOT NULL,
    details JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(255)
);
DO $$
BEGIN
    RAISE NOTICE 'Created transaction_audit_logs table';
END $$;

-- Verify transaction_audit_logs table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'transaction_audit_logs') THEN
        RAISE EXCEPTION 'Table transaction_audit_logs was not created successfully';
    END IF;
    RAISE NOTICE 'Verified transaction_audit_logs table creation';
END $$;

-- Insert default transaction statuses
INSERT INTO fee_nominal.transaction_statuses (code, name, description) VALUES
    ('PENDING', 'Pending', 'Transaction is pending processing'),
    ('PROCESSING', 'Processing', 'Transaction is being processed'),
    ('COMPLETED', 'Completed', 'Transaction has been completed successfully'),
    ('FAILED', 'Failed', 'Transaction has failed'),
    ('CANCELLED', 'Cancelled', 'Transaction has been cancelled'),
    ('REFUNDED', 'Refunded', 'Transaction has been refunded')
ON CONFLICT (code) DO NOTHING;
DO $$
BEGIN
    RAISE NOTICE 'Inserted default transaction statuses';
END $$;

-- Verify data insertion
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM fee_nominal.transaction_statuses WHERE code = 'PENDING') THEN
        RAISE EXCEPTION 'Default transaction statuses were not inserted successfully';
    END IF;
    RAISE NOTICE 'Verified default transaction statuses insertion';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_4__create_transaction_tables migration successfully';
END $$;
