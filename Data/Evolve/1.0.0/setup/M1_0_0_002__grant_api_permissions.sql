-- M1_0_0_002__grant_api_permissions.sql
-- Create API user and grant API permissions
-- This script creates svc_feenominal_api user and grants runtime access
-- REQUIRES: api_password parameter to be set from secrets manager
-- USAGE: SET api_password = 'your_secure_password'; \i M1_0_0_002__grant_api_permissions.sql

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
    api_password TEXT; --e.g. 'api_default_password'. Please set it from your secrets manager before running this script.
BEGIN
    -- Get password from parameter - fail if not set or empty
    BEGIN
        api_password := current_setting('api_password');
        IF api_password IS NULL OR api_password = '' THEN
            RAISE EXCEPTION 'SECURITY ERROR: api_password parameter is not set or empty! Please set it from your secrets manager before running this script.';
        END IF;
    EXCEPTION WHEN OTHERS THEN
        RAISE EXCEPTION 'SECURITY ERROR: api_password parameter is not set! Please set it from your secrets manager before running this script. Usage: SET api_password = ''your_secure_password'';';
    END;
    
    -- Check if user exists and create/update with provided password
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
    current_user_is_superuser BOOLEAN;
    current_user_is_schema_owner BOOLEAN;
BEGIN
    -- Check current user permissions
    SELECT rolname = 'postgres' OR rolsuper INTO current_user_is_superuser 
    FROM pg_roles WHERE rolname = current_user;
    
    SELECT EXISTS (
        SELECT 1 FROM pg_namespace n 
        WHERE n.nspname = schema_name 
        AND n.nspowner = (SELECT oid FROM pg_roles WHERE rolname = current_user)
    ) INTO current_user_is_schema_owner;
    
    RAISE NOTICE 'Current user: %, Superuser: %, Schema owner: %', 
                current_user, current_user_is_superuser, current_user_is_schema_owner;
    
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
    
    -- Set default privileges only if we have sufficient permissions
    IF current_user_is_superuser OR current_user_is_schema_owner THEN
        BEGIN
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
            
            RAISE NOTICE 'Default privileges set for future objects created by deployment user';
            RAISE NOTICE 'Default privileges set for any user in schema';
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE 'Warning: Could not set default privileges: %', SQLERRM;
            RAISE NOTICE 'This is normal if current user is not schema owner or superuser';
        END;
    ELSE
        RAISE NOTICE 'Skipping default privileges setup - current user lacks sufficient permissions';
        RAISE NOTICE 'Default privileges will need to be set manually by schema owner or superuser';
    END IF;
    
    -- Grant permissions on all objects owned by deployment user (comprehensive approach)
    BEGIN
        EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA %I TO %I', schema_name, api_username);
        EXECUTE format('GRANT ALL ON ALL SEQUENCES IN SCHEMA %I TO %I', schema_name, api_username);
        EXECUTE format('GRANT ALL ON ALL FUNCTIONS IN SCHEMA %I TO %I', schema_name, api_username);
        RAISE NOTICE 'Comprehensive permissions granted on all objects in schema';
    EXCEPTION WHEN OTHERS THEN
        RAISE NOTICE 'Warning: Could not grant comprehensive permissions: %', SQLERRM;
        RAISE NOTICE 'Individual object permissions were granted above';
    END;
    
    RAISE NOTICE 'Created API user and granted API permissions to %', api_username;
    RAISE NOTICE 'User can now access % tables, % sequences, % functions', 
                table_count, seq_count, func_count;
    RAISE NOTICE 'Service should now start without permission errors!';
    RAISE NOTICE 'This script is IDEMPOTENT - safe to run multiple times!';
END$$; 