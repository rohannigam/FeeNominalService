/*
Migration: V1_0_0_1__create_schema.sql
Description: Creates the initial fee_nominal schema and utility function for updating timestamps
Dependencies: None (This is the first migration)
Changes:
- Creates fee_nominal schema
- Creates update_updated_at_column() function used by subsequent migrations for automatic timestamp updates
- Sets up default privileges for API user
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_1__create_schema migration...';
END $$;

-- Drop existing schema and its dependencies
DROP SCHEMA IF EXISTS fee_nominal CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped existing fee_nominal schema if it existed';
END $$;

-- Create schema
CREATE SCHEMA IF NOT EXISTS fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Created fee_nominal schema';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Create function for updating updated_at column
CREATE OR REPLACE FUNCTION fee_nominal.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';
DO $$
BEGIN
    RAISE NOTICE 'Created update_updated_at_column function';
END $$;

-- Set default privileges for ALL users in the schema to grant permissions to API user
-- This ensures that ANY object created by ANY user will automatically grant permissions to svc_feenominal_api
ALTER DEFAULT PRIVILEGES IN SCHEMA fee_nominal GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO svc_feenominal_api;
ALTER DEFAULT PRIVILEGES IN SCHEMA fee_nominal GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO svc_feenominal_api;
ALTER DEFAULT PRIVILEGES IN SCHEMA fee_nominal GRANT EXECUTE ON FUNCTIONS TO svc_feenominal_api;

-- Also set default privileges specifically for the deployment user
ALTER DEFAULT PRIVILEGES FOR USER svc_feenominal_deploy IN SCHEMA fee_nominal GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO svc_feenominal_api;
ALTER DEFAULT PRIVILEGES FOR USER svc_feenominal_deploy IN SCHEMA fee_nominal GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO svc_feenominal_api;
ALTER DEFAULT PRIVILEGES FOR USER svc_feenominal_deploy IN SCHEMA fee_nominal GRANT EXECUTE ON FUNCTIONS TO svc_feenominal_api;

-- Grant schema usage to API user
GRANT USAGE ON SCHEMA fee_nominal TO svc_feenominal_api;

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_1__create_schema migration successfully';
END $$;
