-- Add IsForRent column to Products table
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsForRent')
BEGIN
    ALTER TABLE Products ADD IsForRent BIT NOT NULL DEFAULT 0;
    PRINT 'IsForRent column added successfully';
END
ELSE
BEGIN
    PRINT 'IsForRent column already exists';
END

