-- Create merchants table
CREATE TABLE merchants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_id VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(50) NOT NULL
);

-- Create api_keys table
CREATE TABLE api_keys (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES merchants(id),
    key VARCHAR(255) UNIQUE NOT NULL,
    description VARCHAR(255),
    rate_limit INTEGER NOT NULL DEFAULT 1000,
    allowed_endpoints TEXT[] NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_rotated_at TIMESTAMP,
    revoked_at TIMESTAMP,
    created_by VARCHAR(50) NOT NULL,
    onboarding_reference VARCHAR(50)
);

-- Create api_key_usage table for rate limiting
CREATE TABLE api_key_usage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID NOT NULL REFERENCES api_keys(id),
    endpoint VARCHAR(255) NOT NULL,
    request_count INTEGER NOT NULL DEFAULT 1,
    window_start TIMESTAMP NOT NULL,
    window_end TIMESTAMP NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX idx_merchants_external_id ON merchants(external_id);
CREATE INDEX idx_api_keys_merchant_id ON api_keys(merchant_id);
CREATE INDEX idx_api_keys_key ON api_keys(key);
CREATE INDEX idx_api_key_usage_api_key_id ON api_key_usage(api_key_id);
CREATE INDEX idx_api_key_usage_window ON api_key_usage(window_start, window_end);

-- Insert test merchant
INSERT INTO merchants (external_id, name, status, created_by)
VALUES ('DEV001', 'Development Merchant', 'ACTIVE', 'admin');

-- Insert test API key
INSERT INTO api_keys (
    merchant_id,
    key,
    description,
    rate_limit,
    allowed_endpoints,
    status,
    created_by
)
SELECT 
    m.id,
    'test_api_key',
    'Test API Key',
    1000,
    ARRAY['/api/v1/surchargefee/calculate', '/api/v1/surchargefee/calculate-batch', '/api/v1/refunds/process'],
    'ACTIVE',
    'admin'
FROM merchants m
WHERE m.external_id = 'DEV001'; 