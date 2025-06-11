/*
Rollback: U1_0_0_111__add_updated_by_to_merchant_audit_trail_rollback.sql
Description: Removes user tracking from merchant audit logs
Dependencies: None
Changes:
- Removes updated_by column from merchant_audit_logs table
- Removes trigger and function for updated_by tracking
*/

DO $$
BEGIN
    RAISE NOTICE 'Starting running U1_0_0_111__add_updated_by_to_merchant_audit_trail_rollback.sql which is a rollback of V1_0_0_11__add_updated_by_to_merchant_audit_trail...';
END $$;

-- Drop trigger first
DROP TRIGGER IF EXISTS update_merchant_audit_updated_by ON fee_nominal.merchant_audit_logs;
DO $$
BEGIN
    RAISE NOTICE 'Dropped update_merchant_audit_updated_by trigger';
END $$;

-- Drop function
DROP FUNCTION IF EXISTS fee_nominal.update_merchant_audit_updated_by();
DO $$
BEGIN
    RAISE NOTICE 'Dropped update_merchant_audit_updated_by function';
END $$;

-- Remove column
ALTER TABLE fee_nominal.merchant_audit_logs 
DROP COLUMN IF EXISTS updated_by;
DO $$
BEGIN
    RAISE NOTICE 'Dropped updated_by column from merchant_audit_logs table';
END $$;

-- Verify rollback
DO $$ 
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'merchant_audit_logs' 
        AND column_name = 'updated_by'
    ) THEN
        RAISE EXCEPTION 'updated_by column was not removed successfully';
    END IF;
    
    IF EXISTS (
        SELECT 1 
        FROM pg_trigger 
        WHERE tgname = 'update_merchant_audit_updated_by'
    ) THEN
        RAISE EXCEPTION 'update_merchant_audit_updated_by trigger was not removed successfully';
    END IF;
    
    RAISE NOTICE 'Verified all changes were rolled back successfully';
END $$;

DO $$
BEGIN
    RAISE NOTICE 'Completed running U1_0_0_111__add_updated_by_to_merchant_audit_trail_rollback.sql which is a rollback of V1_0_0_11__add_updated_by_to_merchant_audit_trail successfully';
END $$; 