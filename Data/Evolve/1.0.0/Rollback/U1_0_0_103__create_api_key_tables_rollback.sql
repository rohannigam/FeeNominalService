/*
Rollback: U1_0_0_103__create_api_key_tables_rollback.sql
Description: Rolls back the creation of API key-related tables
Dependencies: V1_0_0_3__create_api_key_tables.sql
Changes:
- Drops api_key_audit_logs table
- Drops api_key_secrets table
- Drops api_keys table
- Drops uuid-ossp extension if no other tables are using it
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting U1_0_0_103__create_api_key_tables rollback...';
END $$;

-- Drop tables in reverse order of creation
DROP TABLE IF EXISTS fee_nominal.api_key_audit_logs CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_key_audit_logs table';
END $$;

DROP TABLE IF EXISTS fee_nominal.api_key_secrets CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_key_secrets table';
END $$;

DROP TABLE IF EXISTS fee_nominal.api_keys CASCADE;
DO $$
BEGIN
    RAISE NOTICE 'Dropped api_keys table';
END $$;

-- Drop uuid-ossp extension if no other tables are using it
DO $$
DECLARE
    v_extension_in_use BOOLEAN;
BEGIN
    -- Check if any other tables are using uuid-ossp
    SELECT EXISTS (
        SELECT 1 
        FROM pg_tables t
        JOIN pg_class c ON t.tablename = c.relname
        JOIN pg_attribute a ON a.attrelid = c.oid
        JOIN pg_type typ ON a.atttypid = typ.oid
        WHERE t.schemaname = 'fee_nominal'
        AND typ.typname = 'uuid'
        AND a.attname != 'api_key_id'
    ) INTO v_extension_in_use;

    IF NOT v_extension_in_use THEN
        DROP EXTENSION IF EXISTS "uuid-ossp";
        RAISE NOTICE 'Dropped uuid-ossp extension';
    ELSE
        RAISE NOTICE 'uuid-ossp extension is still in use by other tables';
    END IF;
END $$;

-- Verify tables are dropped
DO $$ 
BEGIN
    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_key_audit_logs') THEN
        RAISE EXCEPTION 'Table api_key_audit_logs was not dropped successfully';
    END IF;
    
    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_key_secrets') THEN
        RAISE EXCEPTION 'Table api_key_secrets was not dropped successfully';
    END IF;
    
    IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = 'fee_nominal' AND tablename = 'api_keys') THEN
        RAISE EXCEPTION 'Table api_keys was not dropped successfully';
    END IF;
    
    RAISE NOTICE 'Verified all tables were dropped successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed U1_0_0_103__create_api_key_tables rollback successfully';
END $$; 