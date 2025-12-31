# CRM - OfficeSuite

Comprehensive CRM and Office Management Suite.

## Features
- Project & Ticket Management
- Client & Invoice Tracking
- Transaction & Financial Logging
- Todo & Planner
- Dynamic User Permissions

## Database Setup
Since the database is hosted on a remote server, the primary backup is provided via SQL scripts:
1. Run `setup_database.sql` to create the schema.
2. Run `seed_data.sql` for initial sample data.

### Manual Backup (.bak)
If you have access to the SQL Server machine, you can generate a `.bak` file using:
```sql
BACKUP DATABASE [HypnNXCRM] TO DISK = 'C:\Backup\HypnNXCRM.bak'
```

## How to Run
1. Open the solution in Visual Studio or use `dotnet run`.
2. Ensure your connection string in `appsettings.json` is correct.
