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
    public class ProjectController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.NotificationService _notificationService;

        private readonly Services.PermissionService _permissionService;

        public ProjectController(SqlHelper db, Services.NotificationService notificationService, Services.PermissionService permissionService)
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
            if (!_permissionService.HasPermission("Projects")) return Forbid();
            string query = @"
                SELECT p.*, u.Username as CreatedByUsername 
                FROM Projects p 
                JOIN Users u ON p.UserId = u.Id 
                WHERE p.IsDeleted = 0
                ORDER BY p.Name";

            var dt = _db.ExecuteQuery(query);

            var projects = new List<Project>();
            foreach (DataRow row in dt.Rows)
            {
                projects.Add(new Project
                {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    Name = row["Name"]?.ToString() ?? "",
                    ProjectCode = row["ProjectCode"]?.ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? "",
                    CreatedByUsername = row["CreatedByUsername"]?.ToString() ?? "System",
                    CreatedAt = (DateTime)row["CreatedAt"]
                });
            }

            return View(projects);
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            string query = "SELECT * FROM Projects WHERE Id = @Id AND IsDeleted = 0";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            var dt = _db.ExecuteQuery(query, parameters.ToArray());
            
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var project = new Project
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                Name = row["Name"]?.ToString() ?? "",
                ProjectCode = row["ProjectCode"]?.ToString() ?? "",
                Description = row["Description"]?.ToString() ?? "",
                CreatedAt = (DateTime)row["CreatedAt"]
            };

            return View(project);
        }

        [HttpPost]
        public IActionResult Edit(Project project)
        {
            if (string.IsNullOrWhiteSpace(project.Name)) return View(project);

             // Permission check
            var checkDt = _db.ExecuteQuery("SELECT UserId FROM Projects WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", project.Id) });
            if (checkDt.Rows.Count == 0) return NotFound();

            string query = "UPDATE Projects SET Name = @Name, ProjectCode = @ProjectCode, Description = @Description WHERE Id = @Id";
            
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Name", project.Name),
                new SqlParameter("@ProjectCode", project.ProjectCode ?? (object)DBNull.Value),
                new SqlParameter("@Description", project.Description ?? (object)DBNull.Value),
                new SqlParameter("@Id", project.Id)
            });

            var userId = GetUserId();
            _notificationService.AddNotification(null, $"Project '{project.Name}' updated by {User.Identity?.Name ?? "Unknown"}", "System", project.Id, "Project", userId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Create(string name, string projectCode, string description)
        {
            if(string.IsNullOrWhiteSpace(name)) return RedirectToAction("Index");

            var userId = GetUserId();
            string query = "INSERT INTO Projects (UserId, Name, ProjectCode, Description) VALUES (@UserId, @Name, @ProjectCode, @Description)";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Name", name),
                new SqlParameter("@ProjectCode", projectCode ?? (object)DBNull.Value),
                new SqlParameter("@Description", description ?? (object)DBNull.Value)
            });

            _notificationService.AddNotification(null, $"New project '{name}' created by {User.Identity?.Name ?? "Unknown"}", "System", null, "Project", userId);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            string query = "UPDATE Projects SET IsDeleted = 1 WHERE Id = @Id";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            _db.ExecuteNonQuery(query, parameters.ToArray());

            var userId = GetUserId();
            _notificationService.AddNotification(null, $"Project (ID: {id}) moved to trash by {User.Identity?.Name ?? "Unknown"}", "System", id, "Project", userId);

            return RedirectToAction("Index");
        }

        public IActionResult Trash()
        {
            string query = "SELECT * FROM Projects WHERE IsDeleted = 1";
            var dt = _db.ExecuteQuery(query);
            var projects = new List<Project>();
            foreach (DataRow row in dt.Rows)
            {
                projects.Add(new Project
                {
                    Name = row["Name"]?.ToString() ?? "",
                    ProjectCode = row["ProjectCode"]?.ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? "",
                    CreatedAt = (DateTime)row["CreatedAt"]
                });
            }
            return View(projects);
        }

        [HttpPost]
        public IActionResult Restore(int id)
        {
            string query = "UPDATE Projects SET IsDeleted = 0 WHERE Id = @Id";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            _db.ExecuteNonQuery(query, parameters.ToArray());
            return RedirectToAction("Trash");
        }

        [HttpPost]
        public IActionResult HardDelete(int id)
        {
            string query = "DELETE FROM Projects WHERE Id = @Id";
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", id) };

            try
            {
                _db.ExecuteNonQuery(query, parameters.ToArray());
                TempData["Success"] = "Project permanently deleted.";
            }
            catch(SqlException ex)
            {
                if(ex.Number == 547) TempData["Error"] = "Cannot delete project. It contains active tasks or data.";
                else TempData["Error"] = "Error deleting project.";
            }

            return RedirectToAction("Trash");
        }

        public IActionResult Board(int id)
        {
            var userId = GetUserId();
            // 1. Fetch Project
            var dt = _db.ExecuteQuery("SELECT * FROM Projects WHERE Id = @Id AND IsDeleted = 0", new SqlParameter[] { new SqlParameter("@Id", id) });
            if (dt.Rows.Count == 0) return NotFound();
            var row = dt.Rows[0];
            var project = new Project { Id = (int)row["Id"], Name = row["Name"].ToString() };

            // 2. Fetch Tasks (Todos) for this project
            string query = @"
                SELECT t.*, u.Username as AssignedToName, s.StatusName, s.ColorCode
                FROM Todos t
                LEFT JOIN Users u ON t.AssignedToUserId = u.Id
                LEFT JOIN TodoStatuses s ON t.StatusId = s.Id
                WHERE t.ProjectId = @ProjectId AND t.IsDeleted = 0
                ORDER BY t.CreatedAt DESC";
            
            var todoDt = _db.ExecuteQuery(query, new SqlParameter[] { new SqlParameter("@ProjectId", id) });
            var todos = new List<Todo>();
            foreach(DataRow r in todoDt.Rows)
            {
                todos.Add(new Todo {
                    Id = (int)r["Id"],
                    Title = r["Title"].ToString(),
                    IsCompleted = (bool)r["IsCompleted"],
                    StatusId = r["StatusId"] != DBNull.Value ? (int)r["StatusId"] : 1,
                    StatusName = r["StatusName"]?.ToString() ?? "Untitled",
                    StatusColor = r["ColorCode"]?.ToString() ?? "#ccc",
                    AssignedToName = r["AssignedToName"]?.ToString() ?? "Unassigned"
                });
            }

            // 3. Fetch Statuses for the board columns
            var statDt = _db.ExecuteQuery("SELECT Id, StatusName, ColorCode FROM TodoStatuses ORDER BY Id");
            ViewBag.Statuses = statDt.AsEnumerable().Select(r => new { 
                Id = (int)r["Id"], 
                Name = r["StatusName"].ToString(), 
                Color = r["ColorCode"].ToString() 
            }).ToList();

            ViewBag.Todos = todos;
            return View(project);
        }
    }
}
