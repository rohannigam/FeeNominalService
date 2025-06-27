-- M1_0_0_002__initial_setup.sql
-- Initial database setup for FeeNominal service
-- This script creates the database, roles, schema, and users with appropriate privileges
-- Attempts to create database using dblink if available, otherwise provides manual instructions
-- Requires M1_0_0_001__enable_dblink.sql to be run first

-- =============================================================================
-- CONFIGURATION VARIABLES - MODIFY THESE FOR DIFFERENT ENVIRONMENTS
-- =============================================================================

-- Set configuration variables using PostgreSQL variables
DO $$
BEGIN
    -- Database and Schema Configuration
    PERFORM set_config('app.db_name', 'feenominal', false);
    PERFORM set_config('app.schema_name', 'fee_nominal', false);
    PERFORM set_config('app.environment', 'dev', false);
    
    -- User Configuration
    PERFORM set_config('app.deploy_username', 'svc_feenominal_deploy', false);
    PERFORM set_config('app.api_username', 'svc_feenominal_api', false);
    
    -- Password Configuration (in production, these should come from environment variables or secrets)
    PERFORM set_config('app.deploy_password', 'deploy_default_password', false);
    PERFORM set_config('app.api_password', 'api_default_password', false);
    
    -- Role Configuration
    PERFORM set_config('app.readonly_role', 'feenominal_readonly', false);
    PERFORM set_config('app.readwrite_role', 'feenominal_readwrite', false);
END$$;

-- =============================================================================
-- SCRIPT EXECUTION - DO NOT MODIFY BELOW THIS LINE
-- =============================================================================

-- 1. Attempt to create the database using dblink if available
DO $$
DECLARE
    db_exists BOOLEAN;
    dblink_available BOOLEAN;
    db_name TEXT := current_setting('app.db_name');
BEGIN
    -- Check if dblink extension is available
    SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'dblink') INTO dblink_available;
    
    -- Check if target database already exists
    SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = db_name) INTO db_exists;
    
    IF dblink_available THEN
        IF NOT db_exists THEN
            -- Try to create database using dblink
            BEGIN
                PERFORM dblink_exec('dbname=postgres', format('CREATE DATABASE %I OWNER postgres ENCODING ''UTF8'' CONNECTION LIMIT -1', db_name));
                RAISE NOTICE 'Database % created successfully using dblink', db_name;
            EXCEPTION WHEN OTHERS THEN
                RAISE WARNING 'Failed to create database using dblink: %', SQLERRM;
                RAISE NOTICE 'Please create database manually: CREATE DATABASE %I OWNER postgres ENCODING ''UTF8'' CONNECTION LIMIT -1;', db_name;
            END;
        ELSE
            RAISE NOTICE 'Database % already exists', db_name;
        END IF;
    ELSE
        -- dblink not available, provide manual instructions
        IF NOT db_exists THEN
            RAISE WARNING 'dblink extension not available. Cannot create database automatically.';
            RAISE NOTICE 'Please create database manually before running this script:';
            RAISE NOTICE '  CREATE DATABASE %I OWNER postgres ENCODING ''UTF8'' CONNECTION LIMIT -1;', db_name;
            RAISE NOTICE 'Or install dblink extension: CREATE EXTENSION IF NOT EXISTS dblink;';
        ELSE
            RAISE NOTICE 'Database % already exists', db_name;
        END IF;
    END IF;
END$$;

-- 2. Check if we're connected to the target database
DO $$
DECLARE
    db_name TEXT := current_setting('app.db_name');
BEGIN
   IF current_database() = db_name THEN
      RAISE NOTICE 'Connected to target database %. Proceeding with setup...', db_name;
   ELSE
      RAISE NOTICE 'WARNING: Not connected to target database %', db_name;
      RAISE NOTICE 'Current database: %', current_database();
      RAISE NOTICE 'Please ensure database % exists and you are connected to it', db_name;
      RAISE NOTICE 'Manual command: CREATE DATABASE %I OWNER postgres ENCODING ''UTF8'' CONNECTION LIMIT -1;', db_name;
   END IF;
   
   -- Check if target database exists
   IF EXISTS (SELECT 1 FROM pg_database WHERE datname = db_name) THEN
      RAISE NOTICE 'Database % exists and is accessible', db_name;
   ELSE
      RAISE WARNING 'Database % does not exist. Please create it before running this script.', db_name;
   END IF;
