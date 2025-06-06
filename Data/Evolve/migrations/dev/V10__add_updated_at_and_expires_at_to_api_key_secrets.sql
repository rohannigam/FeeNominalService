-- Up Migration
ALTER TABLE fee_nominal.api_key_secrets 
    ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS expires_at TIMESTAMP WITH TIME ZONE;

-- Add trigger for updated_at
CREATE TRIGGER update_api_key_secrets_updated_at
    BEFORE UPDATE ON fee_nominal.api_key_secrets
    FOR EACH ROW
    EXECUTE FUNCTION fee_nominal.update_updated_at_column();

/* -- Down Migration
DROP TRIGGER IF EXISTS update_api_key_secrets_updated_at ON fee_nominal.api_key_secrets;
ALTER TABLE fee_nominal.api_key_secrets 
    DROP COLUMN IF EXISTS updated_at,
    DROP COLUMN IF EXISTS expires_at; */ 