CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE customers (
  id UUID PRIMARY KEY,
  external_id VARCHAR(100) NOT NULL UNIQUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE transactions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  customer_id UUID NOT NULL REFERENCES customers(id),
  external_transaction_id VARCHAR(255) NOT NULL,
  transaction_date TIMESTAMPTZ NOT NULL,
  amount NUMERIC(18,4) NOT NULL,
  currency CHAR(3) NOT NULL,
  source_channel VARCHAR(100) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  CONSTRAINT uq_external_transaction_id UNIQUE (external_transaction_id)
);

CREATE INDEX idx_transactions_customer_id ON transactions(customer_id);
CREATE INDEX idx_transactions_date ON transactions(transaction_date);
CREATE INDEX idx_transactions_currency ON transactions(currency);
CREATE INDEX idx_transactions_channel ON transactions(source_channel);