END$$;

-- 3. Create readonly and readwrite roles if they do not exist
DO $$
DECLARE
    readonly_role TEXT := current_setting('app.readonly_role');
    readwrite_role TEXT := current_setting('app.readwrite_role');
BEGIN
   IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = readonly_role) THEN
      EXECUTE format('CREATE ROLE %I', readonly_role);
      RAISE NOTICE 'Role % created successfully', readonly_role;
   ELSE
      RAISE NOTICE 'Role % already exists', readonly_role;
   END IF;
   
   IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = readwrite_role) THEN
      EXECUTE format('CREATE ROLE %I', readwrite_role);
      RAISE NOTICE 'Role % created successfully', readwrite_role;
   ELSE
      RAISE NOTICE 'Role % already exists', readwrite_role;
   END IF;
END$$;

-- Grant connect on database to roles
DO $$
DECLARE
    db_name TEXT := current_setting('app.db_name');
    readonly_role TEXT := current_setting('app.readonly_role');
    readwrite_role TEXT := current_setting('app.readwrite_role');
BEGIN
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO %I, %I', db_name, readonly_role, readwrite_role);
END$$;

-- 4. Create schema if it does not exist
DO $$
DECLARE
    schema_name TEXT := current_setting('app.schema_name');
BEGIN
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);
    RAISE NOTICE 'Schema % created or already exists', schema_name;
END$$;

-- 4.5. Transfer schema ownership to deployment user for Evolve compatibility
DO $$
DECLARE
    schema_name TEXT := current_setting('app.schema_name');
    deploy_username TEXT := current_setting('app.deploy_username');
BEGIN
    -- Transfer ownership of the schema to the deployment user
    EXECUTE format('ALTER SCHEMA %I OWNER TO %I', schema_name, deploy_username);
    RAISE NOTICE 'Schema % ownership transferred to deployment user %', schema_name, deploy_username;
END$$;

-- 4.6. Grant CREATE privilege on database to deployment user for schema operations
DO $$
DECLARE
    db_name TEXT := current_setting('app.db_name');
    deploy_username TEXT := current_setting('app.deploy_username');
BEGIN
    -- Grant CREATE privilege on the database to allow schema creation
    EXECUTE format('GRANT CREATE ON DATABASE %I TO %I', db_name, deploy_username);
    RAISE NOTICE 'CREATE privilege on database % granted to deployment user %', db_name, deploy_username;
END$$;

-- 5. Create deployment user if it does not exist
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    deploy_password TEXT := current_setting('app.deploy_password');
BEGIN
   IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = deploy_username) THEN
      EXECUTE format('CREATE USER %I WITH PASSWORD %L', deploy_username, deploy_password);
      RAISE NOTICE 'Deployment user % created successfully', deploy_username;
   ELSE
      RAISE NOTICE 'Deployment user % already exists', deploy_username;
   END IF;
END$$;

-- 6. Grant privileges to deployment user
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    readwrite_role TEXT := current_setting('app.readwrite_role');
    schema_name TEXT := current_setting('app.schema_name');
