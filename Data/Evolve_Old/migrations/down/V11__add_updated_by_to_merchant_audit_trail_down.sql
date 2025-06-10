-- Down Migration
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 
        FROM information_schema.columns 
        WHERE table_schema = 'fee_nominal' 
        AND table_name = 'merchant_audit_trail' 
        AND column_name = 'updated_by'
    ) THEN
        ALTER TABLE fee_nominal.merchant_audit_trail 
        DROP COLUMN updated_by;
    END IF;
END $$;  */
