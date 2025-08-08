DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_11__add_onboarding_timestamp_to_api_keys migration...';
END $$;

-- Set search path
SET search_path TO fee_nominal;
DO $$
BEGIN
    RAISE NOTICE 'Set search path to fee_nominal';
END $$;

ALTER TABLE fee_nominal.api_keys
ADD COLUMN IF NOT EXISTS onboarding_timestamp TIMESTAMP WITH TIME ZONE;

DO $$
BEGIN
    RAISE NOTICE 'Added onboarding_timestamp column to api_keys table';
END $$; 