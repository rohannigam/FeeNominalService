-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Function to check if a merchant is active
CREATE OR REPLACE FUNCTION is_merchant_active(p_merchant_id UUID)
RETURNS BOOLEAN AS $$
BEGIN
    RETURN EXISTS (
        SELECT 1
        FROM merchants m
        JOIN merchant_statuses ms ON m.status_id = ms.merchant_status_id
        WHERE m.merchant_id = p_merchant_id
        AND ms.code = 'ACTIVE'
        AND ms.is_active = true
    );
END;
$$ LANGUAGE plpgsql;

-- Function to get merchant's active API keys
CREATE OR REPLACE FUNCTION get_merchant_active_api_keys(p_merchant_id UUID)
RETURNS TABLE (
    api_key_id UUID,
    key TEXT,
    name TEXT,
    description TEXT,
    rate_limit INTEGER,
    allowed_endpoints TEXT[],
    expires_at TIMESTAMP WITH TIME ZONE
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        ak.api_key_id,
        ak.key,
        ak.name,
        ak.description,
        ak.rate_limit,
        ak.allowed_endpoints,
        ak.expires_at
    FROM api_keys ak
    WHERE ak.merchant_id = p_merchant_id
    AND ak.status = 'ACTIVE'
    AND (ak.expires_at IS NULL OR ak.expires_at > CURRENT_TIMESTAMP)
    AND ak.revoked_at IS NULL;
END;
$$ LANGUAGE plpgsql;

-- Function to check API key rate limit
CREATE OR REPLACE FUNCTION check_api_key_rate_limit(
    p_api_key_id UUID,
    p_endpoint TEXT,
    p_ip_address TEXT
)
RETURNS BOOLEAN AS $$
DECLARE
    v_rate_limit INTEGER;
    v_current_count INTEGER;
BEGIN
    -- Get the rate limit for the API key
    SELECT rate_limit INTO v_rate_limit
    FROM api_keys
    WHERE api_key_id = p_api_key_id;

    -- Get current request count for the window
    SELECT COALESCE(SUM(request_count), 0) INTO v_current_count
    FROM api_key_usage
    WHERE api_key_id = p_api_key_id
    AND endpoint = p_endpoint
    AND ip_address = p_ip_address
    AND window_start <= CURRENT_TIMESTAMP
    AND window_end > CURRENT_TIMESTAMP;

    -- Return true if under rate limit
    RETURN v_current_count < v_rate_limit;
END;
$$ LANGUAGE plpgsql;

-- Function to record API key usage
CREATE OR REPLACE FUNCTION record_api_key_usage(
    p_api_key_id UUID,
    p_endpoint TEXT,
    p_ip_address TEXT
)
RETURNS VOID AS $$
BEGIN
    INSERT INTO api_key_usage (
        api_key_id,
        endpoint,
        ip_address,
        request_count,
        window_start,
        window_end,
        created_at
    )
    VALUES (
        p_api_key_id,
        p_endpoint,
        p_ip_address,
        1,
        date_trunc('hour', CURRENT_TIMESTAMP),
        date_trunc('hour', CURRENT_TIMESTAMP) + interval '1 hour',
        CURRENT_TIMESTAMP
    )
    ON CONFLICT (api_key_id, endpoint, ip_address, window_start)
    DO UPDATE SET
        request_count = api_key_usage.request_count + 1,
        window_end = EXCLUDED.window_end;
END;
$$ LANGUAGE plpgsql;

-- Create triggers for updated_at
CREATE TRIGGER update_merchants_updated_at
    BEFORE UPDATE ON merchants
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_api_keys_updated_at
    BEFORE UPDATE ON api_keys
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_transactions_updated_at
    BEFORE UPDATE ON transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_batch_transactions_updated_at
    BEFORE UPDATE ON batch_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column(); 