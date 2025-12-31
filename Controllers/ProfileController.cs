using Microsoft.AspNetCore.Mvc;
using OfficeSuite.Models;
using Microsoft.Data.SqlClient;
using OfficeSuite.Services;
using OfficeSuite.Data;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace OfficeSuite.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly SqlHelper _db;
        private readonly Services.NotificationService _notificationService;
        private readonly Services.FileHelper _fileHelper;

        public ProfileController(SqlHelper db, Services.NotificationService notificationService, Services.FileHelper fileHelper)
        {
            _db = db;
            _notificationService = notificationService;
            _fileHelper = fileHelper;
        }

        public IActionResult Index()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
            if (userId == 0) return RedirectToAction("Login", "Account");

            var dt = _db.ExecuteQuery("SELECT * FROM Users WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", userId) });
            
            if (dt.Rows.Count == 0) return NotFound();

            var row = dt.Rows[0];
            var user = new User
            {
                Id = (int)row["Id"],
                Username = row["Username"]?.ToString() ?? "",
                Email = row["Email"]?.ToString() ?? "",
                Role = row["Role"]?.ToString() ?? "",
                ProfileImagePath = row["ProfileImagePath"] != DBNull.Value ? row["ProfileImagePath"].ToString() : null,
                IsActive = row["IsActive"] != DBNull.Value && (bool)row["IsActive"],
                CreatedAt = row["CreatedAt"] != DBNull.Value ? (DateTime)row["CreatedAt"] : DateTime.Now
            };
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile profileImage)
        {
             var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (profileImage != null)
            {
                string path = await _fileHelper.UploadFileAsync(profileImage);
                _db.ExecuteNonQuery("UPDATE Users SET ProfileImagePath = @Path WHERE Id = @Id", new SqlParameter[] {
                    new SqlParameter("@Path", path),
                    new SqlParameter("@Id", userId)
                });
                 _notificationService.AddNotification(null, $"Profile picture updated for user {User.Identity?.Name ?? "Unknown"}", "System", userId, "Users", null);
                 TempData["Success"] = "Profile picture updated successfully.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdatePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                return RedirectToAction("Index");
            }

            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
            if (userId == 0) return RedirectToAction("Login", "Account");

            var dt = _db.ExecuteQuery("SELECT PasswordHash FROM Users WHERE Id = @Id", new SqlParameter[] { new SqlParameter("@Id", userId) });
            
            if (dt.Rows.Count > 0 && dt.Rows[0]["PasswordHash"].ToString() == currentPassword)
            {
                _db.ExecuteNonQuery("UPDATE Users SET PasswordHash = @NewPass WHERE Id = @Id", new SqlParameter[] {
                    new SqlParameter("@NewPass", newPassword),
                    new SqlParameter("@Id", userId)
                });
                _notificationService.AddNotification(null, $"Password updated for user {User.Identity?.Name ?? "Unknown"}", "System", userId, "Users", null);
                TempData["Success"] = "Password updated successfully.";
            }
            else
            {
                TempData["Error"] = "Incorrect current password.";
            }

            return RedirectToAction("Index");
        }
    }
}
