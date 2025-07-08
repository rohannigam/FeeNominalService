/*
Rollback: U1_0_0_125__drop_batch_transactions_table_rollback.sql
Description: Recreates batch_transactions table if rollback is needed
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback for V1_0_0_125__drop_batch_transactions_table...';
END $$;

SET search_path TO fee_nominal;

CREATE TABLE IF NOT EXISTS batch_transactions (
    batch_transaction_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id UUID NOT NULL,
    batch_id VARCHAR(100) NOT NULL,
    status VARCHAR(20) NOT NULL,
    total_amount DECIMAL(19,4) NOT NULL,
    total_surcharge DECIMAL(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP WITH TIME ZONE,
    error_message VARCHAR(500),
    description VARCHAR(255)
);
DO $$ BEGIN RAISE NOTICE 'Recreated batch_transactions table'; END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback for V1_0_0_125__drop_batch_transactions_table.';
END $$; 