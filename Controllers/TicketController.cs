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
    public class TicketController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.FileHelper _fileHelper;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public TicketController(SqlHelper db, Services.FileHelper fileHelper, Services.NotificationService notificationService, Services.PermissionService permissionService)
        {
            _db = db;
            _fileHelper = fileHelper;
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
            if (!_permissionService.HasPermission("Tickets")) return Forbid();
            string query = @"
                SELECT t.*, c.Name as ClientName, u.Username as CreatorName
                FROM Tickets t
                JOIN Clients c ON t.ClientId = c.Id
                JOIN Users u ON t.UserId = u.Id
                WHERE t.IsDeleted = 0
                ORDER BY CASE WHEN t.Status = 'Open' THEN 0 ELSE 1 END, t.CreatedAt DESC";
            
            var dt = _db.ExecuteQuery(query);
            var tickets = new List<Ticket>();
            foreach(DataRow row in dt.Rows)
            {
                tickets.Add(new Ticket {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    ClientId = (int)row["ClientId"],
                    ClientName = row["ClientName"]?.ToString() ?? "",
                    CreatorName = row["CreatorName"]?.ToString() ?? "",
                    Subject = row["Subject"]?.ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? "",
                    Priority = row["Priority"]?.ToString() ?? "",
                    Status = row["Status"]?.ToString() ?? "",
                    AttachmentPath = row["AttachmentPath"] != DBNull.Value ? row["AttachmentPath"].ToString() : null,
                    CreatedAt = (DateTime)row["CreatedAt"]
                });
            }
            return View(tickets);
        }

        public IActionResult Create()
        {
            var userId = GetUserId();
            string query = "SELECT Id, Name FROM Clients WHERE IsDeleted = 0 ORDER BY Name";
            var dt = _db.ExecuteQuery(query);
            ViewBag.Clients = dt.AsEnumerable().Select(r => new Client { Id = (int)r["Id"], Name = r["Name"].ToString() }).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int clientId, string subject, string description, string priority, IFormFile? attachment)
        {
            var userId = GetUserId();
            string attachmentPath = null;
            if(attachment != null)
            {
                attachmentPath = await _fileHelper.UploadFileAsync(attachment);
            }

            string query = @"
                INSERT INTO Tickets (UserId, ClientId, Subject, Description, Priority, Status, AttachmentPath)
                VALUES (@UserId, @ClientId, @Subject, @Desc, @Priority, 'Open', @Path)";
            
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ClientId", clientId),
                new SqlParameter("@Subject", subject),
                new SqlParameter("@Desc", description),
                new SqlParameter("@Priority", priority),
                new SqlParameter("@Path", attachmentPath ?? (object)DBNull.Value)
            });

            _notificationService.AddNotification(null, $"New Ticket '{subject}' created", "Ticket", null, "New Ticket", userId);

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            string query = "SELECT * FROM Tickets WHERE Id = @Id AND IsDeleted = 0";
            if (role != "Admin") query += " AND UserId = @UserId";

            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };
            if (role != "Admin") parameters.Add(new SqlParameter("@UserId", userId));

            var dt = _db.ExecuteQuery(query, parameters.ToArray());
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var ticket = new Ticket
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                ClientId = (int)row["ClientId"],
                Subject = row["Subject"]?.ToString() ?? "",
                Description = row["Description"]?.ToString() ?? "",
                Priority = row["Priority"]?.ToString() ?? "",
                Status = row["Status"]?.ToString() ?? "",
                AttachmentPath = row["AttachmentPath"] != DBNull.Value ? row["AttachmentPath"].ToString() : null,
                CreatedAt = (DateTime)row["CreatedAt"]
            };

            // Fetch Clients for the dropdown
            string clientQuery = "SELECT Id, Name FROM Clients WHERE IsDeleted = 0 ORDER BY Name";
            var cDt = _db.ExecuteQuery(clientQuery);
            ViewBag.Clients = cDt.AsEnumerable().Select(r => new Client { Id = (int)r["Id"], Name = r["Name"].ToString() }).ToList();

            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, int clientId, string subject, string description, string priority, IFormFile? attachment)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            // Verify ownership/admin
            var checkQuery = "SELECT UserId, AttachmentPath FROM Tickets WHERE Id = @Id AND IsDeleted = 0";
            var existing = _db.ExecuteQuery(checkQuery, new SqlParameter[] { new SqlParameter("@Id", id) });
            if (existing.Rows.Count == 0) return NotFound();
            
            var ownerId = (int)existing.Rows[0]["UserId"];
            var currentPath = existing.Rows[0]["AttachmentPath"]?.ToString();

            if (role != "Admin" && ownerId != userId) return Forbid();

            string attachmentPath = currentPath;
            if (attachment != null)
            {
                attachmentPath = await _fileHelper.UploadFileAsync(attachment);
            }

            string updateQuery = @"
                UPDATE Tickets SET 
                ClientId = @ClientId, 
                Subject = @Subject, 
                Description = @Desc, 
                Priority = @Priority, 
                AttachmentPath = @Path
                WHERE Id = @Id";

            _db.ExecuteNonQuery(updateQuery, new SqlParameter[] {
                new SqlParameter("@ClientId", clientId),
                new SqlParameter("@Subject", subject),
                new SqlParameter("@Desc", description),
                new SqlParameter("@Priority", priority),
                new SqlParameter("@Path", attachmentPath ?? (object)DBNull.Value),
                new SqlParameter("@Id", id)
            });

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateStatus(int id, string status)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            if (role != "Admin") return Forbid();

            _db.ExecuteNonQuery("UPDATE Tickets SET Status = @Status WHERE Id = @Id",
                new SqlParameter[] { 
                    new SqlParameter("@Status", status),
                    new SqlParameter("@Id", id)
                });
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            
            if(role != "Admin")
            {
                // Optionally return Forbidden or just redirect with a message (for now redirect)
                return RedirectToAction("Index"); // Basic protection, UI will also hide button
            }

            try
            {
                _db.ExecuteNonQuery("UPDATE Tickets SET IsDeleted = 1 WHERE Id = @Id", 
                    new SqlParameter[] { new SqlParameter("@Id", id) });
                TempData["Success"] = "Ticket deleted successfully.";
            }
            catch(Exception)
            {
                TempData["Error"] = "Error deleting ticket.";
            }
            return RedirectToAction("Index");
        }
        
        [HttpGet]
        public IActionResult Details(int id)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            // 1. Fetch Ticket
            // Access control: User sees own, Admin sees all.
            string ticketQuery = "SELECT * FROM Tickets WHERE Id = @Id";
            if(role != "Admin") ticketQuery += " AND UserId = @UserId";

            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };
            if (role != "Admin") parameters.Add(new SqlParameter("@UserId", userId));

            var dt = _db.ExecuteQuery(ticketQuery, parameters.ToArray());
            
            if(dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var ticket = new Ticket {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                ClientId = (int)row["ClientId"],
                Subject = row["Subject"]?.ToString() ?? "",
                Description = row["Description"]?.ToString() ?? "",
                Priority = row["Priority"]?.ToString() ?? "",
                Status = row["Status"]?.ToString() ?? "",
                AttachmentPath = row["AttachmentPath"] != DBNull.Value ? row["AttachmentPath"].ToString() : null,
                CreatedAt = (DateTime)row["CreatedAt"]
            };

            // 2. Fetch Comments (Joined with Users for Name/Avatar)
            string commentQuery = @"
                SELECT tc.*, u.Username, u.ProfileImagePath
                FROM TicketComments tc
                JOIN Users u ON tc.UserId = u.Id
                WHERE tc.TicketId = @Id
                ORDER BY tc.CreatedAt ASC";
            
            var cDt = _db.ExecuteQuery(commentQuery, new SqlParameter[] { new SqlParameter("@Id", id) });
            var comments = new List<TicketComment>();
            foreach(DataRow cRow in cDt.Rows)
            {
                comments.Add(new TicketComment {
                    Id = (int)cRow["Id"],
                    TicketId = (int)cRow["TicketId"],
                    UserId = (int)cRow["UserId"],
                    UserName = cRow["Username"]?.ToString() ?? "Unknown",
                    UserProfileImage = cRow["ProfileImagePath"] != DBNull.Value ? cRow["ProfileImagePath"].ToString() : null,
                    Comment = cRow["Comment"]?.ToString() ?? "",
                    AttachmentPath = cRow["AttachmentPath"] != DBNull.Value ? cRow["AttachmentPath"].ToString() : null,
                    CreatedAt = (DateTime)cRow["CreatedAt"]
                });
            }

            ViewBag.Comments = comments;
            
            // Client Name fetch if needed or other details
            var clientName = _db.ExecuteScalar("SELECT Name FROM Clients WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", ticket.ClientId) })?.ToString();
            ticket.ClientName = clientName ?? "Unknown";

            return View(ticket);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int ticketId, string message, IFormFile? attachment)
        {
            var userId = GetUserId();
            
            string attachmentPath = null;
            if(attachment != null)
            {
                attachmentPath = await _fileHelper.UploadFileAsync(attachment);
            }

            string query = @"
                INSERT INTO TicketComments (TicketId, UserId, Comment, AttachmentPath)
                VALUES (@TId, @UId, @Msg, @Path)";

            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@TId", ticketId),
                new SqlParameter("@UId", userId),
                new SqlParameter("@Msg", message ?? ""),
                new SqlParameter("@Path", attachmentPath ?? (object)DBNull.Value)
            });
            
             _db.ExecuteNonQuery("UPDATE Tickets SET Status = 'In Progress' WHERE Id = @Id AND Status = 'Open'", 
                 new SqlParameter[] { new SqlParameter("@Id", ticketId) });

            return RedirectToAction("Details", new { id = ticketId });
        }

        [HttpPost]
        public async Task<IActionResult> EditComment(int commentId, int ticketId, string message, IFormFile? attachment)
        {
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            // Check if user owns the comment or is Admin
            var existingComment = _db.ExecuteQuery("SELECT UserId, AttachmentPath FROM TicketComments WHERE Id = @Id", 
                new SqlParameter[] { new SqlParameter("@Id", commentId) });
            
            if (existingComment.Rows.Count == 0) return NotFound();
            var ownerId = (int)existingComment.Rows[0]["UserId"];
            var oldPath = existingComment.Rows[0]["AttachmentPath"]?.ToString();

            if (role != "Admin" && ownerId != userId) return Forbid();

            string attachmentPath = oldPath;
            if (attachment != null)
            {
                attachmentPath = await _fileHelper.UploadFileAsync(attachment);
            }

            string query = "UPDATE TicketComments SET Comment = @Msg, AttachmentPath = @Path WHERE Id = @Id";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Msg", message ?? ""),
                new SqlParameter("@Path", attachmentPath ?? (object)DBNull.Value),
                new SqlParameter("@Id", commentId)
            });

            return RedirectToAction("Details", new { id = ticketId });
        }

        [HttpGet]
        public IActionResult Download(String path)
        {
            if(string.IsNullOrEmpty(path)) return NotFound();
            var netPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(netPath)) return NotFound();
            var bytes = System.IO.File.ReadAllBytes(netPath);
            return File(bytes, "application/octet-stream", Path.GetFileName(netPath));
        }
    }
}
