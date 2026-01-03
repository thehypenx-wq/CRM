using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OfficeSuite.Data;
using OfficeSuite.Models;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;

namespace OfficeSuite.Controllers
{
    public class AccountController : Controller
    {
        private readonly SqlHelper _db;

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
        }

        [Authorize]
        public IActionResult Index()
        {
            var userId = GetUserId();
            string query = @"
                SELECT a.*,
                       (SELECT STRING_AGG(u.Username, ', ') 
                        FROM AccountAccess aa 
                        JOIN Users u ON aa.UserId = u.Id 
                        WHERE aa.AccountId = a.Id) as SharedWith
                FROM Accounts a WHERE a.UserId = @UserId
                UNION
                SELECT a.*,
                       (SELECT STRING_AGG(u.Username, ', ') 
                        FROM AccountAccess aa 
                        JOIN Users u ON aa.UserId = u.Id 
                        WHERE aa.AccountId = a.Id) as SharedWith
                FROM Accounts a
                JOIN AccountAccess aa ON a.Id = aa.AccountId
                WHERE aa.UserId = @UserId";
            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var accounts = new List<Account>();
            foreach(DataRow row in dt.Rows)
            {
                accounts.Add(new Account { 
                    Id = (int)row["Id"], 
                    Name = row["Name"]?.ToString() ?? "", 
                    Currency = row["Currency"]?.ToString() ?? "",
                    Balance = (decimal)row["Balance"],
                    UserId = (int)row["UserId"],
                    SharedWithNames = row["SharedWith"]?.ToString() ?? ""
                });
            }

            // Fetch all users for dropdown
            var usersDt = _db.ExecuteQuery("SELECT Id, Username FROM Users WHERE Id != @Me", new SqlParameter[] { new SqlParameter("@Me", userId) });
            var users = new Dictionary<int, string>();
            foreach(DataRow u in usersDt.Rows) users.Add((int)u["Id"], u["Username"].ToString());
            ViewBag.AllUsers = users;

            return View(accounts);
        }

