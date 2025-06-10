/*
Rollback: V1_0_0_107__fix_audit_function_rollback.sql
Description: Reverts the audit logging function and trigger to their previous state
Dependencies: None
Changes:
- Drops the enhanced log_audit_details function
- Drops the audit_merchants trigger
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting rollback of V1_0_0_107__fix_audit_function...';
END $$;

-- Drop the audit_merchants trigger
DROP TRIGGER IF EXISTS audit_merchants ON fee_nominal.merchants;
RAISE NOTICE 'Dropped audit_merchants trigger';

-- Drop the enhanced log_audit_details function
DROP FUNCTION IF EXISTS fee_nominal.log_audit_details();
RAISE NOTICE 'Dropped log_audit_details function';

-- Optionally, you may want to restore the previous version of the function and trigger here if you have a backup.
-- For now, this rollback only removes the changes introduced by V1_0_0_107.

-- Verify rollback
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM pg_proc WHERE proname = 'log_audit_details' AND pronamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal')
    ) THEN
        RAISE EXCEPTION 'log_audit_details function was not removed successfully';
    END IF;
    IF EXISTS (
        SELECT 1 FROM pg_trigger WHERE tgname = 'audit_merchants' AND tgrelid = (SELECT oid FROM pg_class WHERE relname = 'merchants' AND relnamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'fee_nominal'))
    ) THEN
        RAISE EXCEPTION 'audit_merchants trigger was not removed successfully';
    END IF;
    RAISE NOTICE 'Verified all changes were rolled back successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed rollback of V1_0_0_107__fix_audit_function successfully';
END $$; 