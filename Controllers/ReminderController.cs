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
    public class ReminderController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public ReminderController(SqlHelper db, Services.NotificationService notificationService, Services.PermissionService permissionService)
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
            if (!_permissionService.HasPermission("Reminders")) return Forbid();
            var userId = GetUserId();
            string query = "SELECT * FROM Reminders WHERE UserId = @UserId AND (IsDeleted = 0 OR IsDeleted IS NULL) ORDER BY ReminderDate";
            var dt = _db.ExecuteQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId)
            });

            var reminders = new List<Reminder>();
            foreach (DataRow row in dt.Rows)
            {
                reminders.Add(new Reminder
                {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    ClientName = row["ClientName"]?.ToString() ?? "",
                    Message = row["Message"]?.ToString() ?? "",
                    ReminderDate = (DateTime)row["ReminderDate"],
                    IsSent = (bool)row["IsSent"]
                });
            }

            // Clients for dropdown
            string clientQuery = "SELECT Id, Name, ClientCode FROM Clients WHERE UserId = @UserId AND IsDeleted = 0 ORDER BY Name";
            var clientDt = _db.ExecuteQuery(clientQuery, new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var clients = new List<Client>();
            foreach(DataRow cRow in clientDt.Rows) {
                clients.Add(new Client { 
                    Id = (int)cRow["Id"], 
                    Name = cRow["Name"]?.ToString() ?? "",
                    ClientCode = cRow["ClientCode"]?.ToString() ?? "" 
                });
            }
            ViewBag.Clients = clients;

            return View(reminders);
        }

        [HttpPost]
        public IActionResult Create(string clientName, string message, DateTime reminderDate)
        {
            var userId = GetUserId();
            string query = "INSERT INTO Reminders (UserId, ClientName, Message, ReminderDate) VALUES (@UserId, @ClientName, @Message, @ReminderDate)";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ClientName", clientName),
                new SqlParameter("@Message", message),
                new SqlParameter("@ReminderDate", reminderDate)
            });

            _notificationService.AddNotification(null, $"New reminder for {clientName} added by {User.Identity?.Name ?? "Unknown"}", "System", null, "Reminders", userId);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = GetUserId();
            var dt = _db.ExecuteQuery("SELECT * FROM Reminders WHERE Id = @Id AND UserId = @UserId", 
                new SqlParameter[] { new SqlParameter("@Id", id), new SqlParameter("@UserId", userId) });
            
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var rem = new Reminder
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                ClientName = row["ClientName"]?.ToString() ?? "",
                Message = row["Message"]?.ToString() ?? "",
                ReminderDate = (DateTime)row["ReminderDate"],
                IsSent = (bool)row["IsSent"]
            };
            return View(rem);
        }

        [HttpPost]
        public IActionResult Edit(Reminder rem)
        {
            var userId = GetUserId();
            string query = "UPDATE Reminders SET ClientName = @ClientName, Message = @Message, ReminderDate = @ReminderDate WHERE Id = @Id AND UserId = @UserId";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@ClientName", rem.ClientName),
                new SqlParameter("@Message", rem.Message),
                new SqlParameter("@ReminderDate", rem.ReminderDate),
                new SqlParameter("@Id", rem.Id),
                new SqlParameter("@UserId", userId)
            });

            _notificationService.AddNotification(null, $"Reminder for {rem.ClientName} updated by {User.Identity?.Name ?? "Unknown"}", "System", rem.Id, "Reminders", userId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            // Soft Delete
            string query = "UPDATE Reminders SET IsDeleted = 1 WHERE Id = @Id AND UserId = @UserId";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@UserId", userId)
            });

            _notificationService.AddNotification(null, $"Reminder (ID: {id}) moved to trash by {User.Identity?.Name ?? "Unknown"}", "System", id, "Reminders", userId);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Trash()
        {
            var userId = GetUserId();
            string query = "SELECT * FROM Reminders WHERE UserId = @UserId AND IsDeleted = 1 ORDER BY ReminderDate DESC";
            var dt = _db.ExecuteQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId)
            });

            var reminders = new List<Reminder>();
            foreach (DataRow row in dt.Rows)
            {
                reminders.Add(new Reminder
                {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    ClientName = row["ClientName"]?.ToString() ?? "",
                    Message = row["Message"]?.ToString() ?? "",
                    ReminderDate = (DateTime)row["ReminderDate"],
                    IsSent = (bool)row["IsSent"]
                });
            }

            return View(reminders);
        }

        [HttpPost]
        public IActionResult Restore(int id)
        {
            var userId = GetUserId();
            string query = "UPDATE Reminders SET IsDeleted = 0 WHERE Id = @Id AND UserId = @UserId";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@UserId", userId)
            });

            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult DeletePermanently(int id)
        {
            var userId = GetUserId();
            string query = "DELETE FROM Reminders WHERE Id = @Id AND UserId = @UserId";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@UserId", userId)
            });

            return RedirectToAction("Trash");
        }
    }
}
