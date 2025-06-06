-- Add updated_at and expires_at columns to api_key_secrets table
ALTER TABLE api_key_secrets 
    ADD COLUMN IF NOT EXISTS updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    ADD COLUMN IF NOT EXISTS expires_at TIMESTAMP WITH TIME ZONE;

-- Add trigger for updated_at
CREATE TRIGGER update_api_key_secrets_updated_at
    BEFORE UPDATE ON api_key_secrets
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column(); 