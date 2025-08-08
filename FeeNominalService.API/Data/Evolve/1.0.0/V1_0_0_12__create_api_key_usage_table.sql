/*
Migration: V1_0_0_12__create_api_key_usage_table.sql
Description: Creates table for tracking API key usage and rate limiting
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_3__create_api_key_tables.sql (requires api_keys table)
Changes:
- Creates api_key_usage table for tracking API key usage and rate limiting
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_12__create_api_key_usage_table migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Add extension if not exists
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS fee_nominal.api_key_usage (
    api_key_usage_id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    api_key_id UUID NOT NULL REFERENCES fee_nominal.api_keys(api_key_id),
    endpoint VARCHAR(255) NOT NULL,
    ip_address VARCHAR(45) NOT NULL,  -- IPv6 addresses can be up to 45 characters
    request_count INTEGER NOT NULL DEFAULT 1,
    window_start TIMESTAMP WITH TIME ZONE NOT NULL,
    window_end TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    status_code INTEGER NOT NULL,
    response_time_ms INTEGER NOT NULL,
    CONSTRAINT unique_usage_window UNIQUE (api_key_id, endpoint, ip_address, window_start, window_end)
);

DO $$
BEGIN
    RAISE NOTICE 'Created api_key_usage table';
END $$;

-- Create indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_api_key_usage_api_key_id ON fee_nominal.api_key_usage(api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_window ON fee_nominal.api_key_usage(window_start, window_end);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_endpoint ON fee_nominal.api_key_usage(endpoint);

DO $$
BEGIN
    RAISE NOTICE 'Created indexes on api_key_usage table';
END $$;

-- Verify api_key_usage table
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_key_usage') THEN
        RAISE EXCEPTION 'Table api_key_usage was not created successfully';
    END IF;
    RAISE NOTICE 'Verified api_key_usage table creation';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_12__create_api_key_usage_table migration successfully';
END $$; 