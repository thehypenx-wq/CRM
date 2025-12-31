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
    public class InvoiceController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public InvoiceController(SqlHelper db, Services.NotificationService notificationService, Services.PermissionService permissionService)
        {
            _db = db;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        private Invoice MapInvoiceFromRow(DataRow row)
        {
            return new Invoice
            {
                Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0,
                UserId = row["UserId"] != DBNull.Value ? (int)row["UserId"] : 0,
                ClientId = row["ClientId"] != DBNull.Value ? (int)row["ClientId"] : 0,
                ClientName = row["ClientName"]?.ToString() ?? "",
                ServiceType = row["ServiceType"]?.ToString() ?? "",
                Description = row["Description"]?.ToString() ?? "",
                Amount = row["Amount"] != DBNull.Value ? (decimal)row["Amount"] : 0,
                RenewalDate = row["RenewalDate"] != DBNull.Value ? (DateTime)row["RenewalDate"] : DateTime.Now,
                IsPaid = row["IsPaid"] != DBNull.Value && (bool)row["IsPaid"]
            };
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
        }

        public IActionResult Index()
        {
            if (!_permissionService.HasPermission("Invoices")) return Forbid();
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            // Join with Clients to get ClientName, Filter IsDeleted
            string query = @"
                SELECT i.*, c.Name as ClientName 
                FROM Invoices i 
                JOIN Clients c ON i.ClientId = c.Id 
                WHERE i.IsDeleted = 0
                ORDER BY RenewalDate";
            
            var dt = _db.ExecuteQuery(query);

            var invoices = new List<Invoice>();
            foreach (DataRow row in dt.Rows)
            {
                invoices.Add(MapInvoiceFromRow(row));
            }
            
            // Populate ViewBag for Client Dropdown
            string clientQuery = "SELECT Id, Name FROM Clients WHERE IsDeleted = 0 ORDER BY Name";
            var clientDt = _db.ExecuteQuery(clientQuery);
            
            var clients = new List<Client>();
            foreach(DataRow row in clientDt.Rows) {
                 clients.Add(new Client { Id = (int)row["Id"], Name = row["Name"]?.ToString() ?? "" });
            }
            ViewBag.Clients = clients;

            return View(invoices);
        }

        [HttpPost]
        public IActionResult Create(int clientId, string serviceType, string description, decimal amount, DateTime renewalDate)
        {
            var userId = GetUserId();
            string query = "INSERT INTO Invoices (UserId, ClientId, ServiceType, Description, Amount, RenewalDate) VALUES (@UserId, @ClientId, @ServiceType, @Description, @Amount, @RenewalDate); SELECT CAST(SCOPE_IDENTITY() as int)";
            var invoiceId = (int)_db.ExecuteScalar(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ClientId", clientId),
                new SqlParameter("@ServiceType", serviceType),
                new SqlParameter("@Description", description ?? (object)DBNull.Value),
                new SqlParameter("@Amount", amount),
                new SqlParameter("@RenewalDate", renewalDate)
            });

            // Add auto-reminder and planner event
            string clientName = _db.ExecuteScalar("SELECT Name FROM Clients WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", clientId) })?.ToString() ?? "Client";
            
            // 1. Reminder (7 days before)
            string reminderQuery = "INSERT INTO Reminders (UserId, ClientName, Message, ReminderDate) VALUES (@UserId, @ClientName, @Message, @ReminderDate)";
            _db.ExecuteNonQuery(reminderQuery, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ClientName", clientName),
                new SqlParameter("@Message", $"Renewal for Inv #{invoiceId}: {description}"),
                new SqlParameter("@ReminderDate", renewalDate.AddDays(-7))
            });

            // 2. Planner Event
            string eventQuery = "INSERT INTO AppEvents (UserId, Title, Description, StartDate, EndDate) VALUES (@UserId, @Title, @Description, @StartDate, @EndDate)";
            _db.ExecuteNonQuery(eventQuery, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Title", $"Renewal: {clientName}"),
                new SqlParameter("@Description", $"Invoice #{invoiceId} Renewal: {description}"),
                new SqlParameter("@StartDate", renewalDate.Date),
                new SqlParameter("@EndDate", renewalDate.Date.AddHours(23))
            });

            _notificationService.AddNotification(null, $"New invoice for {description} created (ID: {invoiceId})", "System", invoiceId, "Invoice", userId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult TogglePaid(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            
            // Check current status
            string checkQuery = "SELECT IsPaid, Amount, Description, UserId FROM Invoices WHERE Id = @Id AND IsDeleted = 0";
            var dt = _db.ExecuteQuery(checkQuery, new SqlParameter[] { new SqlParameter("@Id", id) });
            if (dt.Rows.Count == 0) return NotFound();

            var ownerId = (int)dt.Rows[0]["UserId"];
            if (role != "Admin" && ownerId != userId) return Forbid();

            bool isPaid = (bool)dt.Rows[0]["IsPaid"];
            decimal amount = (decimal)dt.Rows[0]["Amount"];
            string description = dt.Rows[0]["Description"].ToString();

            // Toggle
            string query = "UPDATE Invoices SET IsPaid = @NewStatus WHERE Id = @Id";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@NewStatus", !isPaid)
            });

            // If marking as PAID, create Income Transaction
            if (!isPaid)
            {
                // specific account? For now use default or first account.
                // Or maybe just create a transaction without AccountId? No, AccountId is FK likely.
                // We need to pick an account. Let's pick the first one or a "Cash" account.
                // For this implementation, I'll fetch the first account of the user.
                string accQuery = "SELECT TOP 1 Id FROM Accounts WHERE UserId = @UserId";
                var accObj = _db.ExecuteScalar(accQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
                
                if(accObj != null)
                {
                    int accountId = (int)accObj;
                    string transQuery = @"
                        INSERT INTO Transactions (AccountId, UserId, TransactionDate, Description, Amount, Type)
                        VALUES (@AccountId, @UserId, @Date, @Desc, @Amount, 'Income')";
                    
                    _db.ExecuteNonQuery(transQuery, new SqlParameter[] {
                        new SqlParameter("@AccountId", accountId),
                        new SqlParameter("@UserId", userId),
                        new SqlParameter("@Date", DateTime.Now),
                        new SqlParameter("@Desc", $"Payment for Inv #{id}: {description}"),
                        new SqlParameter("@Amount", amount)
                    });
                }
            }

            _notificationService.AddNotification(null, $"Invoice (ID: {id}) payment toggled by {User.Identity?.Name ?? "Unknown"}", "System", id, "Invoice", userId);

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            string query = @"
                SELECT i.*, c.Name as ClientName, c.Email
                FROM Invoices i 
                JOIN Clients c ON i.ClientId = c.Id 
                WHERE i.Id = @Id AND i.IsDeleted = 0";
            
            if(role != "Admin") query += " AND i.UserId = @UserId";

            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };
            if (role != "Admin") parameters.Add(new SqlParameter("@UserId", userId));

            var dt = _db.ExecuteQuery(query, parameters.ToArray());

            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var invoice = new Invoice
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                ClientId = (int)row["ClientId"],
                ClientName = row["ClientName"]?.ToString() ?? "",
                ClientEmail = row["Email"]?.ToString() ?? "",
                ServiceType = row["ServiceType"]?.ToString() ?? "",
                Description = row["Description"]?.ToString() ?? "",
                Amount = (decimal)row["Amount"],
                RenewalDate = (DateTime)row["RenewalDate"],
                IsPaid = (bool)row["IsPaid"]
            };

            // Populate Clients for dropdown
            string clientQuery = "SELECT Id, Name FROM Clients WHERE IsDeleted = 0";
            var clientParams = new List<SqlParameter>();
            if (role != "Admin")
            {
                clientQuery += " AND UserId = @UserId";
                clientParams.Add(new SqlParameter("@UserId", userId));
            }
            clientQuery += " ORDER BY Name";

            var clientDt = _db.ExecuteQuery(clientQuery, clientParams.ToArray());
            var clients = new List<Client>();
            foreach(DataRow cRow in clientDt.Rows) {
                 clients.Add(new Client { Id = (int)cRow["Id"], Name = cRow["Name"]?.ToString() ?? "" });
            }
            ViewBag.Clients = clients;

            return View(invoice);
        }

        [HttpPost]
        public IActionResult Edit(Invoice invoice)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            // ownership check
            string checkQuery = "SELECT UserId FROM Invoices WHERE Id = @Id";
            var dt = _db.ExecuteQuery(checkQuery, new SqlParameter[] { new SqlParameter("@Id", invoice.Id) });
            if (dt.Rows.Count == 0) return NotFound();
            var ownerId = (int)dt.Rows[0]["UserId"];
            if (role != "Admin" && ownerId != userId) return Forbid();

            string query = @"
                UPDATE Invoices 
                SET ClientId = @ClientId, ServiceType = @ServiceType, 
                    Description = @Description, Amount = @Amount, 
                    RenewalDate = @RenewalDate, IsPaid = @IsPaid
                WHERE Id = @Id";

            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@ClientId", invoice.ClientId),
                new SqlParameter("@ServiceType", invoice.ServiceType),
                new SqlParameter("@Description", invoice.Description ?? (object)DBNull.Value),
                new SqlParameter("@Amount", invoice.Amount),
                new SqlParameter("@RenewalDate", invoice.RenewalDate),
                new SqlParameter("@IsPaid", invoice.IsPaid),
                new SqlParameter("@Id", invoice.Id)
            });

            // Also update/add reminder/event on Edit
            string clientName = _db.ExecuteScalar("SELECT Name FROM Clients WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", invoice.ClientId) })?.ToString() ?? "Client";
            
            // For simplicity in Edit (without tracking IDs), we add new ones or can clear old ones by description pattern
            // Clear old ones first to avoid duplicates on every edit
            _db.ExecuteNonQuery("DELETE FROM Reminders WHERE UserId = @UserId AND Message LIKE @Pattern", 
                new SqlParameter[] { new SqlParameter("@UserId", userId), new SqlParameter("@Pattern", $"%Inv #{invoice.Id}:%") });
            _db.ExecuteNonQuery("DELETE FROM AppEvents WHERE UserId = @UserId AND Description LIKE @Pattern", 
                new SqlParameter[] { new SqlParameter("@UserId", userId), new SqlParameter("@Pattern", $"%Invoice #{invoice.Id} Renewal:%") });

            // Create new ones
            _db.ExecuteNonQuery("INSERT INTO Reminders (UserId, ClientName, Message, ReminderDate) VALUES (@UserId, @ClientName, @Message, @ReminderDate)", new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ClientName", clientName),
                new SqlParameter("@Message", $"Renewal for Inv #{invoice.Id}: {invoice.Description}"),
                new SqlParameter("@ReminderDate", invoice.RenewalDate.AddDays(-7))
            });

            _db.ExecuteNonQuery("INSERT INTO AppEvents (UserId, Title, Description, StartDate, EndDate) VALUES (@UserId, @Title, @Description, @StartDate, @EndDate)", new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Title", $"Renewal: {clientName}"),
                new SqlParameter("@Description", $"Invoice #{invoice.Id} Renewal: {invoice.Description}"),
                new SqlParameter("@StartDate", invoice.RenewalDate.Date),
                new SqlParameter("@EndDate", invoice.RenewalDate.Date.AddHours(23))
            });

            _notificationService.AddNotification(null, $"Invoice '{invoice.Description}' updated", "System", invoice.Id, "Invoice", userId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            // Check existence/ownership
            var check = _db.ExecuteQuery("SELECT UserId FROM Invoices WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
            if (check.Rows.Count == 0) return NotFound();
            
            if(role != "Admin" && (int)check.Rows[0]["UserId"] != userId) return Forbid();

            try
            {
                // Delete Invoices
                _db.ExecuteNonQuery("DELETE FROM Invoices WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "Invoice deleted.";
            }
            catch(SqlException ex)
            {
                if(ex.Number == 547) TempData["Error"] = "Cannot delete invoice. Dependent records exist.";
                else TempData["Error"] = "Error deleting invoice.";
            }

            _notificationService.AddNotification(null, $"Invoice #{id} deleted by {User.Identity?.Name ?? "Unknown"}", "System", id, "Invoices", userId);
            
            return RedirectToAction("Index");
        }

        public IActionResult Trash()
        {
             var userId = GetUserId();
             string query = @"
                SELECT i.*, c.Name as ClientName 
                FROM Invoices i
                LEFT JOIN Clients c ON i.ClientId = c.Id
                WHERE i.UserId = @UserId AND i.IsDeleted = 1
                ORDER BY i.RenewalDate";
            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@UserId", userId) });
             var invoices = new List<Invoice>();
            foreach (DataRow row in dt.Rows)
            {
                invoices.Add(MapInvoiceFromRow(row));
            }
             return View(invoices);
        }

        [HttpPost]
        public IActionResult Restore(int id)
        {
            var userId = GetUserId();
            _db.ExecuteNonQuery("UPDATE Invoices SET IsDeleted = 0 WHERE Id = @Id AND UserId = @UserId", 
                new SqlParameter[] { new SqlParameter("@Id", id), new SqlParameter("@UserId", userId) });
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult HardDelete(int id)
        {
            var userId = GetUserId();
            _db.ExecuteNonQuery("DELETE FROM Invoices WHERE Id = @Id AND UserId = @UserId", 
                new SqlParameter[] { new SqlParameter("@Id", id), new SqlParameter("@UserId", userId) });
            return RedirectToAction("Trash");
        }

        public IActionResult Print(int id)
        {
            var userId = GetUserId();
            string query = @"
                SELECT i.*, c.Name as ClientName, c.CompanyName, c.Address, c.Email, c.Phone
                FROM Invoices i 
                JOIN Clients c ON i.ClientId = c.Id 
                WHERE i.Id = @Id AND i.UserId = @UserId";
            
            var dt = _db.ExecuteQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@UserId", userId)
            });

            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var invoice = new Invoice
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                ClientId = (int)row["ClientId"],
                ClientName = row["ClientName"]?.ToString() ?? "",
                ServiceType = row["ServiceType"]?.ToString() ?? "",
                Description = row["Description"]?.ToString() ?? "",
                Amount = (decimal)row["Amount"],
                RenewalDate = (DateTime)row["RenewalDate"],
                IsPaid = (bool)row["IsPaid"]
            };

            // Using ViewBag for extras to avoid creating complex ViewModel just for this
            ViewBag.CompanyName = row["CompanyName"]?.ToString() ?? "";
            ViewBag.Address = row["Address"]?.ToString() ?? "";
            ViewBag.Email = row["Email"]?.ToString() ?? "";
            ViewBag.Phone = row["Phone"]?.ToString() ?? "";

            return View(invoice);
        }


        [HttpPost]
        public IActionResult Renew(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

             // Fetch original invoice to clone
            string query = "SELECT * FROM Invoices WHERE Id = @Id";
            if (role != "Admin") query += " AND UserId = @UserId";

            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };
            if (role != "Admin") parameters.Add(new SqlParameter("@UserId", userId));

            var dt = _db.ExecuteQuery(query, parameters.ToArray());
            
            if(dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var oldRenewalDate = (DateTime)row["RenewalDate"];
            var newRenewalDate = oldRenewalDate.AddYears(1); // Default to 1 year renewal, can be adjusted
            
            // Insert new invoice as "Pending" (IsPaid = 0)
             string insertQuery = @"
                INSERT INTO Invoices (UserId, ClientId, ServiceType, Description, Amount, RenewalDate, IsPaid, IsDeleted) 
                VALUES (@UserId, @ClientId, @ServiceType, @Description, @Amount, @RenewalDate, 0, 0)";
            
            _db.ExecuteNonQuery(insertQuery, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ClientId", (int)row["ClientId"]),
                new SqlParameter("@ServiceType", row["ServiceType"].ToString()),
                new SqlParameter("@Description", "RENEWAL: " + row["Description"].ToString()),
                new SqlParameter("@Amount", (decimal)row["Amount"]),
                new SqlParameter("@RenewalDate", newRenewalDate)
            });

            // Add auto-reminder for renewed invoice
            string rClientName = _db.ExecuteScalar("SELECT Name FROM Clients WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", (int)row["ClientId"]) })?.ToString() ?? "Client";
            string rMsg = "RENEWAL: " + row["Description"].ToString();
            string rQuery = "INSERT INTO Reminders (UserId, ClientName, Message, ReminderDate) VALUES (@UserId, @ClientName, @Message, @ReminderDate)";
            _db.ExecuteNonQuery(rQuery, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ClientName", rClientName),
                new SqlParameter("@Message", $"Next Renewal for {rMsg}"),
                new SqlParameter("@ReminderDate", newRenewalDate.AddDays(-7)) // 7 days before
            });

            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult ExportToExcel()
        {
            var userId = GetUserId();
            string query = @"
                SELECT c.Name as ClientName, i.ServiceType, i.Description, i.Amount, i.RenewalDate, i.IsPaid 
                FROM Invoices i 
                JOIN Clients c ON i.ClientId = c.Id 
                WHERE i.UserId = @UserId AND i.IsDeleted = 0";
            
            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@UserId", userId) });

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Client Name,Service Type,Description,Amount,Renewal Date,Status");

            foreach (DataRow row in dt.Rows)
            {
                var line = string.Join(",", 
                    "\"" + (row["ClientName"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["ServiceType"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["Description"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    row["Amount"]?.ToString() ?? "0",
                    Convert.ToDateTime(row["RenewalDate"]).ToShortDateString(),
                    (bool)(row["IsPaid"] ?? false) ? "Paid" : "Unpaid"
                );
                builder.AppendLine(line);
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Invoices_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
