
USE HypnNXCRM;
GO

-- Insert Default Admin User
IF NOT EXISTS (SELECT * FROM Users WHERE Email = 'admin@test.com')
BEGIN
    INSERT INTO Users (Username, Email, PasswordHash, Role) 
    VALUES ('admin', 'admin@test.com', 'admin', 'Admin');
END
GO

-- Seed Clients
IF NOT EXISTS (SELECT * FROM Clients)
BEGIN
    DECLARE @UserId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'admin@test.com');
    INSERT INTO Clients (UserId, Name, CompanyName, Email, Phone, Address) VALUES 
    (@UserId, 'John Doe', 'TechCorp', 'john@techcorp.com', '1234567890', '123 Tech St'),
    (@UserId, 'Jane Smith', 'DesignCo', 'jane@designco.com', '0987654321', '456 Art Ave'),
    (@UserId, 'Bob Brown', 'SoftSol', 'bob@softsol.com', '1122334455', '789 Soft Rd');
END
GO

-- Seed Todos
IF NOT EXISTS (SELECT * FROM Todos)
BEGIN
    DECLARE @UserId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'admin@test.com');
    INSERT INTO Todos (UserId, Title, IsCompleted, DueDate) VALUES
    (@UserId, 'Project Kickoff', 1, GETDATE()-5),
    (@UserId, 'Design Review', 0, GETDATE()+2),
    (@UserId, 'Client Meeting', 0, GETDATE()+1),
    (@UserId, 'Submit Report', 1, GETDATE()-1);
END
GO

-- Seed Transactions
IF NOT EXISTS (SELECT * FROM Transactions)
BEGIN
    DECLARE @UserId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'admin@test.com');
    INSERT INTO Transactions (UserId, Description, Amount, Type, TransactionDate) VALUES
    (@UserId, 'Consulting Fee', 5000.00, 'Income', GETDATE()-10),
    (@UserId, 'Software License', 200.00, 'Expense', GETDATE()-8),
    (@UserId, 'Hosting Payment', 150.00, 'Expense', GETDATE()-5),
    (@UserId, 'Web Development', 3000.00, 'Income', GETDATE()-2);
END
GO

-- Seed Invoices
IF NOT EXISTS (SELECT * FROM Invoices)
BEGIN
    DECLARE @UserId INT = (SELECT TOP 1 Id FROM Users WHERE Email = 'admin@test.com');
    DECLARE @ClientId1 INT = (SELECT TOP 1 Id FROM Clients WHERE Name = 'John Doe');
    DECLARE @ClientId2 INT = (SELECT TOP 1 Id FROM Clients WHERE Name = 'Jane Smith');

    INSERT INTO Invoices (UserId, ClientId, ServiceType, Description, Amount, RenewalDate, IsPaid) VALUES
    (@UserId, @ClientId1, 'Domain', 'techcorp.com Renewal', 20.00, GETDATE()+30, 0),
    (@UserId, @ClientId1, 'Server', 'VPS Renewal', 100.00, GETDATE()+5, 0),
    (@UserId, @ClientId2, 'Software', 'Adobe License', 600.00, GETDATE()-10, 1);
END
GO
