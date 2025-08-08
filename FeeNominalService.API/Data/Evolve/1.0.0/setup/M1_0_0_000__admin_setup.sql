-- M1_0_0_000__admin_setup.sql
-- POSTGRES ADMIN SETUP: Handle all postgres admin setup only
-- This script sets up postgres as admin and creates schema
-- User creation is handled in separate scripts (M1_0_0_001 and M1_0_0_002)
-- FULLY IDEMPOTENT: Can be run multiple times safely

-- =============================================================================
-- CONFIGURATION
-- =============================================================================

DO $$
BEGIN
    PERFORM set_config('app.db_name', 'feenominal', false);
    PERFORM set_config('app.schema_name', 'fee_nominal', false);
END$$;

-- =============================================================================
-- CREATE SCHEMA (if not exists) - IDEMPOTENT
-- =============================================================================

DO $$
DECLARE
    schema_name TEXT := current_setting('app.schema_name');
BEGIN
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);
    RAISE NOTICE 'Schema % created or already exists', schema_name;
END$$;

-- =============================================================================
-- GIVE POSTGRES FULL ADMIN ACCESS - IDEMPOTENT
-- =============================================================================

DO $$
DECLARE
    schema_name TEXT := current_setting('app.schema_name');
    db_name TEXT := current_setting('app.db_name');
BEGIN
    -- Grant postgres full access to the database (safe to run multiple times)
    EXECUTE format('GRANT ALL PRIVILEGES ON DATABASE %I TO postgres', db_name);
    
    -- Grant postgres full access to the schema (safe to run multiple times)
    EXECUTE format('GRANT ALL PRIVILEGES ON SCHEMA %I TO postgres', schema_name);
    
    -- Grant postgres full access to all existing objects (safe to run multiple times)
    EXECUTE format('GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA %I TO postgres', schema_name);
    EXECUTE format('GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA %I TO postgres', schema_name);
    EXECUTE format('GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA %I TO postgres', schema_name);
    
    -- Set postgres as default owner for future objects (safe to run multiple times)
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON TABLES TO postgres', schema_name);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON SEQUENCES TO postgres', schema_name);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT ALL ON FUNCTIONS TO postgres', schema_name);
    
    -- Grant public schema access for Evolve changelog (safe to run multiple times)
    EXECUTE format('GRANT ALL PRIVILEGES ON SCHEMA public TO postgres');
    EXECUTE format('GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres');
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO postgres');
    
    RAISE NOTICE 'Granted postgres full admin access to database and schema';
END$$;

-- =============================================================================
-- SUMMARY
-- =============================================================================

DO $$
DECLARE
    db_name TEXT := current_setting('app.db_name');
    schema_name TEXT := current_setting('app.schema_name');
BEGIN
    RAISE NOTICE '=== POSTGRES ADMIN SETUP COMPLETE (M1_0_0_000) ===';
    RAISE NOTICE 'Database: %', db_name;
    RAISE NOTICE 'Schema: %', schema_name;
    RAISE NOTICE 'postgres: FULL ADMIN ACCESS (owner of everything)';
    RAISE NOTICE '';
    RAISE NOTICE 'Next steps:';
    RAISE NOTICE '1. Run M1_0_0_001__grant_deploy_permissions.sql to create deployment user';
    RAISE NOTICE '2. Run M1_0_0_002__grant_api_permissions.sql to create API user';
    RAISE NOTICE '';
    RAISE NOTICE 'This script is IDEMPOTENT - safe to run multiple times!';
    RAISE NOTICE '==============================================';
END$$; 