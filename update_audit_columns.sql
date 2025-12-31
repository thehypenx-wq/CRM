USE HypnNXCRM;
GO

-- 1. AppEvents (Planner)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AppEvents') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE AppEvents ADD CreatedDate DATETIME DEFAULT GETDATE();
END
GO

-- 2. Reminders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Reminders') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE Reminders ADD CreatedDate DATETIME DEFAULT GETDATE();
END
GO

-- 3. Transactions
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'CreatedDate')
BEGIN
    ALTER TABLE Transactions ADD CreatedDate DATETIME DEFAULT GETDATE();
END
GO

-- 4. Attachments (Add CreatedBy to track uploader)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Attachments') AND name = 'CreatedBy')
BEGIN
    ALTER TABLE Attachments ADD CreatedBy INT NULL REFERENCES Users(Id);
END
GO

-- 5. ChatGroupMembers (Add AddedBy to track who added the member)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ChatGroupMembers') AND name = 'AddedBy')
BEGIN
    ALTER TABLE ChatGroupMembers ADD AddedBy INT NULL REFERENCES Users(Id);
END
GO
