-- M1_0_0_001__enable_dblink.sql
-- Enable dblink extension for database creation functionality
-- This migration must run before M1_0_0_2__initial_setup.sql

-- Enable dblink extension
CREATE EXTENSION IF NOT EXISTS dblink;

-- Verify dblink is installed
SELECT extname, extversion FROM pg_extension WHERE extname = 'dblink';

-- Test dblink functionality
SELECT dblink_connect('test_connection', 'dbname=postgres');
SELECT dblink_disconnect('test_connection');

-- Success notification
DO $$
BEGIN
    RAISE NOTICE 'dblink extension enabled successfully!';
END$$; 