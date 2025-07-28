-- M1_0_0_001__grant_deploy_permissions.sql
-- Create deployment user and grant deployment permissions
-- This script creates svc_feenominal_deploy user and grants migration capabilities
-- REQUIRES: deploy_password parameter to be set from secrets manager
-- USAGE: SET deploy_password = 'your_secure_password'; \i M1_0_0_001__grant_deploy_permissions.sql

-- =============================================================================
-- CONFIGURATION
-- =============================================================================

DO $$
BEGIN
    PERFORM set_config('app.deploy_username', 'svc_feenominal_deploy', false);
    PERFORM set_config('app.schema_name', 'fee_nominal', false);
    PERFORM set_config('app.db_name', 'feenominal', false);
END$$;

-- =============================================================================
-- CREATE DEPLOYMENT USER - IDEMPOTENT
-- =============================================================================

DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    deploy_password TEXT; --e.g. 'deploy_default_password'. Please set it from your secrets manager before running this script.
BEGIN
    -- Get password from parameter - fail if not set or empty
    BEGIN
        deploy_password := current_setting('deploy_password');
        IF deploy_password IS NULL OR deploy_password = '' THEN
            RAISE EXCEPTION 'SECURITY ERROR: deploy_password parameter is not set or empty! Please set it from your secrets manager before running this script.';
        END IF;
    EXCEPTION WHEN OTHERS THEN
        RAISE EXCEPTION 'SECURITY ERROR: deploy_password parameter is not set! Please set it from your secrets manager before running this script. Usage: SET deploy_password = ''your_secure_password'';';
    END;
    
    -- Check if user exists and create/update with provided password
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = deploy_username) THEN
        EXECUTE format('CREATE USER %I WITH PASSWORD %L', deploy_username, deploy_password);
        RAISE NOTICE 'Created deployment user: %', deploy_username;
    ELSE
        -- Update password if user exists (safe to run multiple times)
        EXECUTE format('ALTER USER %I WITH PASSWORD %L', deploy_username, deploy_password);
        RAISE NOTICE 'Updated password for existing deployment user: %', deploy_username;
    END IF;
END$$;

-- =============================================================================
-- GRANT DEPLOYMENT PERMISSIONS - IDEMPOTENT
-- =============================================================================

DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    schema_name TEXT := current_setting('app.schema_name');
    db_name TEXT := current_setting('app.db_name');
BEGIN
    -- Grant basic connect permissions (safe to run multiple times)
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I', db_name, deploy_username);
    
    -- Grant database-level permissions (safe to run multiple times)
    EXECUTE format('GRANT CREATE ON DATABASE %I TO %I', db_name, deploy_username);
    
    -- Grant schema ownership to deployment user (required for Evolve)
    EXECUTE format('ALTER SCHEMA %I OWNER TO %I', schema_name, deploy_username);
    
    -- Grant schema permissions (safe to run multiple times)
    EXECUTE format('GRANT USAGE, CREATE ON SCHEMA %I TO %I', schema_name, deploy_username);
    
    -- Grant permissions on all existing objects (safe to run multiple times)
    EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA %I TO %I', schema_name, deploy_username);
    EXECUTE format('GRANT ALL ON ALL SEQUENCES IN SCHEMA %I TO %I', schema_name, deploy_username);
    EXECUTE format('GRANT ALL ON ALL FUNCTIONS IN SCHEMA %I TO %I', schema_name, deploy_username);
    
    -- Set default privileges for future objects (safe to run multiple times)
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON TABLES TO %I', schema_name, deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON SEQUENCES TO %I', schema_name, deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON FUNCTIONS TO %I', schema_name, deploy_username);
    
    -- Grant public schema permissions for Evolve changelog (safe to run multiple times)
    EXECUTE format('GRANT USAGE, CREATE ON SCHEMA public TO %I', deploy_username);
    EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA public TO %I', deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO %I', deploy_username);
    
    RAISE NOTICE 'Created deployment user and granted deployment permissions to %', deploy_username;
    RAISE NOTICE 'User is now owner of schema % and can run Evolve migrations', schema_name;
    RAISE NOTICE 'This script is IDEMPOTENT - safe to run multiple times!';
END$$; 