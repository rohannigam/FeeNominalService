-- Migration: V1_0_0_29__unique_active_config_per_provider_merchant.sql
-- Drops the old unique constraint and adds a partial unique index for active configs only

DO $$
BEGIN
    -- Drop the old unique constraint if it exists
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints 
        WHERE constraint_name = 'surcharge_provider_configs_surcharge_provider_id_merchant_i_key'
            AND table_name = 'surcharge_provider_configs'
            AND table_schema = 'fee_nominal'
    ) THEN
        ALTER TABLE fee_nominal.surcharge_provider_configs
            DROP CONSTRAINT surcharge_provider_configs_surcharge_provider_id_merchant_i_key;
    END IF;
END $$;

-- Add a partial unique index for only active configs
CREATE UNIQUE INDEX IF NOT EXISTS unique_active_config_per_provider_merchant
    ON fee_nominal.surcharge_provider_configs (surcharge_provider_id, merchant_id)
    WHERE is_active = true; 