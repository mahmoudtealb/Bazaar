-- Migration: Add DeletedBySender and DeletedByReceiver columns to ChatMessages table
-- Run this script directly on your database if EF migrations are not working

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = 'DeletedBySender')
BEGIN
    ALTER TABLE [dbo].[ChatMessages]
    ADD [DeletedBySender] bit NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ChatMessages]') AND name = 'DeletedByReceiver')
BEGIN
    ALTER TABLE [dbo].[ChatMessages]
    ADD [DeletedByReceiver] bit NOT NULL DEFAULT 0;
END
GO

