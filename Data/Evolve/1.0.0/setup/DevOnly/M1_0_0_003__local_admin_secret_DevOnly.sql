-- =====================================================================
-- Title: Local DevOnly - Insert Admin Secret for Local Secrets Manager
-- File: M1_0_0_003__local_admin_secret_DevOnly.sql
-- Location: Data/Evolve/1.0.0/setup/
-- Purpose: Manual step to insert a local admin secret for development
--          environments using the local secrets manager (not for prod!)
-- =====================================================================

DO $$
BEGIN
    RAISE NOTICE 'Inserting dummy admin merchant (DevOnly) if not exists...';
END $$;

/*
-- Insert or update dummy merchant for local dev
INSERT INTO fee_nominal.merchants (
    merchant_id,
    external_merchant_id,
    external_merchant_guid,
    name,
    status_id,
    created_at,
    updated_at,
    created_by
)
VALUES (
    '00000000-0000-0000-0000-000000000000',
    'admin-devonly',
    '00000000-0000-0000-0000-000000000000',
    'Admin Merchant (DevOnly)',
    (SELECT merchant_status_id FROM fee_nominal.merchant_statuses WHERE code = 'ACTIVE'),
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP,
    'devonly-script'
)
ON CONFLICT (merchant_id) DO UPDATE SET
    external_merchant_id = EXCLUDED.external_merchant_id,
    external_merchant_guid = EXCLUDED.external_merchant_guid,
    name = EXCLUDED.name,
    status_id = EXCLUDED.status_id,
    updated_at = CURRENT_TIMESTAMP,
    created_by = EXCLUDED.created_by;
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting manual local admin secret insert (DevOnly)...';
END $$;

-- MANUAL STEP: Run this ONLY in your local development environment!
-- This will insert a special admin secret for use with the AdminController
-- when running locally. Do NOT run in production or staging.

-- Upsert admin secret for local dev (admin-api-key-secret)
INSERT INTO fee_nominal.api_key_secrets (
    api_key,
    secret,
    merchant_id,
    status,
    is_revoked,
    created_at,
    updated_at,
    expires_at,
    revoked_at,
    last_rotated,
    scope
) VALUES (
    'admin-api-key-secret',                   -- api_key (special value for admin, must match code expectation)
    'your-local-admin-secret',                -- secret (REPLACE with your actual secret)
    NULL,                                    -- merchant_id (NULL for admin)
    'ACTIVE',                                -- status (all caps, matches code)
    FALSE,                                   -- is_revoked
    CURRENT_TIMESTAMP,                       -- created_at
    CURRENT_TIMESTAMP,                       -- updated_at
    NULL,                                    -- expires_at
    NULL,                                    -- revoked_at
    NULL,                                    -- last_rotated
    'admin'                                  -- scope (must be 'admin' for admin secrets)
)
ON CONFLICT (api_key) DO UPDATE SET
    secret = EXCLUDED.secret,
    status = EXCLUDED.status,
    is_revoked = EXCLUDED.is_revoked,
    updated_at = CURRENT_TIMESTAMP,
    expires_at = EXCLUDED.expires_at,
    revoked_at = EXCLUDED.revoked_at,
    last_rotated = EXCLUDED.last_rotated,
    scope = EXCLUDED.scope,
    merchant_id = EXCLUDED.merchant_id;

-- Upsert admin secret for local dev for ScheduleForger service
INSERT INTO fee_nominal.api_key_secrets (
    api_key,
    secret,
    merchant_id,
    status,
    is_revoked,
    created_at,
    updated_at,
    expires_at,
    revoked_at,
    last_rotated,
    scope
) VALUES (
    'scheduleforger-admin-api-key-secret',    -- api_key (serviceName-admin-api-key-secret)
    'your-local-scheduleforger-secret',       -- secret (REPLACE with your actual secret)
    NULL,                                    -- merchant_id (NULL for admin)
    'ACTIVE',                                -- status (all caps, matches code)
    FALSE,                                   -- is_revoked
    CURRENT_TIMESTAMP,                       -- created_at
    CURRENT_TIMESTAMP,                       -- updated_at
    NULL,                                    -- expires_at
    NULL,                                    -- revoked_at
    NULL,                                    -- last_rotated
    'admin'                                  -- scope (must be 'admin' for admin secrets)
)
ON CONFLICT (api_key) DO UPDATE SET
    secret = EXCLUDED.secret,
    status = EXCLUDED.status,
    is_revoked = EXCLUDED.is_revoked,
    updated_at = CURRENT_TIMESTAMP,
    expires_at = EXCLUDED.expires_at,
    revoked_at = EXCLUDED.revoked_at,
    last_rotated = EXCLUDED.last_rotated,
    scope = EXCLUDED.scope,
    merchant_id = EXCLUDED.merchant_id;

DO $$
BEGIN
    RAISE NOTICE 'Completed manual local admin secret insert (DevOnly).';
END $$;

-- =====================================================================
-- END OF FILE
-- ===================================================================== 