-- Drop trigger first
DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON api_key_secrets;

-- Remove columns
ALTER TABLE api_key_secrets 
    DROP COLUMN IF EXISTS updated_at,
    DROP COLUMN IF EXISTS expires_at; 