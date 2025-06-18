DO $$
BEGIN
    RAISE NOTICE 'Starting V1_0_0_11__add_onboarding_timestamp_to_api_keys migration...';
END $$;

ALTER TABLE fee_nominal.api_keys
ADD COLUMN IF NOT EXISTS onboarding_timestamp TIMESTAMP WITH TIME ZONE;

DO $$
BEGIN
    RAISE NOTICE 'Added onboarding_timestamp column to api_keys table';
END $$; 