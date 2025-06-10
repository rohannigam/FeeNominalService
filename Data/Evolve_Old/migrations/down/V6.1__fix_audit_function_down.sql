DROP TRIGGER IF EXISTS audit_transactions ON fee_nominal.transactions;
DROP TRIGGER IF EXISTS audit_api_keys ON fee_nominal.api_keys;
DROP TRIGGER IF EXISTS audit_merchants ON fee_nominal.merchants;
DROP FUNCTION IF EXISTS fee_nominal.log_audit_details();  */
