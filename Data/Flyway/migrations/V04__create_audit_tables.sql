-- Create audit_logs table
CREATE TABLE IF NOT EXISTS audit_logs (
    audit_log_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type VARCHAR(50) NOT NULL,  -- 'MERCHANT', 'API_KEY', 'SERVICE_PROVIDER', etc.
    entity_id UUID NOT NULL,
    action VARCHAR(50) NOT NULL,       -- 'CREATE', 'UPDATE', 'DELETE', 'REVOKE', etc.
    old_values JSONB,                  -- Previous state
    new_values JSONB,                  -- New state
    performed_by VARCHAR(50) NOT NULL, -- Who made the change
    performed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip_address VARCHAR(45),            -- IP address of the requester
    user_agent TEXT,                   -- User agent of the requester
    additional_info JSONB              -- Any additional context
);

-- Create authentication_attempt table
CREATE TABLE IF NOT EXISTS authentication_attempts (
    authentication_attempt_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    api_key_id UUID REFERENCES api_keys(api_key_id),
    merchant_id UUID NOT NULL REFERENCES merchants(merchant_id),
    ip_address VARCHAR(45) NOT NULL,
    user_agent VARCHAR(500) NOT NULL,
    status VARCHAR(20) NOT NULL,
    failure_reason VARCHAR(500),
    attempted_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    success BOOLEAN NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create merchant_audit_trail table
CREATE TABLE fee_nominal.merchant_audit_trail (
    merchant_audit_trail_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES fee_nominal.merchant(merchant_id),
    action VARCHAR(50) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    property_name VARCHAR(100),
    old_value TEXT,
    new_value TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by VARCHAR(50) NOT NULL DEFAULT 'SYSTEM',
    CONSTRAINT fk_merchant_audit_trail_merchant FOREIGN KEY (merchant_id) REFERENCES fee_nominal.merchant(merchant_id)
);

-- Indexes
CREATE INDEX idx_merchant_audit_trail_merchant_id ON fee_nominal.merchant_audit_trail(merchant_id);
CREATE INDEX idx_merchant_audit_trail_created_at ON fee_nominal.merchant_audit_trail(created_at);
CREATE INDEX idx_merchant_audit_trail_action ON fee_nominal.merchant_audit_trail(action);
CREATE INDEX idx_merchant_audit_trail_entity_type ON fee_nominal.merchant_audit_trail(entity_type);
CREATE INDEX idx_merchant_audit_trail_property_name ON fee_nominal.merchant_audit_trail(property_name); 