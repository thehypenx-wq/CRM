using Microsoft.AspNetCore.Mvc;
using OfficeSuite.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace OfficeSuite.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost]
        public IActionResult MarkAllAsRead()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
            {
                _notificationService.MarkAllAsRead(userId);
                return Ok();
            }
            return BadRequest();
        }
    }
}
