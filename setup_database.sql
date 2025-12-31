USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HypnNXCRM')
BEGIN
    CREATE DATABASE HypnNXCRM;
END
GO

USE HypnNXCRM;
GO

-- Users Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL UNIQUE,
        Email NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(256) NOT NULL,
        Role NVARCHAR(20) DEFAULT 'User',
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Todos Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Todos')
BEGIN
    CREATE TABLE Todos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        Title NVARCHAR(200) NOT NULL,
        IsCompleted BIT DEFAULT 0,
        DueDate DATETIME NULL,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Transactions Table (Income/Expense)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transactions')
BEGIN
    CREATE TABLE Transactions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        Description NVARCHAR(200),
        Amount DECIMAL(18,2) NOT NULL,
        Type NVARCHAR(10) NOT NULL CHECK (Type IN ('Income', 'Expense')),
        TransactionDate DATETIME DEFAULT GETDATE()
    );
END
GO

-- Chat Messages
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
BEGIN
    CREATE TABLE ChatMessages (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        UserName NVARCHAR(50),
        Message NVARCHAR(MAX),
        Timestamp DATETIME DEFAULT GETDATE()
    );
END
GO

-- Reminders (Client Setup)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Reminders')
BEGIN
    CREATE TABLE Reminders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        ClientName NVARCHAR(100),
        Message NVARCHAR(MAX),
        ReminderDate DATETIME NOT NULL,
        IsSent BIT DEFAULT 0
    );
END
GO

-- Planner / Events
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AppEvents')
BEGIN
    CREATE TABLE AppEvents (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(MAX),
        StartDate DATETIME NOT NULL,
        EndDate DATETIME NOT NULL
    );
END
GO

-- Clients Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Clients')
BEGIN
    CREATE TABLE Clients (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        Name NVARCHAR(100) NOT NULL,
        CompanyName NVARCHAR(150),
        Email NVARCHAR(100),
        Phone NVARCHAR(50),
        Address NVARCHAR(MAX),
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Invoices Table (Renewals)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Invoices')
BEGIN
    CREATE TABLE Invoices (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        ClientId INT FOREIGN KEY REFERENCES Clients(Id),
        ServiceType NVARCHAR(50) NOT NULL, -- Domain, Server, Email, Software
        Description NVARCHAR(250),
        Amount DECIMAL(18,2) NOT NULL,
        RenewalDate DATETIME NOT NULL,
        IsPaid BIT DEFAULT 0,
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Projects Table

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Projects')
BEGIN
    CREATE TABLE Projects (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        Name NVARCHAR(150) NOT NULL,
        Description NVARCHAR(MAX),
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Attachments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Attachments')
BEGIN
    CREATE TABLE Attachments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        EntityType NVARCHAR(50) NOT NULL, -- Todo, Planner, Chat, Email
        EntityId INT NOT NULL,
        FilePath NVARCHAR(MAX) NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        UploadedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- ChatGroups Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatGroups')
BEGIN
    CREATE TABLE ChatGroups (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        CreatedBy INT FOREIGN KEY REFERENCES Users(Id),
        CreatedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- ChatGroupMembers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatGroupMembers')
BEGIN
    CREATE TABLE ChatGroupMembers (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        GroupId INT FOREIGN KEY REFERENCES ChatGroups(Id),
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        JoinedAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- EmailHistory Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailHistory')
BEGIN
    CREATE TABLE EmailHistory (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT FOREIGN KEY REFERENCES Users(Id),
        ToEmail NVARCHAR(MAX) NOT NULL,
        Subject NVARCHAR(255),
        Body NVARCHAR(MAX),
        AttachmentPath NVARCHAR(MAX),
        SentAt DATETIME DEFAULT GETDATE()
    );
END
GO

-- Alter Tables (Check if columns exist before adding)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Todos') AND name = 'ProjectId')
BEGIN
    ALTER TABLE Todos ADD ProjectId INT NULL FOREIGN KEY REFERENCES Projects(Id);
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Todos') AND name = 'AssignedToUserId')
BEGIN
    ALTER TABLE Todos ADD AssignedToUserId INT NULL FOREIGN KEY REFERENCES Users(Id);
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Todos') AND name = 'Remark')
BEGIN
    ALTER TABLE Todos ADD Remark NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clients') AND name = 'Remark')
BEGIN
    ALTER TABLE Clients ADD Remark NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'ReminderDate')
BEGIN
    ALTER TABLE Invoices ADD ReminderDate DATETIME NULL;
END
GO
