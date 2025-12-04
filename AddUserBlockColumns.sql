-- ============================================
-- Add User Block Feature Columns
-- ============================================
-- Run this script in your SQL Server database
-- Database: StudentBazaar
-- ============================================

USE StudentBazaar;
GO

-- Check if columns already exist before adding them
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'IsBlocked')
BEGIN
    ALTER TABLE AspNetUsers
    ADD IsBlocked BIT NOT NULL DEFAULT 0;
    PRINT 'Column IsBlocked added successfully.';
END
ELSE
BEGIN
    PRINT 'Column IsBlocked already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'BlockReason')
BEGIN
    ALTER TABLE AspNetUsers
    ADD BlockReason NVARCHAR(500) NULL;
    PRINT 'Column BlockReason added successfully.';
END
ELSE
BEGIN
    PRINT 'Column BlockReason already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'BlockedAt')
BEGIN
    ALTER TABLE AspNetUsers
    ADD BlockedAt DATETIME2 NULL;
    PRINT 'Column BlockedAt added successfully.';
END
ELSE
BEGIN
    PRINT 'Column BlockedAt already exists.';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AspNetUsers') AND name = 'BlockedByUserId')
BEGIN
    ALTER TABLE AspNetUsers
    ADD BlockedByUserId INT NULL;
    PRINT 'Column BlockedByUserId added successfully.';
END
ELSE
BEGIN
    PRINT 'Column BlockedByUserId already exists.';
END
GO

PRINT 'All columns have been added successfully!';
GO