BEGIN
    EXECUTE format('GRANT %I TO %I', readwrite_role, deploy_username);
    EXECUTE format('GRANT USAGE, CREATE ON SCHEMA %I TO %I', schema_name, deploy_username);
    EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA %I TO %I', schema_name, deploy_username);
    EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA %I TO %I', schema_name, deploy_username);
    EXECUTE format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA %I TO %I', schema_name, deploy_username);

    -- Set default privileges for future objects created by deployment user
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON TABLES TO %I', schema_name, deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO %I', schema_name, deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT EXECUTE ON FUNCTIONS TO %I', schema_name, deploy_username);

    -- Set default ownership for future objects created in this schema
    EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA %I GRANT ALL ON TABLES TO %I', schema_name, deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA %I GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO %I', schema_name, deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES FOR ROLE postgres IN SCHEMA %I GRANT EXECUTE ON FUNCTIONS TO %I', schema_name, deploy_username);

    -- 6.5. Grant public schema permissions for Evolve changelog table (fallback)
    EXECUTE format('GRANT USAGE, CREATE ON SCHEMA public TO %I', deploy_username);
    EXECUTE format('GRANT ALL ON ALL TABLES IN SCHEMA public TO %I', deploy_username);
    EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA public TO %I', deploy_username);
    
    -- Set default privileges for future objects in public schema
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO %I', deploy_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO %I', deploy_username);
    
    RAISE NOTICE 'All privileges granted to deployment user %', deploy_username;
END$$;

-- 7. Create API user if it does not exist
DO $$
DECLARE
    api_username TEXT := current_setting('app.api_username');
    api_password TEXT := current_setting('app.api_password');
BEGIN
   IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = api_username) THEN
      EXECUTE format('CREATE USER %I WITH PASSWORD %L', api_username, api_password);
      RAISE NOTICE 'API user % created successfully', api_username;
   ELSE
      RAISE NOTICE 'API user % already exists', api_username;
   END IF;
END$$;

-- 8. Grant privileges to API user (readwrite, but no CREATE/DROP)
DO $$
DECLARE
    api_username TEXT := current_setting('app.api_username');
    readwrite_role TEXT := current_setting('app.readwrite_role');
    schema_name TEXT := current_setting('app.schema_name');
BEGIN
    EXECUTE format('GRANT %I TO %I', readwrite_role, api_username);
    EXECUTE format('GRANT USAGE ON SCHEMA %I TO %I', schema_name, api_username);
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO %I', schema_name, api_username);
    EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA %I TO %I', schema_name, api_username);
    EXECUTE format('GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA %I TO %I', schema_name, api_username);

    -- Set default privileges for future objects created by API user
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I', schema_name, api_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO %I', schema_name, api_username);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT EXECUTE ON FUNCTIONS TO %I', schema_name, api_username);

    RAISE NOTICE 'All privileges granted to API user %', api_username;
END$$;

-- 8.5. Grant privileges on all existing tables to API user
DO $$
DECLARE
    api_username TEXT := current_setting('app.api_username');
    schema_name TEXT := current_setting('app.schema_name');
    table_record RECORD;
BEGIN
    FOR table_record IN 
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = schema_name
    LOOP
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON %I.%I TO %I', schema_name, table_record.tablename, api_username);
    END LOOP;
    RAISE NOTICE 'Granted privileges on all tables in schema % to API user %', schema_name, api_username;
END$$;

-- 9. Grant readonly role to both users for possible future use
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    api_username TEXT := current_setting('app.api_username');
    readonly_role TEXT := current_setting('app.readonly_role');
BEGIN
    EXECUTE format('GRANT %I TO %I, %I', readonly_role, deploy_username, api_username);
END$$;

-- 10. Final summary
DO $$
DECLARE
    environment TEXT := current_setting('app.environment');
    db_name TEXT := current_setting('app.db_name');
    schema_name TEXT := current_setting('app.schema_name');
    readonly_role TEXT := current_setting('app.readonly_role');
    readwrite_role TEXT := current_setting('app.readwrite_role');
    deploy_username TEXT := current_setting('app.deploy_username');
    api_username TEXT := current_setting('app.api_username');
BEGIN
   RAISE NOTICE '=== FeeNominal Database Setup Complete ===';
   RAISE NOTICE 'Environment: %', environment;
   RAISE NOTICE 'Database: %', db_name;
   RAISE NOTICE 'Schema: %', schema_name;
   RAISE NOTICE 'Roles: %, %', readonly_role, readwrite_role;
   RAISE NOTICE 'Users: %, %', deploy_username, api_username;
   RAISE NOTICE 'All privileges and default privileges have been configured';
   RAISE NOTICE '==============================================';
END$$; 