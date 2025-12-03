using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentBazaar.Web.Services;
using System.Security.Claims;

namespace StudentBazaar.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET: /Notifications/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { count = 0 });
            }

            var count = await _notificationService.GetUnreadMessagesCountAsync(userId);
            return Json(new { count });
        }

        // POST: /Notifications/MarkConversationAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkConversationAsRead([FromBody] MarkConversationReadRequest request)
        {
            if (request == null || request.OtherUserId <= 0)
            {
                return Json(new { success = false, error = "Invalid request." });
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return Json(new { success = false, error = "User not authenticated." });
            }

            await _notificationService.MarkConversationAsReadAsync(userId, request.OtherUserId);
            return Json(new { success = true });
        }
    }

    public class MarkConversationReadRequest
    {
        public int OtherUserId { get; set; }
    }
}

