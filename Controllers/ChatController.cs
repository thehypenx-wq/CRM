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
    public class ChatController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.FileHelper _fileHelper;
        private readonly Services.NotificationService _notificationService;
        private readonly Services.PermissionService _permissionService;

        public ChatController(SqlHelper db, Services.FileHelper fileHelper, Services.NotificationService notificationService, Services.PermissionService permissionService)
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
            if (!_permissionService.HasPermission("GroupChat")) return Forbid();
            // Load Chat Groups
            var groups = new List<string> { "General" };
            string groupQs = "SELECT Name FROM ChatGroups";
            var dtGroups = _db.ExecuteQuery(groupQs);
            foreach(DataRow r in dtGroups.Rows) groups.Add(r["Name"]?.ToString() ?? "");

            ViewBag.Groups = groups;
            ViewBag.CurrentUserName = User.Identity?.Name ?? "Unknown";

            return View();
        }

        [HttpPost]
        public IActionResult CreateGroup(string name)
        {
            if(!string.IsNullOrWhiteSpace(name))
            {
                var userId = GetUserId();
                var exists = _db.ExecuteScalar("SELECT COUNT(1) FROM ChatGroups WHERE Name = @Name", new SqlParameter[] { new SqlParameter("@Name", name) });
                if((int)exists == 0)
                {
                    _db.ExecuteNonQuery("INSERT INTO ChatGroups (Name, CreatedBy) VALUES (@Name, @UserId)", 
                        new SqlParameter[] { new SqlParameter("@Name", name), new SqlParameter("@UserId", userId) });
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult GetMessages(string groupName)
        {
            if(string.IsNullOrEmpty(groupName)) groupName = "General";
            
            string query = "SELECT * FROM ChatMessages WHERE GroupName = @GroupName ORDER BY Timestamp";
            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@GroupName", groupName) });
            
            var messages = new List<object>();
            foreach(DataRow row in dt.Rows)
            {
                messages.Add(new {
                    user = row["UserName"]?.ToString() ?? "Unknown",
                    message = row["Message"]?.ToString() ?? "",
                    timestamp = (row["Timestamp"] != DBNull.Value ? (DateTime)row["Timestamp"] : DateTime.Now).ToString("t")
                });
            }
            return Json(messages);
        }

        [HttpPost]
        public IActionResult SendMessage(string message, string groupName)
        {
            var userId = GetUserId();
            var userName = User.Identity?.Name ?? "Unknown";
            
            if(!string.IsNullOrWhiteSpace(message))
            {
                string query = "INSERT INTO ChatMessages (UserId, UserName, Message, GroupName) VALUES (@UserId, @UserName, @Message, @GroupName)";
                _db.ExecuteNonQuery(query, new SqlParameter[] {
                    new SqlParameter("@UserId", userId),
                    new SqlParameter("@UserName", userName),
                    new SqlParameter("@Message", message),
                    new SqlParameter("@GroupName", groupName ?? "General")
                });

                // Add Activity Notification
                _notificationService.AddNotification(null, $"{userName} posted in {groupName}", "Chat", null, groupName, userId);
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UploadAttachment(IFormFile file)
        {
            if (file != null)
            {
                var path = await _fileHelper.UploadFileAsync(file);
                // We don't save to DB here as a separate attachment record linked to 'Chat' entity yet 
                // because we just return the URL to be sent as a message. 
                // But for audit, we CAN.
                
                return Json(new { url = path, filename = file.FileName });
            }
            return BadRequest();
        }
    }
}
