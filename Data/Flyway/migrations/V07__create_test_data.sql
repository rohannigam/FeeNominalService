-- Insert test data only if tables are empty
DO $$
BEGIN
    -- Insert merchant statuses if empty
    IF NOT EXISTS (SELECT 1 FROM merchant_statuses) THEN
        INSERT INTO merchant_statuses (code, name, description, is_active)
        VALUES 
            ('ACTIVE', 'Active', 'Merchant is active and can process transactions', true),
            ('INACTIVE', 'Inactive', 'Merchant is inactive and cannot process transactions', true),
            ('SUSPENDED', 'Suspended', 'Merchant is suspended due to compliance issues', true),
            ('PENDING', 'Pending', 'Merchant is pending activation', true);
    END IF;

    -- Insert test merchants if empty
    IF NOT EXISTS (SELECT 1 FROM merchants) THEN
        INSERT INTO merchants (
            merchant_id,
            external_merchant_id,
            external_merchant_guid,
            name,
            status_id,
            created_at,
            updated_at,
            created_by
        )
        SELECT 
            gen_random_uuid(),
            'TEST-MERCHANT-' || i,
            gen_random_uuid(),
            'Test Merchant ' || i,
            (SELECT merchant_status_id FROM merchant_statuses WHERE code = 'ACTIVE'),
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP,
            'SYSTEM'
        FROM generate_series(1, 5) i;
    END IF;

    -- Insert test surcharge providers if empty
    IF NOT EXISTS (SELECT 1 FROM surcharge_providers) THEN
        INSERT INTO surcharge_providers (
            provider_id,
            name,
            code,
            description,
            base_url,
            authentication_type,
            credentials_schema,
            status,
            created_at,
            updated_at
        )
        VALUES 
            (
                gen_random_uuid(),
                'Test Provider 1',
                'TEST1',
                'Test Surcharge Provider 1',
                'https://test1.example.com',
                'API_KEY',
                '{"type": "object", "properties": {"apiKey": {"type": "string"}}}',
                'ACTIVE',
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            ),
            (
                gen_random_uuid(),
                'Test Provider 2',
                'TEST2',
                'Test Surcharge Provider 2',
                'https://test2.example.com',
                'OAUTH2',
                '{"type": "object", "properties": {"clientId": {"type": "string"}, "clientSecret": {"type": "string"}}}',
                'ACTIVE',
                CURRENT_TIMESTAMP,
                CURRENT_TIMESTAMP
            );
    END IF;

    -- Insert test API keys if empty
    IF NOT EXISTS (SELECT 1 FROM api_keys) THEN
        INSERT INTO api_keys (
            api_key_id,
            merchant_id,
            key,
            name,
            description,
            rate_limit,
            allowed_endpoints,
            status,
            expiration_days,
            expires_at,
            created_at,
            updated_at,
            purpose
        )
        SELECT 
            gen_random_uuid(),
            m.merchant_id,
            'test-key-' || gen_random_uuid(),
            'Test API Key ' || i,
            'Test API Key Description ' || i,
            1000,
            ARRAY['/api/v1/transactions', '/api/v1/batch'],
            'ACTIVE',
            30,
            CURRENT_TIMESTAMP + interval '30 days',
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP,
            'TEST'
        FROM merchants m
        CROSS JOIN generate_series(1, 2) i;
    END IF;

    -- Insert test surcharge provider configs if empty
    IF NOT EXISTS (SELECT 1 FROM surcharge_provider_configs) THEN
        INSERT INTO surcharge_provider_configs (
            config_id,
            provider_id,
            merchant_id,
            config_name,
            api_version,
            credentials,
            is_active,
            metadata,
            created_at,
            updated_at
        )
        SELECT 
            gen_random_uuid(),
            sp.provider_id,
            m.merchant_id,
            'Test Config ' || i,
            'v1',
            CASE 
                WHEN sp.authentication_type = 'API_KEY' THEN 
                    '{"apiKey": "test-key-' || gen_random_uuid() || '"}'
                ELSE 
                    '{"clientId": "test-client-' || gen_random_uuid() || '", "clientSecret": "test-secret-' || gen_random_uuid() || '"}'
            END,
            true,
            '{"test": true}',
            CURRENT_TIMESTAMP,
            CURRENT_TIMESTAMP
        FROM surcharge_providers sp
        CROSS JOIN merchants m
        CROSS JOIN generate_series(1, 2) i;
    END IF;
END $$; 