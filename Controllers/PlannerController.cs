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
    public class PlannerController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.FileHelper _fileHelper;
        private readonly Services.NotificationService _notificationService;
        private readonly Services.PermissionService _permissionService;

        public PlannerController(SqlHelper db, Services.FileHelper fileHelper, Services.NotificationService notificationService, Services.PermissionService permissionService)
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
            if (!_permissionService.HasPermission("Planner")) return Forbid();
            string query = "SELECT * FROM AppEvents ORDER BY StartDate";
            var dt = _db.ExecuteQuery(query);

            var events = new List<AppEvent>();
            foreach (DataRow row in dt.Rows)
            {
                var evt = new AppEvent
                {
                    Id = (int)row["Id"],
                    UserId = (int)row["UserId"],
                    Title = row["Title"]?.ToString() ?? "",
                    Description = row["Description"]?.ToString() ?? "",
                    StartDate = (DateTime)row["StartDate"],
                    EndDate = (DateTime)row["EndDate"]
                };

                 // Fetch Attachments
                string attQuery = "SELECT * FROM Attachments WHERE EntityType = 'Planner' AND EntityId = @EntityId";
                var attDt = _db.ExecuteQuery(attQuery, new SqlParameter[] { new SqlParameter("@EntityId", evt.Id) });
                foreach(DataRow attRow in attDt.Rows)
                {
                    evt.Attachments.Add(new Attachment {
                        Id = (int)attRow["Id"],
                        FileName = attRow["FileName"]?.ToString() ?? "",
                        FilePath = attRow["FilePath"]?.ToString() ?? ""
                    });
                }
                events.Add(evt);
            }

            return View(events);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string title, string description, DateTime startDate, DateTime endDate, List<IFormFile> attachments)
        {
            if (string.IsNullOrWhiteSpace(title)) return RedirectToAction("Index");

            var userId = GetUserId();
            string query = @"
                INSERT INTO AppEvents (UserId, Title, Description, StartDate, EndDate) 
                VALUES (@UserId, @Title, @Description, @StartDate, @EndDate);
                SELECT CAST(SCOPE_IDENTITY() as int)";
            
            var eventId = (int)_db.ExecuteScalar(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@Title", title),
                new SqlParameter("@Description", description ?? (object)DBNull.Value),
                new SqlParameter("@StartDate", startDate),
                new SqlParameter("@EndDate", endDate)
            });

            // Add Activity
             _notificationService.AddNotification(null, $"New Event scheduled: {title} by {User.Identity?.Name ?? "Unknown"}", "System", (int)eventId, "AppEvents", userId);

            // Handle Attachments
            if (attachments != null && attachments.Count > 0)
            {
                foreach (var file in attachments)
                {
                    var path = await _fileHelper.UploadFileAsync(file);
                    if (path != null)
                    {
                        var attQuery = "INSERT INTO Attachments (EntityType, EntityId, FilePath, FileName, CreatedBy) VALUES ('Planner', @EntityId, @FilePath, @FileName, @CreatedBy)";
                        _db.ExecuteNonQuery(attQuery, new SqlParameter[] {
                            new SqlParameter("@EntityId", eventId),
                            new SqlParameter("@FilePath", path),
                            new SqlParameter("@FileName", file.FileName),
                            new SqlParameter("@CreatedBy", userId)
                        });
                    }
                }
            }

            return RedirectToAction("Index");
        }


        [HttpGet]
        public IActionResult Edit(int id)
        {
            var userId = GetUserId();
            var dt = _db.ExecuteQuery("SELECT * FROM AppEvents WHERE Id = @Id AND UserId = @UserId", 
                new SqlParameter[] { new SqlParameter("@Id", id), new SqlParameter("@UserId", userId) });
            
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var evt = new AppEvent
            {
                Id = (int)row["Id"],
                UserId = (int)row["UserId"],
                Title = row["Title"]?.ToString() ?? "",
                Description = row["Description"]?.ToString() ?? "",
                StartDate = (DateTime)row["StartDate"],
                EndDate = (DateTime)row["EndDate"]
            };

            // Fetch Attachments
            string attQuery = "SELECT * FROM Attachments WHERE EntityType = 'Planner' AND EntityId = @EntityId";
            var attDt = _db.ExecuteQuery(attQuery, new SqlParameter[] { new SqlParameter("@EntityId", evt.Id) });
            foreach(DataRow attRow in attDt.Rows)
            {
                evt.Attachments.Add(new Attachment {
                    Id = (int)attRow["Id"],
                    FileName = attRow["FileName"]?.ToString() ?? "",
                    FilePath = attRow["FilePath"]?.ToString() ?? ""
                });
            }

            return View(evt);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string title, string description, DateTime startDate, DateTime endDate, List<IFormFile> attachments)
        {
            if (string.IsNullOrWhiteSpace(title)) return RedirectToAction("Index");

            var userId = GetUserId();
            string query = "UPDATE AppEvents SET Title = @Title, Description = @Description, StartDate = @StartDate, EndDate = @EndDate WHERE Id = @Id AND UserId = @UserId";

            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Title", title),
                new SqlParameter("@Description", description ?? (object)DBNull.Value),
                new SqlParameter("@StartDate", startDate),
                new SqlParameter("@EndDate", endDate),
                new SqlParameter("@Id", id),
                new SqlParameter("@UserId", userId)
            });

            // Handle New Attachments
            if (attachments != null && attachments.Count > 0)
            {
                foreach (var file in attachments)
                {
                    var path = await _fileHelper.UploadFileAsync(file);
                    if (path != null)
                    {
                        var attQuery = "INSERT INTO Attachments (EntityType, EntityId, FilePath, FileName, CreatedBy) VALUES ('Planner', @EntityId, @FilePath, @FileName, @CreatedBy)";
                        _db.ExecuteNonQuery(attQuery, new SqlParameter[] {
                            new SqlParameter("@EntityId", id),
                            new SqlParameter("@FilePath", path),
                            new SqlParameter("@FileName", file.FileName),
                            new SqlParameter("@CreatedBy", userId)
                        });
                    }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var userId = GetUserId();
            string query = "DELETE FROM AppEvents WHERE Id = @Id AND UserId = @UserId";
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@Id", id),
                new SqlParameter("@UserId", userId)
            });

            _notificationService.AddNotification(null, $"Event (ID: {id}) deleted by {User.Identity?.Name ?? "Unknown"}", "System", id, "AppEvents", userId);

            return RedirectToAction("Index");
        }
    }
}
