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
    public class TodoController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.FileHelper _fileHelper;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public TodoController(SqlHelper db, Services.FileHelper fileHelper, Services.NotificationService notificationService, Services.PermissionService permissionService)
        {
            _db = db;
            _fileHelper = fileHelper;
            _notificationService = notificationService;
            _permissionService = permissionService;
        }

        private Todo MapTodoFromRow(DataRow row)
        {
            return new Todo
            {
                Id = row["Id"] != DBNull.Value ? (int)row["Id"] : 0,
                UserId = row["UserId"] != DBNull.Value ? (int)row["UserId"] : 0,
                Title = row["Title"]?.ToString() ?? "",
                IsCompleted = row["IsCompleted"] != DBNull.Value && (bool)row["IsCompleted"],
                DueDate = row["DueDate"] != DBNull.Value ? (DateTime)row["DueDate"] : (DateTime?)null,
                CreatedAt = row["CreatedAt"] != DBNull.Value ? (DateTime)row["CreatedAt"] : DateTime.Now,
                ProjectId = row["ProjectId"] != DBNull.Value ? (int)row["ProjectId"] : (int?)null,
                ProjectName = row["ProjectName"]?.ToString() ?? "",
                AssignedToUserId = row["AssignedToUserId"] != DBNull.Value ? (int)row["AssignedToUserId"] : (int?)null,
                AssignedToName = row["AssignedToName"]?.ToString() ?? "",
                Remark = row["Remark"]?.ToString() ?? "",
                StatusId = row["StatusId"] != DBNull.Value ? (int)row["StatusId"] : (int?)null,
                StatusName = row["StatusName"]?.ToString() ?? "",
                StatusColor = row["ColorCode"]?.ToString() ?? "",
                CreatedBy = row["UserId"] != DBNull.Value ? (int)row["UserId"] : 0,
                CreatedByName = row["CreatedByName"]?.ToString() ?? ""
            };
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
        }

        public IActionResult Index()
        {
            if (!_permissionService.HasPermission("TodoList")) return Forbid();
            var userId = GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";

            // Fetch Todos with Project Name, Assigned User Name, and Status
            string query = @"
                SELECT t.*, p.Name as ProjectName, u.Username as AssignedToName, s.StatusName, s.ColorCode,
                       c.Username as CreatedByName
                FROM Todos t
                LEFT JOIN Projects p ON t.ProjectId = p.Id
                LEFT JOIN Users u ON t.AssignedToUserId = u.Id
                LEFT JOIN TodoStatuses s ON t.StatusId = s.Id
                LEFT JOIN Users c ON t.UserId = c.Id
                WHERE t.IsDeleted = 0";
            
            query += " ORDER BY CASE WHEN s.StatusName = 'Completed' THEN 1 ELSE 0 END, t.DueDate";

            var dt = _db.ExecuteQuery(query);

            var todos = new List<Todo>();
            foreach (DataRow row in dt.Rows)
            {
                var todo = MapTodoFromRow(row);
                
                // Fetch Attachments for each Todo
                string attQuery = "SELECT * FROM Attachments WHERE EntityType = 'Todo' AND EntityId = @EntityId";
                var attDt = _db.ExecuteQuery(attQuery, new SqlParameter[] { new SqlParameter("@EntityId", todo.Id) });
                foreach(DataRow attRow in attDt.Rows)
                {
                    todo.Attachments.Add(new Attachment {
                        Id = attRow["Id"] != DBNull.Value ? (int)attRow["Id"] : 0,
                        FileName = attRow["FileName"]?.ToString() ?? "",
                        FilePath = attRow["FilePath"]?.ToString() ?? ""
                    });
                }
                // Fetch Comments
                string comQuery = @"
                    SELECT c.*, u.Username as UserName 
                    FROM TodoComments c 
                    JOIN Users u ON c.UserId = u.Id 
                    WHERE c.TodoId = @TodoId 
                    ORDER BY c.CreatedAt DESC";
                var comDt = _db.ExecuteQuery(comQuery, new SqlParameter[] { new SqlParameter("@TodoId", todo.Id) });
                foreach(DataRow comRow in comDt.Rows)
                {
                    todo.Comments.Add(new TodoComment {
                        Id = (int)comRow["Id"],
                        TodoId = (int)comRow["TodoId"],
                        UserId = (int)comRow["UserId"],
                        UserName = comRow["UserName"]?.ToString() ?? "Unknown",
                        Comment = comRow["Comment"]?.ToString() ?? "",
                        AttachmentPath = comRow["AttachmentPath"] != DBNull.Value ? comRow["AttachmentPath"].ToString() : null,
                        CreatedAt = (DateTime)comRow["CreatedAt"]
                    });
                }
                todos.Add(todo);
            }

            // Populate ViewBags
            PopulateViewBags(userId);

            return View(todos);
        }

        private void PopulateViewBags(int userId)
        {
             // Projects
            var projDt = _db.ExecuteQuery("SELECT Id, Name FROM Projects WHERE IsDeleted = 0 ORDER BY Name");
            ViewBag.Projects = projDt.AsEnumerable().Select(r => new Project { Id = (int)r["Id"], Name = r["Name"]?.ToString() ?? "" }).ToList();

            // Users (For assignment - currently all users)
            var userDt = _db.ExecuteQuery("SELECT Id, Username FROM Users");
            ViewBag.Users = userDt.AsEnumerable().Select(r => new User { Id = (int)r["Id"], Username = r["Username"]?.ToString() ?? "" }).ToList();

            // Statuses
            var statDt = _db.ExecuteQuery("SELECT Id, StatusName, ColorCode FROM TodoStatuses");
            ViewBag.Statuses = statDt.AsEnumerable().Select(r => new { Id = (int)r["Id"], Name = r["StatusName"].ToString(), Color = r["ColorCode"].ToString() }).ToList();
        }

        // ... (Create method unchanged) ...

        [HttpPost]
        public async Task<IActionResult> Create(string title, DateTime? dueDate, int? projectId, int? assignedToUserId, int? statusId, string? remark, List<IFormFile>? attachments)
        {
             // ... Same logic as before, just ensuring PopulateViewBags is called if we return View (redirecting to Index anyway)
             if (string.IsNullOrWhiteSpace(title)) return RedirectToAction("Index");
            
            var userId = GetUserId();
            var username = User.Identity?.Name ?? "Unknown"; 
            
            // Auto-append creator comment
            if(!string.IsNullOrWhiteSpace(remark))
            {
                remark += $"  -- [Created by {username} on {DateTime.Now:g}]";
            }
            else
            {
                remark = $"[Created by {username} on {DateTime.Now:g}]";
            }

             // ... (Insert Logic) ...
            string query = @"
                INSERT INTO Todos (UserId, Title, IsCompleted, DueDate, ProjectId, AssignedToUserId, Remark, StatusId, IsDeleted) 
                VALUES (@UserId, @Title, 0, @DueDate, @ProjectId, @AssignedToUserId, @Remark, @StatusId, 0);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            var todoId = (int)_db.ExecuteScalar(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Title", title),
                new SqlParameter("@DueDate", dueDate ?? (object)DBNull.Value),
                new SqlParameter("@ProjectId", projectId ?? (object)DBNull.Value),
                new SqlParameter("@AssignedToUserId", assignedToUserId ?? (object)DBNull.Value),
                new SqlParameter("@Remark", remark ?? (object)DBNull.Value),
                new SqlParameter("@StatusId", statusId ?? 1) 
            });

            _notificationService.AddNotification(null, $"New task '{title}' created by {username}", "System", todoId, "Todo", userId);

            // ... (Notifications & Attachments Logic same as before) ...
            if(assignedToUserId.HasValue)
            {
                _notificationService.AddNotification(assignedToUserId.Value, $"You were assigned a new task: {title}", "Todo", todoId, "Todo", userId);
            }
            _notificationService.AddNotification(null, $"New Task created: {title}", "Todo", todoId, "Todo", userId);

             if (attachments != null && attachments.Count > 0)
            {
                foreach (var file in attachments)
                {
                    var path = await _fileHelper.UploadFileAsync(file);
                    if (path != null)
                    {
                        var attQuery = "INSERT INTO Attachments (EntityType, EntityId, FilePath, FileName, CreatedBy) VALUES ('Todo', @EntityId, @FilePath, @FileName, @CreatedBy)";
                        _db.ExecuteNonQuery(attQuery, new SqlParameter[] {
                            new SqlParameter("@EntityId", todoId),
                            new SqlParameter("@FilePath", path),
                            new SqlParameter("@FileName", file.FileName),
                            new SqlParameter("@CreatedBy", userId)
                        });
                    }
                }
            }

            return RedirectToAction("Index");
        }


        // ... (UpdateStatus Unchanged) ...
         [HttpPost]
        public IActionResult UpdateStatus(int id, int statusId)
        {
            var userId = GetUserId();
            // Also update IsCompleted flag for compat
            string query = @"
                UPDATE Todos 
                SET StatusId = @StatusId,
                    IsCompleted = CASE WHEN @StatusId = (SELECT Id FROM TodoStatuses WHERE StatusName='Completed') THEN 1 ELSE 0 END
                WHERE Id = @Id AND UserId = @UserId";

            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@StatusId", statusId),
                new SqlParameter("@UserId", userId)
            });

             // Add Activity
            _notificationService.AddNotification(null, $"Task status updated by {User.Identity?.Name ?? "Unknown"}", "System", id, "Todo", userId);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateStatusAjax(int id, int statusId)
        {
            try
            {
                var userId = GetUserId();
                // Also update IsCompleted flag for compat
                string query = @"
                    UPDATE Todos 
                    SET StatusId = @StatusId,
                        IsCompleted = CASE WHEN @StatusId = (SELECT Id FROM TodoStatuses WHERE StatusName='Completed') THEN 1 ELSE 0 END
                    WHERE Id = @Id";

                _db.ExecuteNonQuery(query, new SqlParameter[] {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@StatusId", statusId)
                });

                // Add Activity
                _notificationService.AddNotification(null, $"Task status updated via Board by {User.Identity?.Name ?? "Unknown"}", "System", id, "Todo", userId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }



        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            // Soft Delete
            string query = "UPDATE Todos SET IsDeleted = 1 WHERE Id = @Id"; 
            try
            {
                _db.ExecuteNonQuery(query, new SqlParameter[] {
                    new SqlParameter("@Id", id)
                });
                _notificationService.AddNotification(null, $"Task (ID: {id}) moved to trash by {User.Identity?.Name ?? "Unknown"}", "System", id, "Todo", userId);
                TempData["Success"] = "Task moved to trash.";
            }
            catch(Exception)
            {
                TempData["Error"] = "Error moving task to trash.";
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = GetUserId();
            string query = @"SELECT * FROM Todos WHERE Id = @Id AND IsDeleted = 0";
            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@Id", id) });

            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var todo = new Todo
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                Title = row["Title"]?.ToString() ?? "",
                IsCompleted = (bool)row["IsCompleted"],
                DueDate = row["DueDate"] != DBNull.Value ? (DateTime)row["DueDate"] : (DateTime?)null,
                CreatedAt = (DateTime)row["CreatedAt"],
                ProjectId = row["ProjectId"] != DBNull.Value ? (int)row["ProjectId"] : (int?)null,
                AssignedToUserId = row["AssignedToUserId"] != DBNull.Value ? (int)row["AssignedToUserId"] : (int?)null,
                Remark = row["Remark"]?.ToString() ?? "",
                StatusId = row["StatusId"] != DBNull.Value ? (int)row["StatusId"] : (int?)null
            };

            // Fetch Attachments
            string attQuery = "SELECT * FROM Attachments WHERE EntityType = 'Todo' AND EntityId = @EntityId";
            var attDt = _db.ExecuteQuery(attQuery, new SqlParameter[] { new SqlParameter("@EntityId", todo.Id) });
            foreach (DataRow attRow in attDt.Rows)
            {
                todo.Attachments.Add(new Attachment
                {
                    Id = (int)attRow["Id"],
                    FileName = attRow["FileName"]?.ToString() ?? "",
                    FilePath = attRow["FilePath"]?.ToString() ?? ""
                });
            }

            PopulateViewBags(userId);
            return View(todo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string title, DateTime? dueDate, int? projectId, int? assignedToUserId, int? statusId, string newComment, List<IFormFile> attachments)
        {
             var userId = GetUserId();
             var username = User.Identity?.Name ?? "Unknown";

             // 1. Fetch existing remark
             string getRemarkQuery = "SELECT Remark FROM Todos WHERE Id = @Id";
             var currentRemark = _db.ExecuteScalar(getRemarkQuery, new SqlParameter[] { new SqlParameter("@Id", id) })?.ToString() ?? "";

             // 2. Append new comment if exists
             string updatedRemark = currentRemark;
             if (!string.IsNullOrWhiteSpace(newComment))
             {
                 string timestamp = DateTime.Now.ToString("g");
                 string commentBlock = $"\n\n--- Comment by {username} on {timestamp} ---\n{newComment}";
                 updatedRemark += commentBlock;
             }

             // 3. Update Todo
             string query = @"
                UPDATE Todos
                SET Title = @Title,
                    DueDate = @DueDate,
                    ProjectId = @ProjectId,
                    AssignedToUserId = @AssignedToUserId,
                    StatusId = @StatusId,
                    Remark = @Remark,
                    IsCompleted = CASE WHEN @StatusId = (SELECT Id FROM TodoStatuses WHERE StatusName='Completed') THEN 1 ELSE 0 END
                WHERE Id = @Id";

             _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@Title", title),
                new SqlParameter("@DueDate", dueDate ?? (object)DBNull.Value),
                new SqlParameter("@ProjectId", projectId ?? (object)DBNull.Value),
                new SqlParameter("@AssignedToUserId", assignedToUserId ?? (object)DBNull.Value),
                new SqlParameter("@StatusId", statusId ?? (object)DBNull.Value),
                new SqlParameter("@Remark", updatedRemark)
             });

             _notificationService.AddNotification(null, $"Task '{title}' updated by {username}", "System", id, "Todo", userId);

             // 4. Handle New Attachments
             if (attachments != null && attachments.Count > 0)
             {
                 foreach (var file in attachments)
                 {
                     var path = await _fileHelper.UploadFileAsync(file);
                     if (path != null)
                     {
                         var attQuery = "INSERT INTO Attachments (EntityType, EntityId, FilePath, FileName, CreatedBy) VALUES ('Todo', @EntityId, @FilePath, @FileName, @CreatedBy)";
                         _db.ExecuteNonQuery(attQuery, new SqlParameter[] {
                            new SqlParameter("@EntityId", id),
                            new SqlParameter("@FilePath", path),
                            new SqlParameter("@FileName", file.FileName),
                            new SqlParameter("@CreatedBy", userId)
                        });
                     }
                 }
             }

             _notificationService.AddNotification(null, $"Task updated: {title}", "Todo", id, "Todo", userId);
             return RedirectToAction("Index");
        }

        public IActionResult Trash()
        {
            var userId = GetUserId();
            string query = @"
                SELECT t.*, p.Name as ProjectName, u.Username as AssignedToName, s.StatusName, s.ColorCode,
                       c.Username as CreatedByName
                FROM Todos t
                LEFT JOIN Projects p ON t.ProjectId = p.Id
                LEFT JOIN Users u ON t.AssignedToUserId = u.Id
                LEFT JOIN TodoStatuses s ON t.StatusId = s.Id
                LEFT JOIN Users c ON t.UserId = c.Id
                WHERE (t.UserId = @UserId OR t.AssignedToUserId = @UserId) AND t.IsDeleted = 1
                ORDER BY t.DueDate DESC";

            var dt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@UserId", userId) });
             var todos = new List<Todo>();
            foreach (DataRow row in dt.Rows)
            {
                var todo = MapTodoFromRow(row);
                todos.Add(todo);
            }

            return View(todos);
        }

        [HttpPost]
        public IActionResult Restore(int id)
        {
             var userId = GetUserId();
            string query = "UPDATE Todos SET IsDeleted = 0 WHERE Id = @Id AND UserId = @UserId";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@UserId", userId)
            });
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult HardDelete(int id)
        {
             var userId = GetUserId();
             try
             {
                 // Delete Attachments first
                 _db.ExecuteNonQuery("DELETE FROM Attachments WHERE EntityType='Todo' AND EntityId = " + id);
                
                 string query = "DELETE FROM Todos WHERE Id = @Id AND UserId = @UserId";
                 _db.ExecuteNonQuery(query, new SqlParameter[] {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@UserId", userId)
                 });
                 TempData["Success"] = "Task permanently deleted.";
             }
             catch(SqlException ex)
             {
                 if(ex.Number == 547) TempData["Error"] = "Cannot delete task. Dependent records exist.";
                 else TempData["Error"] = "Error deleting task.";
             }
            return RedirectToAction("Trash");
        }
            [HttpPost]
        public async Task<IActionResult> AddComment(int todoId, string comment, IFormFile? attachment)
        {
            if(!string.IsNullOrWhiteSpace(comment) || attachment != null)
            {
                var userId = GetUserId();
                 string attachmentPath = null;
                 if(attachment != null)
                 {
                     attachmentPath = await _fileHelper.UploadFileAsync(attachment);
                 }

                _db.ExecuteNonQuery("INSERT INTO TodoComments (TodoId, UserId, Comment, AttachmentPath) VALUES (@TodoId, @UserId, @Comment, @AttachmentPath)", 
                    new SqlParameter[] { 
                        new SqlParameter("@TodoId", todoId),
                        new SqlParameter("@UserId", userId),
                        new SqlParameter("@Comment", comment ?? ""),
                        new SqlParameter("@AttachmentPath", attachmentPath ?? (object)DBNull.Value)
                    });
                
                // Add Activity Notification
                var userName = User.Identity?.Name ?? "Unknown";
                _notificationService.AddNotification(null, $"{userName} commented on a task", "Todo", todoId, "Task Comment", userId);
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult Download(String path)
        {
            var netPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(netPath)) return NotFound();
            var bytes = System.IO.File.ReadAllBytes(netPath);
            return File(bytes, "application/octet-stream", Path.GetFileName(netPath));
        }
    }
}
