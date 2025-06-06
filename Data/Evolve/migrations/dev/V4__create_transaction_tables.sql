-- Up Migration
CREATE TABLE IF NOT EXISTS fee_nominal.transactions (
    transaction_id SERIAL PRIMARY KEY,
    merchant_id INTEGER NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    surcharge_provider_id INTEGER NOT NULL REFERENCES fee_nominal.surcharge_providers(surcharge_provider_id),
    amount NUMERIC(18,2) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    status VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS fee_nominal.batch_transactions (
    batch_transaction_id SERIAL PRIMARY KEY,
    merchant_id INTEGER NOT NULL REFERENCES fee_nominal.merchants(merchant_id),
    surcharge_provider_id INTEGER NOT NULL REFERENCES fee_nominal.surcharge_providers(surcharge_provider_id),
    status VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS fee_nominal.batch_transaction_items (
    batch_transaction_item_id SERIAL PRIMARY KEY,
    batch_transaction_id INTEGER NOT NULL REFERENCES fee_nominal.batch_transactions(batch_transaction_id),
    transaction_id INTEGER NOT NULL REFERENCES fee_nominal.transactions(transaction_id),
    amount NUMERIC(18,2) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    status VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

/* -- Down Migration
DROP TABLE IF EXISTS fee_nominal.batch_transaction_items;
DROP TABLE IF EXISTS fee_nominal.batch_transactions;
DROP TABLE IF EXISTS fee_nominal.transactions;  */