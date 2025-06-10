DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON fee_nominal.api_key_secrets;
ALTER TABLE fee_nominal.api_key_secrets 
    DROP COLUMN IF EXISTS updated_at,
    DROP COLUMN IF EXISTS expires_at; */
