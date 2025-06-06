-- Up Migration
ALTER TABLE fee_nominal.api_key_secrets ADD COLUMN IF NOT EXISTS last_rotated_at TIMESTAMP WITH TIME ZONE;

/* -- Down Migration
ALTER TABLE fee_nominal.api_key_secrets DROP COLUMN IF EXISTS last_rotated_at;  */