-- M1_0_0_003__update_deployment_privileges.sql
-- Update deployment user privileges to include ALTER permissions on tables
-- This is needed for database migrations that modify table structures

-- Set configuration variables
DO $$
BEGIN
    -- User Configuration
    PERFORM set_config('app.deploy_username', 'svc_feenominal_deploy', false);
    PERFORM set_config('app.schema_name', 'fee_nominal', false);
END$$;

-- Grant ALL privileges to deployment user on existing tables (includes ALTER)
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    schema_name TEXT := current_setting('app.schema_name');
BEGIN
    -- Grant ALL on all existing tables in the schema (includes ALTER)
    EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA %I TO %I', schema_name, deploy_username);
    
    -- Update default privileges for future tables
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON TABLES TO %I', schema_name, deploy_username);
    
    RAISE NOTICE 'Granted ALL privileges (including ALTER) to deployment user % on all tables in schema %', deploy_username, schema_name;
    RAISE NOTICE 'Updated default privileges for future tables';
END$$;

-- Verify the privileges
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    schema_name TEXT := current_setting('app.schema_name');
    table_count INTEGER;
BEGIN
    -- Count tables where the user has ALL privilege
    SELECT COUNT(*) INTO table_count
    FROM information_schema.table_privileges 
    WHERE table_schema = schema_name 
    AND grantee = deploy_username 
    AND privilege_type = 'ALL';
    
    RAISE NOTICE 'Deployment user % has ALL privileges on % tables in schema %', deploy_username, table_count, schema_name;
    
    IF table_count = 0 THEN
        RAISE WARNING 'No ALL privileges found. Please check if tables exist and user has proper permissions.';
    END IF;
END$$;

DO $$
BEGIN
    RAISE NOTICE '=== Deployment Privileges Update Complete ===';
    RAISE NOTICE 'Deployment user now has ALL privileges (including ALTER) on all tables';
    RAISE NOTICE 'Default privileges updated for future tables';
    RAISE NOTICE '==============================================';
END$$; 