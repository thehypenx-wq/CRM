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
    public class BulkEmailController : Controller
    {
         private readonly SqlHelper _db;
         private readonly Services.FileHelper _fileHelper;

        private readonly Services.PermissionService _permissionService;

        public BulkEmailController(SqlHelper db, Services.FileHelper fileHelper, Services.PermissionService permissionService)
        {
            _db = db;
            _fileHelper = fileHelper;
            _permissionService = permissionService;
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && !string.IsNullOrEmpty(claim.Value) ? int.Parse(claim.Value) : 0;
        }

        public IActionResult Index(string to = null, string subject = null)
        {
            if (!_permissionService.HasPermission("BulkEmail")) return Forbid();
            ViewBag.To = to;
            ViewBag.Subject = subject;
            // History
             var userId = GetUserId();
            var dt = _db.ExecuteQuery("SELECT * FROM EmailHistory WHERE UserId = @UserId ORDER BY SentAt DESC", 
                new SqlParameter[] { new SqlParameter("@UserId", userId) });
            
            // Clients for selection
            var clientDt = _db.ExecuteQuery("SELECT Id, Name, Email FROM Clients WHERE UserId = @UserId AND IsDeleted = 0 AND Email IS NOT NULL", 
                new SqlParameter[] { new SqlParameter("@UserId", userId) });
            var clients = new List<Client>();
            foreach (DataRow row in clientDt.Rows)
            {
                clients.Add(new Client { Id = (int)row["Id"], Name = row["Name"]?.ToString() ?? "", Email = row["Email"]?.ToString() ?? "" });
            }
            ViewBag.Clients = clients;

            return View(dt);
        }

        [HttpPost]
        public async Task<IActionResult> Send(string toDetails, string subject, string body, IFormFile attachment)
        {
            if (string.IsNullOrWhiteSpace(toDetails))
            {
                TempData["Error"] = "Recipient list cannot be empty.";
                return RedirectToAction("Index");
            }

            // Save to History
            var userId = GetUserId();
            string attPath = null;
            
            if(attachment != null)
            {
                attPath = await _fileHelper.UploadFileAsync(attachment);
            }

            string query = @"INSERT INTO EmailHistory (UserId, ToEmail, Subject, Body, AttachmentPath) 
                             VALUES (@UserId, @ToEmail, @Subject, @Body, @AttPath)";
            
            _db.ExecuteNonQuery(query, new SqlParameter[] {
                new SqlParameter("@UserId", userId),
                new SqlParameter("@ToEmail", toDetails),
                new SqlParameter("@Subject", subject ?? "(No Subject)"),
                new SqlParameter("@Body", body ?? ""),
                new SqlParameter("@AttPath", attPath ?? (object)DBNull.Value)
            });

            TempData["Message"] = "Emails queued successfully!";
            return RedirectToAction("Index");
        }
    }
}
