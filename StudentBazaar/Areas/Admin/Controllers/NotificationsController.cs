

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

        // GET: /Admin/Notifications/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            // For admin: return total unread messages across the system
            var count = await _context.ChatMessages
                .CountAsync(m => !m.IsRead);
            
            return Json(new { count });
        }

        // GET: /Admin/Notifications/GetDashboardNotificationsCount
        [HttpGet]
        public async Task<IActionResult> GetDashboardNotificationsCount()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Count only unread notifications for this admin
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();
            
            return Json(new { 
                count = unreadNotifications
            });
        }

        // GET: /Admin/Notifications/GetNotifications
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            // Get recent notifications for this admin
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
    }
}

