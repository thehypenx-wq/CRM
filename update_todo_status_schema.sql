USE HypnNXCRM;
GO

-- 1. Create TodoStatuses Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TodoStatuses')
BEGIN
    CREATE TABLE TodoStatuses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        StatusName NVARCHAR(50) NOT NULL,
        ColorCode NVARCHAR(20) NOT NULL -- Hex code or Bootstrap class
    );

    -- 2. Seed Default Statuses
    INSERT INTO TodoStatuses (StatusName, ColorCode) VALUES 
    ('Pending', '#ffc107'),      -- Warning (Yellow)
    ('In Progress', '#17a2b8'),  -- Info (Blue)
    ('Completed', '#28a745'),    -- Success (Green)
    ('On Hold', '#6c757d');      -- Secondary (Grey)
END
GO

-- 3. Add StatusId to Todos Table
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Todos') AND name = 'StatusId')
BEGIN
    ALTER TABLE Todos ADD StatusId INT NULL FOREIGN KEY REFERENCES TodoStatuses(Id);
END
GO

-- 4. Migrate Existing Data (IsCompleted -> StatusId)
-- If IsCompleted = 1 -> Completed (3), Else -> Pending (1)
UPDATE Todos 
SET StatusId = CASE WHEN IsCompleted = 1 THEN 3 ELSE 1 END
WHERE StatusId IS NULL;
GO
