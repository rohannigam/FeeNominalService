-- Rollback: U1_0_0_129__unique_active_config_per_provider_merchant_rollback.sql
-- Drops the partial unique index and restores the original unique constraint

-- Drop the partial unique index
DROP INDEX IF EXISTS fee_nominal.unique_active_config_per_provider_merchant;

-- Restore the original unique constraint
ALTER TABLE fee_nominal.surcharge_provider_configs
    ADD CONSTRAINT surcharge_provider_configs_surcharge_provider_id_merchant_i_key
    UNIQUE (surcharge_provider_id, merchant_id); 