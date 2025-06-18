DO $$
BEGIN
    RAISE NOTICE 'Starting rollback for V1_0_0_11__add_onboarding_timestamp_to_api_keys...';
END $$;

ALTER TABLE fee_nominal.api_keys
DROP COLUMN IF EXISTS onboarding_timestamp;

DO $$
BEGIN
    RAISE NOTICE 'Removed onboarding_timestamp column from api_keys table';
END $$; 