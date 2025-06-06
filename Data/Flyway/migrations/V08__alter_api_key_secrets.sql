-- Alter api_key_secrets table to increase secret column length
ALTER TABLE api_key_secrets ALTER COLUMN secret TYPE VARCHAR(255); 