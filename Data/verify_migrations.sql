-- =============================================
-- Migration Verification Script
-- =============================================

-- Set search path
SET search_path TO fee_nominal;

-- Create temporary table to store verification results
CREATE TEMPORARY TABLE verification_results (
    check_name VARCHAR(100),
    status VARCHAR(20),
    details TEXT
);

-- Function to add verification result
CREATE OR REPLACE FUNCTION add_verification_result(
    p_check_name VARCHAR(100),
    p_status VARCHAR(20),
    p_details TEXT DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    INSERT INTO verification_results (check_name, status, details)
    VALUES (p_check_name, p_status, p_details);
END;
$$ LANGUAGE plpgsql;

-- 1. Check merchant_statuses table structure
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'fee_nominal'
        AND table_name = 'merchant_statuses'
        AND column_name = 'merchant_status_id'
        AND data_type = 'integer'
    ) THEN
        PERFORM add_verification_result('merchant_statuses_id_type', 'PASS');
    ELSE
        PERFORM add_verification_result('merchant_statuses_id_type', 'FAIL', 'merchant_status_id should be INTEGER');
    END IF;
END $$;

-- 2. Check api_key_secrets secret column length
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'fee_nominal'
        AND table_name = 'api_key_secrets'
        AND column_name = 'secret'
        AND character_maximum_length = 255
    ) THEN
        PERFORM add_verification_result('api_key_secrets_secret_length', 'PASS');
    ELSE
        PERFORM add_verification_result('api_key_secrets_secret_length', 'FAIL', 'secret column should be VARCHAR(255)');
    END IF;
END $$;

-- 3. Check merchant_statuses default data
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM merchant_statuses
        WHERE merchant_status_id = -2
        AND code = 'SUSPENDED'
    ) THEN
        PERFORM add_verification_result('merchant_statuses_default_data', 'PASS');
    ELSE
        PERFORM add_verification_result('merchant_statuses_default_data', 'FAIL', 'Default merchant statuses not found');
    END IF;
END $$;

-- 4. Check all required indexes
DO $$
DECLARE
    required_indexes TEXT[] := ARRAY[
        'idx_merchant_statuses_code',
        'idx_merchants_external_merchant_id',
        'idx_merchants_status_id',
        'idx_api_keys_merchant_id',
        'idx_api_keys_key',
        'idx_api_key_secrets_api_key',
        'idx_api_key_secrets_merchant_id'
    ];
    missing_indexes TEXT[];
BEGIN
    SELECT array_agg(index_name)
    INTO missing_indexes
    FROM unnest(required_indexes) AS index_name
    WHERE NOT EXISTS (
        SELECT 1
        FROM pg_indexes
        WHERE schemaname = 'fee_nominal'
        AND indexname = index_name
    );

    IF array_length(missing_indexes, 1) IS NULL THEN
        PERFORM add_verification_result('required_indexes', 'PASS');
    ELSE
        PERFORM add_verification_result('required_indexes', 'FAIL', 'Missing indexes: ' || array_to_string(missing_indexes, ', '));
    END IF;
END $$;

-- 5. Check updated_at triggers
DO $$
DECLARE
    required_triggers TEXT[] := ARRAY[
        'update_merchants_updated_at',
        'update_api_keys_updated_at',
        'update_transactions_updated_at'
    ];
    missing_triggers TEXT[];
BEGIN
    SELECT array_agg(trigger_name)
    INTO missing_triggers
    FROM unnest(required_triggers) AS trigger_name
    WHERE NOT EXISTS (
        SELECT 1
        FROM pg_trigger
        WHERE tgname = trigger_name
    );

    IF array_length(missing_triggers, 1) IS NULL THEN
        PERFORM add_verification_result('updated_at_triggers', 'PASS');
    ELSE
        PERFORM add_verification_result('updated_at_triggers', 'FAIL', 'Missing triggers: ' || array_to_string(missing_triggers, ', '));
    END IF;
END $$;

-- Display verification results
SELECT 
    check_name,
    status,
    details
FROM verification_results
ORDER BY 
    CASE status
        WHEN 'FAIL' THEN 1
        WHEN 'PASS' THEN 2
        ELSE 3
    END,
    check_name;

-- Cleanup
DROP FUNCTION add_verification_result(VARCHAR, VARCHAR, TEXT);
DROP TABLE verification_results; 