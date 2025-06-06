-- Up Migration
ALTER TABLE fee_nominal.merchant_audit_trail 
ADD COLUMN updated_by VARCHAR(50) NOT NULL DEFAULT 'SYSTEM';

/* -- Down Migration
ALTER TABLE fee_nominal.merchant_audit_trail 
DROP COLUMN updated_by;  */