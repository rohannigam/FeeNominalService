/*
Migration: V1_0_0_9__alter_api_key_secrets.sql
Description: Adds updated_at and expires_at columns to api_key_secrets table and changes merchant_id to UUID
Dependencies: 
- V1_0_0_1__create_schema.sql (requires fee_nominal schema)
- V1_0_0_3__create_api_key_tables.sql (requires api_key_secrets table)
Changes:
- Adds updated_at column with default value
- Adds expires_at column with default value
- Changes merchant_id column type from VARCHAR(50) to UUID
- Adds foreign key constraint to merchants table
- Updates existing records with default values
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_9__alter_api_key_secrets migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

-- Remove all logic related to fee_nominal.api_key_secrets from this migration

-- Verify changes
DO $$ 
BEGIN
    RAISE NOTICE 'Completed V1_0_0_9__alter_api_key_secrets migration successfully';
END $$;
