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
    public class ClientController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public ClientController(SqlHelper db, Services.NotificationService notificationService, Services.PermissionService permissionService)
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
            if (!_permissionService.HasPermission("Clients")) return Forbid();
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            string query = @"
                SELECT c.*, u.Username as CreatedByUsername 
                FROM Clients c 
                LEFT JOIN Users u ON c.UserId = u.Id 
                WHERE c.IsDeleted = 0
                ORDER BY c.Name";
            
            var dt = _db.ExecuteQuery(query);

            var clients = new List<Client>();
            foreach (DataRow row in dt.Rows)
            {
                clients.Add(new Client
                {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    Name = row["Name"]?.ToString() ?? "",
                    CompanyName = row["CompanyName"]?.ToString() ?? "",
                    Email = row["Email"]?.ToString() ?? "",
                    Phone = row["Phone"]?.ToString() ?? "",
                    Address = row["Address"]?.ToString() ?? "",
                    Remark = row["Remark"]?.ToString() ?? "",
                    ClientCode = row["ClientCode"]?.ToString() ?? "",
                    CreatedByUsername = row["CreatedByUsername"]?.ToString() ?? "Unknown"
                });
            }

            return View(clients);
        }

        [HttpPost]
        public IActionResult Create(string name, string clientCode, string companyName, string email, string phone, string address, string remark, string returnUrl = null)
        {
            if(string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(clientCode)) return RedirectToAction("Index");

            var userId = GetUserId();
            string query = "INSERT INTO Clients (UserId, Name, ClientCode, CompanyName, Email, Phone, Address, Remark) VALUES (@UserId, @Name, @ClientCode, @CompanyName, @Email, @Phone, @Address, @Remark)";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Name", name),
                new SqlParameter("@ClientCode", clientCode),
                new SqlParameter("@CompanyName", companyName ?? (object)DBNull.Value),
                new SqlParameter("@Email", email ?? (object)DBNull.Value),
                new SqlParameter("@Phone", phone ?? (object)DBNull.Value),
                new SqlParameter("@Address", address ?? (object)DBNull.Value),
                new SqlParameter("@Remark", remark ?? (object)DBNull.Value),
            });

            _notificationService.AddNotification(null, $"New client '{name}' created by {User.Identity?.Name ?? "Unknown"}", "System", null, "Client", userId);

            if (!string.IsNullOrEmpty(returnUrl)) return Redirect(returnUrl);

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            string query = "SELECT * FROM Clients WHERE Id = @Id AND IsDeleted = 0";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            var dt = _db.ExecuteQuery(query, parameters.ToArray());
            
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var client = new Client
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                Name = row["Name"].ToString(),
                CompanyName = row["CompanyName"].ToString(),
                Email = row["Email"].ToString(),
                Phone = row["Phone"].ToString(),
                Address = row["Address"].ToString(),
                Remark = row["Remark"].ToString(),
                ClientCode = row["ClientCode"]?.ToString() ?? ""
            };

            return View(client);
        }

        [HttpPost]
        public IActionResult Edit(Client client)
        {
            if (string.IsNullOrWhiteSpace(client.Name)) return View(client);

            var userId = GetUserId();
            // Permission check
            string checkQuery = "SELECT UserId FROM Clients WHERE Id = @Id";
            var existing = _db.ExecuteQuery(checkQuery, new SqlParameter[] { new SqlParameter("@Id", client.Id) });
            if (existing.Rows.Count == 0) return NotFound();

            string query = @"
                UPDATE Clients 
                SET Name = @Name, ClientCode = @ClientCode, CompanyName = @CompanyName, Email = @Email, 
                    Phone = @Phone, Address = @Address, Remark = @Remark 
                WHERE Id = @Id";

            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Name", client.Name),
                new SqlParameter("@ClientCode", client.ClientCode),
                new SqlParameter("@CompanyName", client.CompanyName ?? (object)DBNull.Value),
                new SqlParameter("@Email", client.Email ?? (object)DBNull.Value),
                new SqlParameter("@Phone", client.Phone ?? (object)DBNull.Value),
                new SqlParameter("@Address", client.Address ?? (object)DBNull.Value),
                new SqlParameter("@Remark", client.Remark ?? (object)DBNull.Value),
                new SqlParameter("@Id", client.Id)
            });

            _notificationService.AddNotification(null, $"Client '{client.Name}' updated by {User.Identity?.Name ?? "Unknown"}", "System", client.Id, "Client", userId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            string query = "UPDATE Clients SET IsDeleted = 1 WHERE Id = @Id";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            _db.ExecuteNonQuery(query, parameters.ToArray());

            _notificationService.AddNotification(null, $"Client (ID: {id}) moved to trash by {User.Identity?.Name ?? "Unknown"}", "System", id, "Client", userId);

            return RedirectToAction("Index");
        }

        public IActionResult Trash()
        {
            var userId = GetUserId();
            string query = "SELECT * FROM Clients WHERE IsDeleted = 1";
            var dt = _db.ExecuteQuery(query);
            var clients = new List<Client>();
            foreach (DataRow row in dt.Rows)
            {
                clients.Add(new Client
                {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    Name = row["Name"].ToString(),
                    CompanyName = row["CompanyName"].ToString(),
                    Email = row["Email"].ToString(),
                    Phone = row["Phone"].ToString(),
                    Address = row["Address"].ToString(),
                    Remark = row["Remark"].ToString(),
                    ClientCode = row["ClientCode"]?.ToString() ?? ""
                });
            }
            return View(clients);
        }

        [HttpPost]
        public IActionResult Restore(int id)
        {
            var userId = GetUserId();
            string query = "UPDATE Clients SET IsDeleted = 0 WHERE Id = @Id";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            _db.ExecuteNonQuery(query, parameters.ToArray());
            return RedirectToAction("Trash");
        }

         [HttpPost]
        public IActionResult HardDelete(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            
            try
            {
                 // Admins can delete any, Users only their own
                string query = "DELETE FROM Clients WHERE Id = @Id" + (role != "Admin" ? " AND UserId = @UserId" : "");
                var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };
                if (role != "Admin") parameters.Add(new SqlParameter("@UserId", userId));

                int rows = _db.ExecuteNonQuery(query, parameters.ToArray());
                if(rows > 0)
                    TempData["Success"] = "Client permanently deleted.";
                else
                    TempData["Error"] = "Client not found or access denied.";
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547) TempData["Error"] = "Cannot delete client. They have associated tickets or invoices.";
                else TempData["Error"] = "Error deleting client.";
            }

            return RedirectToAction("Trash");
        }
        [HttpPost]
        public IActionResult QuickAdd(string name, string clientCode, string phone)
        {
            if (string.IsNullOrWhiteSpace(name)) return Json(new { success = false, message = "Name is required" });
            if (string.IsNullOrWhiteSpace(clientCode)) return Json(new { success = false, message = "Client Code is required" });

            var userId = GetUserId();
            try
            {
                _db.ExecuteNonQuery("INSERT INTO Clients (UserId, Name, ClientCode, Phone) VALUES (@UserId, @Name, @ClientCode, @Phone)",
                    new SqlParameter[] {
                        new SqlParameter("@UserId", userId),
                        new SqlParameter("@Name", name),
                        new SqlParameter("@ClientCode", clientCode),
                        new SqlParameter("@Phone", phone ?? (object)DBNull.Value)
                    });
                return Json(new { success = true, name = name, code = clientCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public IActionResult ExportToExcel()
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            string query = "SELECT Name, ClientCode, CompanyName, Email, Phone, Address, Remark FROM Clients WHERE IsDeleted = 0";
            var parameters = new List<SqlParameter>();

            if (role != "Admin")
            {
                query += " AND UserId = @UserId";
                parameters.Add(new SqlParameter("@UserId", userId));
            }

            var dt = _db.ExecuteQuery(query, parameters.ToArray());

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Name,Client Code,Company Name,Email,Phone,Address,Remark");

            foreach (DataRow row in dt.Rows)
            {
                var line = string.Join(",", 
                    "\"" + (row["Name"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["ClientCode"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["CompanyName"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["Email"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["Phone"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["Address"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\"",
                    "\"" + (row["Remark"]?.ToString()?.Replace("\"", "\"\"") ?? "") + "\""
                );
                builder.AppendLine(line);
            }

            return File(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"Clients_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
