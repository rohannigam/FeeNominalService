-- Rollback: Drop audit tables
DROP TABLE IF EXISTS merchant_audit_trail CASCADE;
DROP TABLE IF EXISTS audit_logs CASCADE; 