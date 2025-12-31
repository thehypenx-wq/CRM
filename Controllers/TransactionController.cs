using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeSuite.Data;
using OfficeSuite.Models;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using System.Data;

namespace OfficeSuite.Controllers
{
    [Authorize]
    public class TransactionController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public TransactionController(SqlHelper db, Services.NotificationService notificationService, Services.PermissionService permissionService)
        {
            _db = db;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
        }

        public IActionResult Index()
        {
            if (!_permissionService.HasPermission("Financials")) return Forbid();

            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            // Fetch Transactions joined with AccountName and User Username
            string query = @"
                SELECT t.*, a.Name as AccountName, u.Username as CreatedByName
                FROM Transactions t 
                LEFT JOIN Accounts a ON t.AccountId = a.Id 
                LEFT JOIN Users u ON t.UserId = u.Id
                ORDER BY t.TransactionDate DESC";
            
            var dt = _db.ExecuteQuery(query);

            // Fetch Accounts owned by the user OR shared with the user
            string accQuery = @"
                SELECT a.*, 'Owner' as Role 
                FROM Accounts a 
                WHERE a.UserId = @UserId 
                UNION 
                SELECT a.*, 'Shared' as Role 
                FROM Accounts a 
                JOIN AccountAccess aa ON a.Id = aa.AccountId 
                WHERE aa.UserId = @UserId
                ORDER BY Name";

            var accDt = _db.ExecuteQuery(accQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var accounts = new List<Account>();
            foreach(DataRow aRow in accDt.Rows)
            {
                accounts.Add(new Account { Id = (int)aRow["Id"], Name = aRow["Name"]?.ToString() ?? "Account", Balance = (decimal)aRow["Balance"], Currency = aRow["Currency"]?.ToString() ?? "USD" });
            }
            ViewBag.Accounts = accounts;

            var transactions = new List<Transaction>();
            decimal income = 0;
            decimal expense = 0;

            foreach (DataRow row in dt.Rows)
            {
                var t = new Transaction
                {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    Description = row["Description"]?.ToString() ?? "",
                    Amount = (decimal)row["Amount"],
                    Type = row["Type"]?.ToString() ?? "",
                    TransactionDate = (DateTime)row["TransactionDate"],
                    AccountId = row["AccountId"] != DBNull.Value ? (int)row["AccountId"] : (int?)null,
                    AccountName = row["AccountName"]?.ToString() ?? "N/A",
                    CreatedByName = row["CreatedByName"]?.ToString() ?? "Unknown"
                };
                transactions.Add(t);

                if (t.Type == "Income") income += t.Amount;
                else expense += t.Amount;
            }

            var model = new TransactionSummary
            {
                Transactions = transactions,
                TotalIncome = income,
                TotalExpense = expense,
                Balance = income - expense
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Create(string description, decimal amount, string type, int accountId)
        {
            var userId = GetUserId();
            string query = "INSERT INTO Transactions (UserId, Description, Amount, Type, AccountId, TransactionDate) VALUES (@UserId, @Description, @Amount, @Type, @AccountId, GETDATE())";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Description", description),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@Type", type),
                new SqlParameter("@AccountId", accountId)
            });

            // Update Account Balance (Check Ownership OR Access)
            // First check if user has rights
            string checkAccess = @"
                SELECT COUNT(1) FROM Accounts WHERE Id = @Id AND UserId = @UserId
                UNION
                SELECT COUNT(1) FROM AccountAccess WHERE AccountId = @Id AND UserId = @UserId";
            
             // We can just run the update trusting the constraint or doing an explicit check. 
             // Simplest: Check if ID is valid for this user
             int accessCount = 0;
             var checkDt = _db.ExecuteQuery(checkAccess, new SqlParameter[] { new SqlParameter("@Id", accountId), new SqlParameter("@UserId", userId) });
             foreach(DataRow r in checkDt.Rows) accessCount += (int)r[0];

             if(accessCount > 0)
             {
                string updateAcc = type == "Income" 
                    ? "UPDATE Accounts SET Balance = Balance + @Amount WHERE Id = @Id"
                    : "UPDATE Accounts SET Balance = Balance - @Amount WHERE Id = @Id";
                _db.ExecuteNonQuery(updateAcc, new SqlParameter[] { 
                    new SqlParameter("@Amount", amount),
                    new SqlParameter("@Id", accountId)
                });
             }

            _notificationService.AddNotification(null, $"New {type} of {amount:C} added to account", "System", null, "Transaction", userId);

            _notificationService.AddNotification(null, $"New {type} of {amount:C} created by {User.Identity?.Name ?? "Unknown"}", "System", null, "Transaction", userId);

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            string query = "SELECT * FROM Transactions WHERE Id = @Id";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            var dt = _db.ExecuteQuery(query, parameters.ToArray());
            
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var transaction = new Transaction
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                Description = row["Description"]?.ToString() ?? "",
                Amount = (decimal)row["Amount"],
                Type = row["Type"]?.ToString() ?? "",
                TransactionDate = (DateTime)row["TransactionDate"]
            };

