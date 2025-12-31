DECLARE @MiteshId INT;
SELECT @MiteshId = Id FROM Users WHERE Username = 'mitesh';

IF @MiteshId IS NOT NULL
BEGIN
    PRINT 'Found Mitesh ID: ' + CAST(@MiteshId AS VARCHAR);

    -- Identify Clients to Remove (All except top 2)
    DECLARE @ClientsToRemove TABLE (Id INT);
    INSERT INTO @ClientsToRemove (Id)
    SELECT Id FROM Clients 
    WHERE UserId = @MiteshId 
      AND Id NOT IN (SELECT TOP 2 Id FROM Clients WHERE UserId = @MiteshId ORDER BY Id);

    DECLARE @CountToRemove INT;
    SELECT @CountToRemove = COUNT(*) FROM @ClientsToRemove;
    PRINT 'Clients to remove: ' + CAST(@CountToRemove AS VARCHAR);

    IF @CountToRemove > 0
    BEGIN
        -- 1. Clean up Tickets linked to these clients
        DELETE FROM TicketComments 
        WHERE TicketId IN (SELECT Id FROM Tickets WHERE ClientId IN (SELECT Id FROM @ClientsToRemove));
        
        DELETE FROM Tickets 
        WHERE ClientId IN (SELECT Id FROM @ClientsToRemove);
        PRINT 'Deleted dependent Tickets.';

        -- 2. Clean up Invoices linked to these clients
        IF OBJECT_ID('dbo.InvoiceItems', 'U') IS NOT NULL
        BEGIN
            DELETE FROM InvoiceItems 
            WHERE InvoiceId IN (SELECT Id FROM Invoices WHERE ClientId IN (SELECT Id FROM @ClientsToRemove));
        END

        -- Also delete InvoicePayments if exists? Safest to assume Invoice is the main parent.
        IF OBJECT_ID('dbo.InvoicePayments', 'U') IS NOT NULL
        BEGIN
             DELETE FROM InvoicePayments
             WHERE InvoiceId IN (SELECT Id FROM Invoices WHERE ClientId IN (SELECT Id FROM @ClientsToRemove));
        END

        DELETE FROM Invoices 
        WHERE ClientId IN (SELECT Id FROM @ClientsToRemove);
        PRINT 'Deleted dependent Invoices.';

        -- 3. Delete the Clients themselves
        DELETE FROM Clients 
        WHERE Id IN (SELECT Id FROM @ClientsToRemove);
        PRINT 'Deleted Clients.';
    END
    ELSE
    BEGIN
        PRINT 'No clients to remove (Count <= 2).';
    END
END
ELSE
BEGIN
    PRINT 'User Mitesh not found.';
END
