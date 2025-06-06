-- Rollback: Alter api_key_secrets table to revert secret column length
ALTER TABLE api_key_secrets ALTER COLUMN secret TYPE VARCHAR(100); 