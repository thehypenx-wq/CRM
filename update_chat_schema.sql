USE HypnNXCRM;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChatMessages') AND name = 'GroupName')
BEGIN
    ALTER TABLE ChatMessages ADD GroupName NVARCHAR(100) DEFAULT 'General';
END
GO
