/*
Migration: V1_0_0_1__create_schema.sql
Description: Creates the initial fee_nominal schema and utility function for updating timestamps
Dependencies: None (This is the first migration)
Changes:
- Creates fee_nominal schema
- Creates update_updated_at_column() function used by subsequent migrations for automatic timestamp updates
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_1__create_schema migration...';
END $$;

-- Drop existing schema and its dependencies
DROP SCHEMA IF EXISTS fee_nominal CASCADE;
RAISE NOTICE 'Dropped existing fee_nominal schema if it existed';

-- Create schema
CREATE SCHEMA IF NOT EXISTS fee_nominal;
RAISE NOTICE 'Created fee_nominal schema';

-- Set search path
SET search_path TO fee_nominal;
RAISE NOTICE 'Set search path to fee_nominal';

-- Create function for updating updated_at column
CREATE OR REPLACE FUNCTION fee_nominal.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';
RAISE NOTICE 'Created update_updated_at_column function';

DO $$
BEGIN
    RAISE NOTICE 'Completed V1_0_0_1__create_schema migration successfully';
END $$;
