-- Apply IsForRent Migration Manually
-- Run this script if Migration was not applied automatically

-- Check if column exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsForRent')
BEGIN
    -- Add IsForRent column
    ALTER TABLE Products ADD IsForRent BIT NOT NULL DEFAULT 0;
    PRINT 'IsForRent column added successfully';
END
ELSE
BEGIN
    PRINT 'IsForRent column already exists';
END

-- Verify the column was added
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'IsForRent';

