-- M1_0_0_002__grant_api_permissions.sql
-- Create API user and grant API permissions
-- This script creates svc_feenominal_api user and grants runtime access
-- FULLY IDEMPOTENT: Can be run multiple times safely

-- =============================================================================
-- CONFIGURATION
-- =============================================================================

DO $$
BEGIN
    PERFORM set_config('app.api_username', 'svc_feenominal_api', false);
    PERFORM set_config('app.schema_name', 'fee_nominal', false);
    PERFORM set_config('app.db_name', 'feenominal', false);
END$$;

-- =============================================================================
-- CREATE API USER - IDEMPOTENT
-- =============================================================================

DO $$
DECLARE
    api_username TEXT := current_setting('app.api_username');
    api_password TEXT := 'api_default_password';
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = api_username) THEN
        EXECUTE format('CREATE USER %I WITH PASSWORD %L', api_username, api_password);
        RAISE NOTICE 'Created API user: %', api_username;
    ELSE
        -- Update password if user exists (safe to run multiple times)
        EXECUTE format('ALTER USER %I WITH PASSWORD %L', api_username, api_password);
        RAISE NOTICE 'Updated password for existing API user: %', api_username;
    END IF;
END$$;

-- =============================================================================
-- GRANT API PERMISSIONS - IDEMPOTENT
-- =============================================================================

DO $$
DECLARE
    api_username TEXT := current_setting('app.api_username');
    schema_name TEXT := current_setting('app.schema_name');
    db_name TEXT := current_setting('app.db_name');
    table_record RECORD;
    seq_record RECORD;
    func_record RECORD;
    table_count INTEGER := 0;
    seq_count INTEGER := 0;
    func_count INTEGER := 0;
BEGIN
    -- Grant basic connect permissions (safe to run multiple times)
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', db_name, api_username);
    
    -- Grant schema permissions (safe to run multiple times)
    EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', schema_name, api_username);
    
    -- Grant permissions on all existing tables (safe to run multiple times)
    FOR table_record IN 
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = schema_name
    LOOP
        BEGIN
            EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON %I.%I TO %I', 
                          schema_name, table_record.tablename, api_username);
            table_count := table_count + 1;
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE 'Warning: Could not grant permissions on table %.%: %', 
                        schema_name, table_record.tablename, SQLERRM;
        END;
    END LOOP;
    
    -- Grant permissions on all existing sequences (safe to run multiple times)
    FOR seq_record IN 
        SELECT sequence_name 
        FROM information_schema.sequences 
        WHERE sequence_schema = schema_name
    LOOP
        BEGIN
            EXECUTE format('GRANT USAGE, SELECT, UPDATE ON %I.%I TO %I', 
                          schema_name, seq_record.sequence_name, api_username);
            seq_count := seq_count + 1;
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE 'Warning: Could not grant permissions on sequence %.%: %', 
                        schema_name, seq_record.sequence_name, SQLERRM;
        END;
    END LOOP;
    
    -- Grant permissions on all existing functions (safe to run multiple times)
    FOR func_record IN 
        SELECT p.proname, pg_get_function_identity_arguments(p.oid) as args
        FROM pg_proc p
        JOIN pg_namespace n ON p.pronamespace = n.oid
        WHERE n.nspname = schema_name
    LOOP
        BEGIN
            EXECUTE format('GRANT EXECUTE ON FUNCTION %I.%I(%s) TO %I', 
                          schema_name, func_record.proname, func_record.args, api_username);
            func_count := func_count + 1;
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE 'Warning: Could not grant permissions on function %.%(%s): %', 
                        schema_name, func_record.proname, func_record.args, SQLERRM;
        END;
    END LOOP;
    
    -- Set default privileges for future objects created by deployment user (safe to run multiple times)
    EXECUTE format('ALTER DEFAULT PRIVILEGES FOR USER svc_feenominal_deploy IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I', 
                  schema_name, api_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES FOR USER svc_feenominal_deploy IN SCHEMA %I GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO %I', 
                  schema_name, api_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES FOR USER svc_feenominal_deploy IN SCHEMA %I GRANT EXECUTE ON FUNCTIONS TO %I', 
                  schema_name, api_username);
    
    -- Also set default privileges for any user in the schema (safe to run multiple times)
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I', 
                  schema_name, api_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO %I', 
                  schema_name, api_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT EXECUTE ON FUNCTIONS TO %I', 
                  schema_name, api_username);
    
    -- Grant permissions on all objects owned by deployment user (comprehensive approach)
    EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA %I TO %I', schema_name, api_username);
    EXECUTE format('GRANT ALL ON ALL SEQUENCES IN SCHEMA %I TO %I', schema_name, api_username);
    EXECUTE format('GRANT ALL ON ALL FUNCTIONS IN SCHEMA %I TO %I', schema_name, api_username);
    
    RAISE NOTICE 'Created API user and granted API permissions to %', api_username;
    RAISE NOTICE 'User can now access % tables, % sequences, % functions', 
                table_count, seq_count, func_count;
    RAISE NOTICE 'Default privileges set for future objects created by deployment user';
    RAISE NOTICE 'Comprehensive permissions granted on all objects in schema';
    RAISE NOTICE 'Service should now start without permission errors!';
    RAISE NOTICE 'This script is IDEMPOTENT - safe to run multiple times!';
END$$; 