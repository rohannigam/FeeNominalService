-- Create transaction table
CREATE TABLE IF NOT EXISTS transactions (
    transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES merchants(merchant_id),
    surcharge_provider_id UUID REFERENCES surcharge_providers(surcharge_provider_id),
    surcharge_provider_config_id UUID REFERENCES surcharge_provider_configs(surcharge_provider_config_id),
    amount DECIMAL(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    surcharge_amount DECIMAL(19,4) NOT NULL,
    total_amount DECIMAL(19,4) NOT NULL,
    status VARCHAR(20) NOT NULL,
    external_reference VARCHAR(100),              -- External system's transaction reference
    external_source VARCHAR(50) NOT NULL,         -- Source system (e.g., 'SOAP_API', 'REST_API')
    external_transaction_id VARCHAR(100) NOT NULL, -- External system's transaction ID
    service_provider_response JSONB,
    service_provider_error JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create batch_transaction table
CREATE TABLE IF NOT EXISTS batch_transactions (
    batch_transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL REFERENCES merchants(merchant_id),
    surcharge_provider_id UUID REFERENCES surcharge_providers(surcharge_provider_id),
    surcharge_provider_config_id UUID REFERENCES surcharge_provider_configs(surcharge_provider_config_id),
    batch_reference VARCHAR(50) NOT NULL UNIQUE,
    status VARCHAR(20) NOT NULL,
    total_transactions INTEGER NOT NULL,
    successful_transactions INTEGER NOT NULL DEFAULT 0,
    failed_transactions INTEGER NOT NULL DEFAULT 0,
    external_reference VARCHAR(100),              -- External system's batch reference
    external_source VARCHAR(50) NOT NULL,         -- Source system (e.g., 'SOAP_API', 'REST_API')
    external_batch_id VARCHAR(100) NOT NULL,      -- External system's batch ID
    service_provider_response JSONB,
    service_provider_error JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE
); 