-- M1_0_0_004__transfer_table_ownership.sql
-- Transfer ownership of all tables in the schema to the deployment user
-- This is needed for database migrations that modify table structures

-- Set configuration variables
DO $$
BEGIN
    -- User Configuration
    PERFORM set_config('app.deploy_username', 'svc_feenominal_deploy', false);
    PERFORM set_config('app.schema_name', 'fee_nominal', false);
END$$;

-- Transfer ownership of all tables to deployment user
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    schema_name TEXT := current_setting('app.schema_name');
    table_record RECORD;
    table_count INTEGER := 0;
BEGIN
    -- Loop through all tables in the schema and transfer ownership
    FOR table_record IN 
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = schema_name
    LOOP
        EXECUTE format('ALTER TABLE %I.%I OWNER TO %I', schema_name, table_record.tablename, deploy_username);
        table_count := table_count + 1;
        RAISE NOTICE 'Transferred ownership of table % to %', table_record.tablename, deploy_username;
    END LOOP;
    
    RAISE NOTICE 'Transferred ownership of % tables to deployment user %', table_count, deploy_username;
END$$;

-- Transfer ownership of all sequences to deployment user
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    schema_name TEXT := current_setting('app.schema_name');
    seq_record RECORD;
    seq_count INTEGER := 0;
BEGIN
    -- Loop through all sequences in the schema and transfer ownership
    FOR seq_record IN 
        SELECT sequence_name 
        FROM information_schema.sequences 
        WHERE sequence_schema = schema_name
    LOOP
        EXECUTE format('ALTER SEQUENCE %I.%I OWNER TO %I', schema_name, seq_record.sequence_name, deploy_username);
        seq_count := seq_count + 1;
        RAISE NOTICE 'Transferred ownership of sequence % to %', seq_record.sequence_name, deploy_username;
    END LOOP;
    
    RAISE NOTICE 'Transferred ownership of % sequences to deployment user %', seq_count, deploy_username;
END$$;

-- Transfer ownership of all functions to deployment user
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    schema_name TEXT := current_setting('app.schema_name');
    func_record RECORD;
    func_count INTEGER := 0;
BEGIN
    -- Loop through all functions in the schema and transfer ownership
    FOR func_record IN 
        SELECT p.proname, pg_get_function_identity_arguments(p.oid) as args
        FROM pg_proc p
        JOIN pg_namespace n ON p.pronamespace = n.oid
        WHERE n.nspname = schema_name
    LOOP
        EXECUTE format('ALTER FUNCTION %I.%I(%s) OWNER TO %I', 
                      schema_name, func_record.proname, func_record.args, deploy_username);
        func_count := func_count + 1;
        RAISE NOTICE 'Transferred ownership of function % to %', func_record.proname, deploy_username;
    END LOOP;
    
    RAISE NOTICE 'Transferred ownership of % functions to deployment user %', func_count, deploy_username;
END$$;

-- Verify ownership transfer
DO $$
DECLARE
    deploy_username TEXT := current_setting('app.deploy_username');
    schema_name TEXT := current_setting('app.schema_name');
    owned_tables INTEGER;
    owned_sequences INTEGER;
    owned_functions INTEGER;
BEGIN
    -- Count tables owned by deployment user
    SELECT COUNT(*) INTO owned_tables
    FROM pg_tables 
    WHERE schemaname = schema_name AND tableowner = deploy_username;
    
    -- Count sequences owned by deployment user
    SELECT COUNT(*) INTO owned_sequences
    FROM information_schema.sequences 
    WHERE sequence_schema = schema_name 
    AND sequence_name IN (
        SELECT sequence_name 
        FROM information_schema.sequences s
        JOIN pg_class c ON s.sequence_name = c.relname
        JOIN pg_roles r ON c.relowner = r.oid
        WHERE r.rolname = deploy_username
    );
    
    -- Count functions owned by deployment user
    SELECT COUNT(*) INTO owned_functions
    FROM pg_proc p
    JOIN pg_namespace n ON p.pronamespace = n.oid
    JOIN pg_roles r ON p.proowner = r.oid
    WHERE n.nspname = schema_name AND r.rolname = deploy_username;
    
    RAISE NOTICE 'Ownership verification:';
    RAISE NOTICE '  Tables owned by %: %', deploy_username, owned_tables;
    RAISE NOTICE '  Sequences owned by %: %', deploy_username, owned_sequences;
    RAISE NOTICE '  Functions owned by %: %', deploy_username, owned_functions;
END$$;

DO $$
BEGIN
    RAISE NOTICE '=== Table Ownership Transfer Complete ===';
    RAISE NOTICE 'All tables, sequences, and functions now owned by deployment user';
    RAISE NOTICE 'Deployment user can now perform ALTER TABLE operations';
    RAISE NOTICE '==============================================';
END$$; 