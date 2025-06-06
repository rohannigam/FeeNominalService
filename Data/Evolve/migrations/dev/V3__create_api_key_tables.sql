-- Up Migration
CREATE TABLE IF NOT EXISTS fee_nominal.api_keys (
    api_key_id SERIAL PRIMARY KEY,
    merchant_id INTEGER NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    key VARCHAR(50) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP WITH TIME ZONE,
    expires_at TIMESTAMP WITH TIME ZONE
);

-- Create api_key_secrets table
CREATE TABLE IF NOT EXISTS fee_nominal.api_key_secrets (
    api_key_secret_id SERIAL PRIMARY KEY,
    api_key_id INTEGER NOT NULL REFERENCES fee_nominal.api_keys(api_key_id),
    secret VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP WITH TIME ZONE
);

-- Create api_key_usage_logs table
CREATE TABLE IF NOT EXISTS fee_nominal.api_key_usage_logs (
    log_id SERIAL PRIMARY KEY,
    api_key_id INTEGER NOT NULL REFERENCES fee_nominal.api_keys(api_key_id),
    endpoint VARCHAR(255) NOT NULL,
    method VARCHAR(10) NOT NULL,
    status_code INTEGER NOT NULL,
    response_time_ms INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_api_keys_merchant_id ON fee_nominal.api_keys(merchant_id);
CREATE INDEX IF NOT EXISTS idx_api_keys_key ON fee_nominal.api_keys(key);
CREATE INDEX IF NOT EXISTS idx_api_key_secrets_api_key_id ON fee_nominal.api_key_secrets(api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_logs_api_key_id ON fee_nominal.api_key_usage_logs(api_key_id);
CREATE INDEX IF NOT EXISTS idx_api_key_usage_logs_created_at ON fee_nominal.api_key_usage_logs(created_at);

/* -- Down Migration
DROP TABLE IF EXISTS fee_nominal.api_key_usage_logs;
DROP TABLE IF EXISTS fee_nominal.api_key_secrets;
DROP TABLE IF EXISTS fee_nominal.api_keys;  */