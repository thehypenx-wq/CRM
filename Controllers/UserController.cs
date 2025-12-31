using Microsoft.AspNetCore.Mvc;
using OfficeSuite.Models;
using Microsoft.Data.SqlClient;
using OfficeSuite.Services;
using OfficeSuite.Data;
using System.Data;
using Microsoft.AspNetCore.Authorization;

namespace OfficeSuite.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public UserController(SqlHelper db, Services.NotificationService notificationService, Services.PermissionService permissionService)
        {
            _db = db;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        public IActionResult Index()
        {
            if (!_permissionService.HasPermission("UserManagement")) return Forbid();
            var dt = _db.ExecuteQuery("SELECT * FROM Users");
            var users = new List<User>();
            foreach (DataRow row in dt.Rows)
            {
                users.Add(new User
                {
                    Id = (int)row["Id"],
                    Username = row["Username"]?.ToString() ?? "",
                    Email = row["Email"]?.ToString() ?? "",
                    Role = row["Role"]?.ToString() ?? "",
                    IsActive = row["IsActive"] != DBNull.Value && (bool)row["IsActive"],
                    CreatedAt = row["CreatedAt"] != DBNull.Value ? (DateTime)row["CreatedAt"] : DateTime.Now
                });
            }
            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(User user)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                 ModelState.AddModelError("", "All fields are required.");
                 return View(user);
            }

            // Check if username/email exists
            var check = _db.ExecuteQuery("SELECT * FROM Users WHERE Username = @u OR Email = @e", new SqlParameter[] {
                new SqlParameter("@u", user.Username),
                new SqlParameter("@e", user.Email)
            });
            if (check.Rows.Count > 0)
            {
                ModelState.AddModelError("", "Username or Email already exists.");
                return View(user);
            }

            string query = "INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES (@Username, @Email, @Password, 'User')";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@Password", user.PasswordHash) // In a real app, hash this!
            });

            _notificationService.AddNotification(null, $"New user '{user.Username}' created by {User.Identity?.Name ?? "Unknown"}", "System", null, "Users", null);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var dt = _db.ExecuteQuery("SELECT * FROM Users WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
            if (dt.Rows.Count == 0) return NotFound();
            
            var row = dt.Rows[0];
            var user = new User
            {
                Id = (int)row["Id"],
                Username = row["Username"]?.ToString() ?? "",
                Email = row["Email"]?.ToString() ?? ""
            };
            return View(user);
        }

        [HttpPost]
        public IActionResult Edit(User user)
        {
            string query = "UPDATE Users SET Username = @Username, Email = @Email WHERE Id = @Id";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Username", user.Username),
                new SqlParameter("@Email", user.Email),
                new SqlParameter("@Id", user.Id)
            });

            _notificationService.AddNotification(null, $"User '{user.Username}' updated by {User.Identity?.Name ?? "Unknown"}", "System", user.Id, "Users", null);

            return RedirectToAction("Index");
        }
        
        [HttpPost]
        public IActionResult ToggleStatus(int id)
        {
            _db.ExecuteNonQuery("UPDATE Users SET IsActive = ~IsActive WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
            _notificationService.AddNotification(null, $"User status toggled for ID: {id} by {User.Identity?.Name ?? "Unknown"}", "System", id, "Users", null);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                var param = new SqlParameter[] { new SqlParameter("@Id", id) };

                // 1. Unassign Todos assigned to this user
                _db.ExecuteNonQuery("UPDATE Todos SET AssignedToUserId = NULL WHERE AssignedToUserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                // 2. Delete Tickets & Comments
                _db.ExecuteNonQuery("DELETE FROM TicketComments WHERE UserId = @Id OR TicketId IN (SELECT Id FROM Tickets WHERE UserId = @Id)", new SqlParameter[] { new SqlParameter("@Id", id) });
                _db.ExecuteNonQuery("DELETE FROM Tickets WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                // 3. Delete Invoices
                _db.ExecuteNonQuery("DELETE FROM Invoices WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                // 4. Delete Projects & Todos
                // Delete comments and attachments for todos owned by user OR in projects owned by user
                _db.ExecuteNonQuery("DELETE FROM TodoComments WHERE UserId = @Id OR TodoId IN (SELECT Id FROM Todos WHERE UserId = @Id)", new SqlParameter[] { new SqlParameter("@Id", id) });
                _db.ExecuteNonQuery("DELETE FROM Attachments WHERE CreatedBy = @Id OR (EntityType='Todo' AND EntityId IN (SELECT Id FROM Todos WHERE UserId = @Id))", new SqlParameter[] { new SqlParameter("@Id", id) });
                
                // Delete Todos owned by user
                _db.ExecuteNonQuery("DELETE FROM Todos WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
                
                // Delete Todos in projects owned by user (that might have been created by others)
                _db.ExecuteNonQuery("DELETE FROM Todos WHERE ProjectId IN (SELECT Id FROM Projects WHERE UserId = @Id)", new SqlParameter[] { new SqlParameter("@Id", id) });
                
                // Delete Projects
                _db.ExecuteNonQuery("DELETE FROM Projects WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                // 5. Clients
                _db.ExecuteNonQuery("DELETE FROM Clients WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                // 6. Financials
                _db.ExecuteNonQuery("DELETE FROM Transactions WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
                _db.ExecuteNonQuery("DELETE FROM AccountAccess WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
                // Delete transactions in accounts owned by user
                _db.ExecuteNonQuery("DELETE FROM Transactions WHERE AccountId IN (SELECT Id FROM Accounts WHERE UserId = @Id)", new SqlParameter[] { new SqlParameter("@Id", id) });
                _db.ExecuteNonQuery("DELETE FROM Accounts WHERE UserId = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                // 7. Generic Cleanup
                // Ensure we delete notifications received by user AND created by user to avoid FK conflicts
                _db.ExecuteNonQuery("DELETE FROM Notifications WHERE UserId = @Id OR CreatedBy = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
                _db.ExecuteNonQuery("DELETE FROM Attachments WHERE CreatedBy = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                // 8. Finally Delete User
                _db.ExecuteNonQuery("DELETE FROM Users WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });

                _notificationService.AddNotification(null, $"User (ID: {id}) and all their data deleted by {User.Identity?.Name ?? "Unknown"}", "System", id, "Users", null);
                TempData["Success"] = "User and all associated data deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error deleting user: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Permissions(int id)
        {
            var userDt = _db.ExecuteQuery("SELECT Username FROM Users WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", id) });
            if (userDt.Rows.Count == 0) return NotFound();

            ViewBag.Username = userDt.Rows[0]["Username"].ToString();
            ViewBag.UserId = id;

            var permissions = _permissionService.GetUserPermissions(id);
            return View(permissions);
        }

        [HttpPost]
        public IActionResult Permissions(int id, List<int> moduleIds)
        {
            _permissionService.UpdatePermissions(id, moduleIds ?? new List<int>());
            TempData["Success"] = "Permissions updated successfully.";
            return RedirectToAction("Index");
        }
    }
}
