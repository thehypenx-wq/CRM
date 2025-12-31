-- Define the users to KEEP
DECLARE @KeepUserIds TABLE (Id INT);

INSERT INTO @KeepUserIds (Id)
SELECT Id FROM Users WHERE Username IN ('admin', 'mitesh', 'parth');

-- 1. Helper: Delete Child Records linked to data being deleted

-- Delete TicketComments for tickets that will be deleted OR comments by users to be deleted
DELETE FROM TicketComments 
WHERE TicketId IN (SELECT Id FROM Tickets WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds))
   OR UserId NOT IN (SELECT Id FROM @KeepUserIds);

-- Delete Todos for projects that will be deleted OR todos assigned to users to be deleted
DELETE FROM Todos 
WHERE ProjectId IN (SELECT Id FROM Projects WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds))
   OR AssignedToUserId NOT IN (SELECT Id FROM @KeepUserIds);

-- Delete Transactions linked to Accounts that will be deleted OR transactions by users to be deleted
DELETE FROM Transactions
WHERE AccountId IN (SELECT Id FROM Accounts WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds))
   OR UserId NOT IN (SELECT Id FROM @KeepUserIds);

-- Delete AccountAccess for accounts to be deleted OR access for users to be deleted
DELETE FROM AccountAccess
WHERE AccountId IN (SELECT Id FROM Accounts WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds))
   OR UserId NOT IN (SELECT Id FROM @KeepUserIds);

-- Delete InvoiceItems (if exists, assuming standard naming) - checking mainly Invoices
-- Since I don't have the schema for items, I'll rely on Cascade Delete if setup, or check Invoices first.
-- Assuming Invoices are the main parent for billing. 
-- Just in case InvoiceItems exists:
IF OBJECT_ID('dbo.InvoiceItems', 'U') IS NOT NULL
BEGIN
    DELETE FROM InvoiceItems 
    WHERE InvoiceId IN (SELECT Id FROM Invoices WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds));
END

-- 2. Delete Main Records owned by other users

DELETE FROM Notifications WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds) AND UserId IS NOT NULL; -- Keep global? User asked to remove dummy data.

DELETE FROM Tickets WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds);

DELETE FROM Projects WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds);

DELETE FROM Invoices WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds);

DELETE FROM Clients WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds);

DELETE FROM Accounts WHERE UserId NOT IN (SELECT Id FROM @KeepUserIds);

-- 3. Finally, delete the Users themselves
DELETE FROM Users WHERE Id NOT IN (SELECT Id FROM @KeepUserIds);

SELECT 'Cleanup Completed' as Status;