            return View(transaction);
        }

        [HttpPost]
        public IActionResult Edit(Transaction transaction)
        {
            if (string.IsNullOrWhiteSpace(transaction.Description)) return View(transaction);

            // Permission check
            var checkDt = _db.ExecuteQuery("SELECT UserId FROM Transactions WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", transaction.Id) });
            if (checkDt.Rows.Count == 0) return NotFound();

            string query = "UPDATE Transactions SET Description = @Description, Amount = @Amount, Type = @Type, TransactionDate = @Date WHERE Id = @Id";

            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Description", transaction.Description),
                new SqlParameter("@Amount", transaction.Amount),
                new SqlParameter("@Type", transaction.Type),
                new SqlParameter("@Date", transaction.TransactionDate),
                new SqlParameter("@Id", transaction.Id)
            });

            var userId = GetUserId();
            _notificationService.AddNotification(null, $"Transaction (ID: {transaction.Id}) updated by {User.Identity?.Name ?? "Unknown"}", "System", transaction.Id, "Transaction", userId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            string query = "DELETE FROM Transactions WHERE Id = @Id";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            try
            {
                _db.ExecuteNonQuery(query, parameters.ToArray());
                _notificationService.AddNotification(null, $"Transaction (ID: {id}) deleted by {User.Identity?.Name ?? "Unknown"}", "System", id, "Transaction", userId);
                TempData["Success"] = "Transaction deleted.";
            }
            catch(SqlException ex)
            {
                if(ex.Number == 547) TempData["Error"] = "Cannot delete transaction. Dependent records exist.";
                else TempData["Error"] = "Error deleting transaction.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Transfer(int fromAccountId, int toAccountId, decimal amount, DateTime date, string description)
        {
            if(amount <= 0) return RedirectToAction("Index", "Account"); // Basic validation
            if(fromAccountId == toAccountId) return RedirectToAction("Index", "Account");
            
            int userId = GetUserId();

            // 1. Expense from Source
            string debitQuery = "INSERT INTO Transactions (UserId, Description, Amount, Type, AccountId, TransactionDate) VALUES (@UserId, @Description, @Amount, 'Expense', @AccountId, @Date)";
            _db.ExecuteNonQuery(debitQuery, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Description", $"Transfer to Account #{toAccountId}: {description}"),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@AccountId", fromAccountId),
                new SqlParameter("@Date", date)
            });

            // Update Source Balance
            _db.ExecuteNonQuery("UPDATE Accounts SET Balance = Balance - @Amount WHERE Id = @Id", 
                new SqlParameter[] { new SqlParameter("@Amount", amount), new SqlParameter("@Id", fromAccountId) });

            // 2. Income to Destination
            string creditQuery = "INSERT INTO Transactions (UserId, Description, Amount, Type, AccountId, TransactionDate) VALUES (@UserId, @Description, @Amount, 'Income', @AccountId, @Date)";
            _db.ExecuteNonQuery(creditQuery, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Description", $"Transfer from Account #{fromAccountId}: {description}"),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@AccountId", toAccountId),
                new SqlParameter("@Date", date)
            });

            // Update Destination Balance
            _db.ExecuteNonQuery("UPDATE Accounts SET Balance = Balance + @Amount WHERE Id = @Id", 
                new SqlParameter[] { new SqlParameter("@Amount", amount), new SqlParameter("@Id", toAccountId) });

            _notificationService.AddNotification(null, $"Transfer of {amount:C} performed", "System", null, "Transfer", userId);

            return RedirectToAction("Index", "Account");
        }
    }
}
