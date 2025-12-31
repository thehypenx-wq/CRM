USE HypnNXCRM;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL FOREIGN KEY REFERENCES Users(Id), -- NULL means "All Users" or "System"
        Message NVARCHAR(MAX) NOT NULL,
        Type NVARCHAR(50) NOT NULL, -- 'Chat', 'Todo', 'Planner', 'System'
        RelatedEntityId INT NULL,
        RelatedEntityName NVARCHAR(100) NULL, -- 'Todo', 'Project', 'Event'
        IsRead BIT DEFAULT 0,
        CreatedAt DATETIME DEFAULT GETDATE(),
        CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id) -- Who caused this notification
    );
END
GO
