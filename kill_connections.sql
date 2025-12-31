USE master;
GO
DECLARE @DatabaseName nvarchar(50) = 'HypnNXCRM';
DECLARE @SQL nvarchar(max) = '';

SELECT @SQL = @SQL + 'KILL ' + CAST(session_id AS varchar(10)) + '; '
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID(@DatabaseName)
AND session_id <> @@SPID;

EXEC(@SQL);
GO
