using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using OfficeSuite.Data;

namespace OfficeSuite.Data
{
    public static class DbInitializer
    {
        public static void Initialize(SqlHelper db)
        {
            // Create Statuses first as other tables depend on them
            string statusScript = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TodoStatuses')
            BEGIN
                CREATE TABLE TodoStatuses (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    StatusName NVARCHAR(50) NOT NULL,
                    ColorCode NVARCHAR(20) NOT NULL
                );
                INSERT INTO TodoStatuses (StatusName, ColorCode) VALUES 
                ('Pending', '#ffc107'),
                ('In Progress', '#17a2b8'),
                ('Completed', '#28a745'),
                ('On Hold', '#6c757d');
            END";
            db.ExecuteNonQuery(statusScript);

            string script = @"
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Todos')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Todos' AND COLUMN_NAME = 'IsDeleted')
        ALTER TABLE Todos ADD IsDeleted BIT NOT NULL DEFAULT 0;
    
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Todos' AND COLUMN_NAME = 'StatusId')
        ALTER TABLE Todos ADD StatusId INT NULL FOREIGN KEY REFERENCES TodoStatuses(Id);
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Clients')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clients' AND COLUMN_NAME = 'IsDeleted')
        ALTER TABLE Clients ADD IsDeleted BIT NOT NULL DEFAULT 0;
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Invoices')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Invoices' AND COLUMN_NAME = 'IsDeleted')
        ALTER TABLE Invoices ADD IsDeleted BIT NOT NULL DEFAULT 0;
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Accounts')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Accounts' AND COLUMN_NAME = 'Currency')
        ALTER TABLE Accounts ADD Currency NVARCHAR(10) NOT NULL DEFAULT 'USD';
END
";
            db.ExecuteNonQuery(script);

            string commentsScript = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TodoComments')
            BEGIN
                CREATE TABLE TodoComments (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    TodoId INT NOT NULL,
                    UserId INT NOT NULL,
                    Comment NVARCHAR(MAX),
                    AttachmentPath NVARCHAR(MAX),
                    CreatedAt DATETIME DEFAULT GETDATE(),
                    FOREIGN KEY (TodoId) REFERENCES Todos(Id) ON DELETE CASCADE
                );
            END";
            db.ExecuteNonQuery(commentsScript);

            string genericScript = @"
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AccountAccess')
BEGIN
    CREATE TABLE AccountAccess (
        Id INT PRIMARY KEY IDENTITY(1,1),
        AccountId INT NOT NULL,
        UserId INT NOT NULL
    );
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Reminders')
BEGIN
    CREATE TABLE Reminders (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT NOT NULL,
        ClientName NVARCHAR(100),
        Message NVARCHAR(MAX),
        ReminderDate DATETIME,
        IsSent BIT DEFAULT 0,
        IsDeleted BIT DEFAULT 0
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Reminders' AND COLUMN_NAME = 'IsDeleted')
        ALTER TABLE Reminders ADD IsDeleted BIT DEFAULT 0;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Notifications')
BEGIN
    CREATE TABLE Notifications (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UserId INT,
        Message NVARCHAR(MAX),
        Type NVARCHAR(50),
        RelatedEntityId INT,
        RelatedEntityName NVARCHAR(50),
        IsRead BIT DEFAULT 0,
        CreatedBy INT,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Notifications' AND COLUMN_NAME = 'IsRead')
        ALTER TABLE Notifications ADD IsRead BIT DEFAULT 0;
END
";
            db.ExecuteNonQuery(genericScript);

            string ticketScript = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Tickets')
            BEGIN
                CREATE TABLE Tickets (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    UserId INT NOT NULL,
                    ClientId INT NOT NULL,
                    Subject NVARCHAR(255),
                    Description NVARCHAR(MAX),
                    Priority NVARCHAR(50) DEFAULT 'Medium',
                    Status NVARCHAR(50) DEFAULT 'Open',
                    AttachmentPath NVARCHAR(MAX),
                    CreatedAt DATETIME DEFAULT GETDATE(),
                    IsDeleted BIT DEFAULT 0
                );
            END";
            db.ExecuteNonQuery(ticketScript);
            string ticketCommentScript = @"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TicketComments')
            BEGIN
                CREATE TABLE TicketComments (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    TicketId INT NOT NULL,
                    UserId INT NOT NULL,
                    Comment NVARCHAR(MAX),
                    AttachmentPath NVARCHAR(MAX),
                    CreatedAt DATETIME DEFAULT GETDATE(),
                    FOREIGN KEY (TicketId) REFERENCES Tickets(Id) ON DELETE CASCADE
                );
            END";
            db.ExecuteNonQuery(ticketCommentScript);

            string userProfileScript = @"
            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'ProfileImagePath')
                    ALTER TABLE Users ADD ProfileImagePath NVARCHAR(MAX) NULL;
                
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'IsActive')
                    ALTER TABLE Users ADD IsActive BIT NOT NULL DEFAULT 1;
            END";
            db.ExecuteNonQuery(userProfileScript);

            string todoProjectScript = @"
            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Todos')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Todos' AND COLUMN_NAME = 'ProjectId')
                    ALTER TABLE Todos ADD ProjectId INT NULL;
            END";
            db.ExecuteNonQuery(todoProjectScript);
            
            string todoCommentAttachmentScript = @"
            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TodoComments')
            BEGIN
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TodoComments' AND COLUMN_NAME = 'AttachmentPath')
                    ALTER TABLE TodoComments ADD AttachmentPath NVARCHAR(MAX) NULL;
            END";
            db.ExecuteNonQuery(todoCommentAttachmentScript);

            // Seed Default Users
            string seedUsersScript = @"
            IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
            BEGIN
                IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'admin')
                BEGIN
                    INSERT INTO Users (Username, Email, PasswordHash, Role, IsActive) 
                    VALUES ('admin', 'admin@test.com', 'admin', 'Admin', 1);
                END
                
                IF NOT EXISTS (SELECT * FROM Users WHERE Username = 'parth')
                BEGIN
                    INSERT INTO Users (Username, Email, PasswordHash, Role, IsActive) 
                    VALUES ('parth', 'parth@test.com', 'admin', 'Admin', 1);
                END
            END";
            db.ExecuteNonQuery(seedUsersScript);
        }
    }
}
