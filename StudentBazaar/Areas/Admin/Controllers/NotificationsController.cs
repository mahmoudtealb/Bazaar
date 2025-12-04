using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentBazaar.DataAccess;
using StudentBazaar.Entities.Models;

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Notifications/GetDashboardNotificationsCount
        [HttpGet]
        public async Task<IActionResult> GetDashboardNotificationsCount()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || userId <= 0)
                {
                    return Json(new { count = 0 });
                }
                
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .CountAsync();
                
                return Json(new { 
                    count = unreadNotifications
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting notifications count: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        // GET: /Admin/Notifications/GetNotifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || userId <= 0)
                {
                    return Json(new List<object>());
                }
                
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId)
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(10)
                    .Select(n => new
                    {
                        id = n.Id,
                        title = n.Title,
                        message = n.Message,
                        type = n.Type ?? "Info",
                        isRead = n.IsRead,
                        linkUrl = n.LinkUrl,
                        createdAt = n.CreatedAt
                    })
                    .ToListAsync();
                
                return Json(notifications);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting notifications: {ex.Message}");
                return Json(new List<object>());
            }
        }

        // POST: /Admin/Notifications/MarkAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || userId <= 0)
                {
                    return Json(new { success = false, error = "Invalid user" });
                }
                
                if (request == null || request.Id <= 0)
                {
                    return Json(new { success = false, error = "Invalid notification ID" });
                }
                
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId);
                
                if (notification == null)
                {
                    return Json(new { success = false, error = "Notification not found" });
                }

                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
                return Json(new { success = false, error = "An error occurred" });
            }
        }

        // POST: /Admin/Notifications/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId) || userId <= 0)
                {
                    return Json(new { success = false, error = "Invalid user" });
                }
                
                var unreadNotifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                if (unreadNotifications.Any())
                {
                    var now = DateTime.UtcNow;
                    foreach (var notification in unreadNotifications)
                    {
                        notification.IsRead = true;
                        notification.ReadAt = now;
                    }
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking all notifications as read: {ex.Message}");
                return Json(new { success = false, error = "An error occurred" });
            }
        }
    }

    public class MarkAsReadRequest
    {
        public int Id { get; set; }
    }
}