        [Authorize]
        public IActionResult Details(int id, int? userIdFilter)
        {
            var userId = GetUserId();
            
            // 1. Validate Access (Owner or Shared)
            var accessCheck = _db.ExecuteScalar(@"
                SELECT COUNT(1) FROM Accounts a 
                LEFT JOIN AccountAccess aa ON a.Id = aa.AccountId 
                WHERE a.Id = @AccId AND (a.UserId = @UserId OR aa.UserId = @UserId)", 
                new SqlParameter[] { new SqlParameter("@AccId", id), new SqlParameter("@UserId", userId) });
            
            if((int)accessCheck == 0) return Forbid();

            // 2. Fetch Account Info
            var accDt = _db.ExecuteQuery("SELECT * FROM Accounts WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
            if(accDt.Rows.Count == 0) return NotFound();
            
            var account = new Account {
                Id = (int)accDt.Rows[0]["Id"],
                Name = accDt.Rows[0]["Name"].ToString(),
                Currency = accDt.Rows[0]["Currency"]?.ToString() ?? "USD",
                Balance = (decimal)accDt.Rows[0]["Balance"]
            };

            // 3. Fetch Transactions
            string transQuery = @"
                SELECT t.*, a.Name as AccountName, u.Username as CreatedByName
                FROM Transactions t 
                LEFT JOIN Accounts a ON t.AccountId = a.Id 
                LEFT JOIN Users u ON t.UserId = u.Id
                WHERE t.AccountId = @AccId";
            
            if(userIdFilter.HasValue)
            {
                transQuery += " AND t.UserId = @FilterUser";
            }

            transQuery += " ORDER BY t.TransactionDate DESC";

            var parameters = new List<SqlParameter> { new SqlParameter("@AccId", id) };
            if (userIdFilter.HasValue) parameters.Add(new SqlParameter("@FilterUser", userIdFilter.Value));

            var transDt = _db.ExecuteQuery(transQuery, parameters.ToArray());
            var transactions = new List<Transaction>();
            foreach(DataRow r in transDt.Rows)
            {
                transactions.Add(new Transaction {
                    Id = (int)r["Id"],
                    UserId = (int)r["UserId"],
                    Description = r["Description"]?.ToString() ?? "",
                    Amount = (decimal)r["Amount"],
                    Type = r["Type"]?.ToString() ?? "",
                    TransactionDate = (DateTime)r["TransactionDate"],
                    CreatedByName = r["CreatedByName"]?.ToString() ?? "Unknown"
                });
            }

            // 4. Fetch Potential Editors (Owner + Shared Users) for Filter
            var editorsDt = _db.ExecuteQuery(@"
                SELECT u.Id, u.Username 
                FROM Users u 
                WHERE u.Id = (SELECT UserId FROM Accounts WHERE Id = @AccId)
                UNION
                SELECT u.Id, u.Username
                FROM Users u
                JOIN AccountAccess aa ON u.Id = aa.UserId
                WHERE aa.AccountId = @AccId",
                new SqlParameter[] { new SqlParameter("@AccId", id) });
            
            ViewBag.Editors = editorsDt.AsEnumerable().ToDictionary(row => (int)row["Id"], row => row["Username"].ToString());
            ViewBag.Transactions = transactions;
            ViewBag.FilterUser = userIdFilter;

            return View(account);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Create(string name, string currency, decimal balance)
        {
            var userId = GetUserId();
            string query = "INSERT INTO Accounts (UserId, Name, Currency, Balance) VALUES (@UserId, @Name, @Currency, @Balance)";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Name", name),
                new SqlParameter("@Currency", currency ?? "USD"),
                new SqlParameter("@Balance", balance)
            });
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public IActionResult Share(int accountId, string username)
        {
             var userId = GetUserId();
             // 1. Verify ownership
             var acc = _db.ExecuteScalar("SELECT COUNT(1) FROM Accounts WHERE Id = @Id AND UserId = @OwnerId", 
                 new SqlParameter[] { new SqlParameter("@Id", accountId), new SqlParameter("@OwnerId", userId) });
             
             if((int)acc == 0) return BadRequest("You do not own this account.");

             // 2. Find User ID to share with
             var targetIdObj = _db.ExecuteScalar("SELECT Id FROM Users WHERE Username = @User", 
                 new SqlParameter[] { new SqlParameter("@User", username) });

             if(targetIdObj == null) return BadRequest("User not found.");
             int targetId = (int)targetIdObj;

             // 3. Add Access
             string insert = "INSERT INTO AccountAccess (AccountId, UserId) VALUES (@Acc, @User)";
             try {
                _db.ExecuteNonQuery(insert, new SqlParameter[] { new SqlParameter("@Acc", accountId), new SqlParameter("@User", targetId) });
             } catch { /* Ignore duplicates */ }

             return RedirectToAction("Index");
        }

        [Authorize]
        [Authorize]
        [HttpPost]
        public IActionResult Edit(int id, string name, string currency)
        {
             try
             {
                 var userId = GetUserId();
                 // Check ownership
                 var checkObj = _db.ExecuteScalar("SELECT Count(1) FROM Accounts WHERE Id = @Id AND UserId = @UserId", 
                     new SqlParameter[] { new SqlParameter("@Id", id), new SqlParameter("@UserId", userId) });
                 
                 int check = checkObj != null && checkObj != DBNull.Value ? Convert.ToInt32(checkObj) : 0;
                 if(check == 0) return Forbid();

                 _db.ExecuteNonQuery("UPDATE Accounts SET Name = @Name, Currency = @Currency WHERE Id = @Id", 
                     new SqlParameter[] { 
                         new SqlParameter("@Name", name),
                         new SqlParameter("@Currency", currency),
                         new SqlParameter("@Id", id)
                     });
                 
                 TempData["Success"] = "Account updated successfully.";
             }
             catch(Exception ex)
             {
                 TempData["Error"] = "Error updating account: " + ex.Message;
             }
            
             return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public IActionResult Delete(int id)
        {
             var userId = GetUserId();
             // Check ownership
             var check = _db.ExecuteScalar("SELECT Count(1) FROM Accounts WHERE Id = @Id AND UserId = @UserId", 
                 new SqlParameter[] { new SqlParameter("@Id", id), new SqlParameter("@UserId", userId) });
             
             if((int)check == 0) return Forbid();

             try
             {
                 // Delete Access first
                 _db.ExecuteNonQuery("DELETE FROM AccountAccess WHERE AccountId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
                 
                 // Try delete Account
                 _db.ExecuteNonQuery("DELETE FROM Accounts WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
                 TempData["Success"] = "Account deleted successfully.";
             }
             catch (SqlException ex)
             {
                 if (ex.Number == 547) TempData["Error"] = "Cannot delete account. Transactions exist.";
                 else TempData["Error"] = "Error deleting account.";
             }

             return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public IActionResult DownloadStatement(int id)
        {
            var userId = GetUserId();
             // 1. Verify Access
             var accessCheck = _db.ExecuteScalar(@"
                SELECT COUNT(1) FROM Accounts a 
                LEFT JOIN AccountAccess aa ON a.Id = aa.AccountId 
                WHERE a.Id = @AccId AND (a.UserId = @UserId OR aa.UserId = @UserId)", 
                new SqlParameter[] { new SqlParameter("@AccId", id), new SqlParameter("@UserId", userId) });
            
            if((int)accessCheck == 0) return Forbid();

            // 2. Fetch Transactions
            string transQuery = @"
                SELECT t.*, u.Username as CreatedByName
                FROM Transactions t 
                LEFT JOIN Users u ON t.UserId = u.Id
                WHERE t.AccountId = @AccId
                ORDER BY t.TransactionDate DESC";
            
            var dt = _db.ExecuteQuery(transQuery, new SqlParameter[] { new SqlParameter("@AccId", id) });

            // 3. Generate CSV
            var sb = new StringBuilder();
            sb.AppendLine("Date,Description,Type,Amount,CreatedBy");

            foreach(DataRow row in dt.Rows)
            {
                var date = ((DateTime)row["TransactionDate"]).ToString("yyyy-MM-dd HH:mm");
                var desc = row["Description"]?.ToString().Replace(",", " ");
                var type = row["Type"]?.ToString();
                var amount = row["Amount"]?.ToString();
                var user = row["CreatedByName"]?.ToString();
                sb.AppendLine($"{date},{desc},{type},{amount},{user}");
            }

            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var content = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[bom.Length + content.Length];
            Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
            Buffer.BlockCopy(content, 0, result, bom.Length, content.Length);

            return File(result, "text/csv", $"Account_Statement_{id}.csv");
        }

        public AccountController(SqlHelper db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                if (role == "Admin")
                    return RedirectToAction("Index", "Home");
                else
                    return RedirectToAction("Index", "Todo");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            string query = "SELECT Id, Username, Role, PasswordHash, IsActive FROM Users WHERE (Email = @Input OR Username = @Input)";
            var parameters = new SqlParameter[] {
                new SqlParameter("@Input", model.Email)
            };

            var dt = _db.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                var dbPasswordHash = row["PasswordHash"]?.ToString() ?? "";

                // Simple password check (In production use BCrypt/Argon2)
                if (dbPasswordHash == model.Password) 
                {
                    if (row["IsActive"] != DBNull.Value && !(bool)row["IsActive"])
                    {
                        ModelState.AddModelError("", "Your account is deactivated. Please contact admin.");
                        return View(model);
                    }
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, row["Id"]?.ToString() ?? "0"),
                        new Claim(ClaimTypes.Name, row["Username"]?.ToString() ?? "Unknown"),
                        new Claim(ClaimTypes.Role, row["Role"]?.ToString() ?? "User")
                    };

                    var identity = new ClaimsIdentity(claims, "CookieAuth");
                    var principal = new ClaimsPrincipal(identity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                    };

                    await HttpContext.SignInAsync("CookieAuth", principal, authProperties);
                    
                    var userRole = row["Role"]?.ToString() ?? "User";
                    if (userRole == "Admin")
                        return RedirectToAction("Index", "Home");
                    else
                        return RedirectToAction("Index", "Todo");
                }
            }

            ModelState.AddModelError("", "Invalid Login Attempt");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (model.Password != model.ConfirmPassword) {
                ModelState.AddModelError("", "Passwords do not match");
                return View(model);
            }

            string checkQuery = "SELECT COUNT(1) FROM Users WHERE Email = @Email OR Username = @Username";
            int exists = (int)_db.ExecuteScalar(checkQuery, new SqlParameter[] {
                new SqlParameter("@Email", model.Email),
                new SqlParameter("@Username", model.Username)
            });

            if (exists > 0) {
                ModelState.AddModelError("", "User already exists");
                return View(model);
            }

            string insertQuery = "INSERT INTO Users (Username, Email, PasswordHash) VALUES (@Username, @Email, @Password)";
            _db.ExecuteNonQuery(insertQuery, new SqlParameter[] {
                new SqlParameter("@Username", model.Username),
                new SqlParameter("@Email", model.Email),
                new SqlParameter("@Password", model.Password) // Plaintext for demo, should be hashed
            });

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetSharedUsers(int accountId)
        {
            try
            {
                var userId = GetUserId();
                 // Check ownership
                 var checkObj = _db.ExecuteScalar("SELECT Count(1) FROM Accounts WHERE Id = @Id AND UserId = @UserId", 
                     new SqlParameter[] { new SqlParameter("@Id", accountId), new SqlParameter("@UserId", userId) });
                 
                 int check = checkObj != null && checkObj != DBNull.Value ? Convert.ToInt32(checkObj) : 0;
                 if(check == 0) return Forbid();

                var dt = _db.ExecuteQuery(@"
                    SELECT aa.UserId, u.Username
                    FROM AccountAccess aa
                    JOIN Users u ON aa.UserId = u.Id
                    WHERE aa.AccountId = @AccId",
                    new SqlParameter[] { new SqlParameter("@AccId", accountId) });

                var list = new List<object>();
                foreach(DataRow r in dt.Rows)
                {
                    list.Add(new { userId = (int)r["UserId"], username = r["Username"].ToString() });
                }
                return Json(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public IActionResult RevokeAccess(int accountId, int accessUserId)
        {
            try
            {
                var userId = GetUserId();
                 // Check ownership
                 var checkObj = _db.ExecuteScalar("SELECT Count(1) FROM Accounts WHERE Id = @Id AND UserId = @UserId", 
                     new SqlParameter[] { new SqlParameter("@Id", accountId), new SqlParameter("@UserId", userId) });
                 
                 int check = checkObj != null && checkObj != DBNull.Value ? Convert.ToInt32(checkObj) : 0;
                 if(check == 0) return Forbid();

                 _db.ExecuteNonQuery("DELETE FROM AccountAccess WHERE AccountId = @AccId AND UserId = @TargetId",
                     new SqlParameter[] { new SqlParameter("@AccId", accountId), new SqlParameter("@TargetId", accessUserId) });

                 return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error revoking access: " + ex.Message);
            }
        }
    }
}
