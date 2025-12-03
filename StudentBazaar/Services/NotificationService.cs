

namespace StudentBazaar.Web.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, string? type = null, string? linkUrl = null);
        Task BroadcastToAdminsAsync(string title, string message, string? type = null, string? linkUrl = null);
        
        // Unread messages methods
        int GetUnreadMessagesCount(int userId);
        Task<int> GetUnreadMessagesCountAsync(int userId);
        Task MarkConversationAsReadAsync(int userId, int otherUserId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<AdminHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<AdminHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(int userId, string title, string message, string? type = null, string? linkUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type ?? "Info",
                LinkUrl = linkUrl
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task BroadcastToAdminsAsync(string title, string message, string? type = "Info", string? linkUrl = null)
        {
            var adminUsers = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")))
                .ToListAsync();

            foreach (var admin in adminUsers)
            {
                await SendNotificationAsync(admin.Id, title, message, type ?? "Info", linkUrl);
            }

            // Send real-time notification via SignalR
            await _hubContext.Clients.Group("Admins").SendAsync("AdminNotification", new { title, message, type = type ?? "Info", linkUrl });
            
            // Also send specific event notifications
            if (title == "New Product" && linkUrl != null)
            {
                // Extract product ID from link URL (format: /Admin/Products/Details/{id})
                var productIdMatch = System.Text.RegularExpressions.Regex.Match(linkUrl, @"/Admin/Products/Details/(\d+)");
                if (productIdMatch.Success && int.TryParse(productIdMatch.Groups[1].Value, out int productId))
                {
                    await _hubContext.Clients.Group("Admins").SendAsync("NewProduct", productId);
                }
            }
            else if (title == "New Order" && linkUrl != null)
            {
                // Extract order ID from link URL (format: /Admin/Orders/Details/{id})
                var orderIdMatch = System.Text.RegularExpressions.Regex.Match(linkUrl, @"/Admin/Orders/Details/(\d+)");
                if (orderIdMatch.Success && int.TryParse(orderIdMatch.Groups[1].Value, out int orderId))
                {
                    await _hubContext.Clients.Group("Admins").SendAsync("NewOrder", orderId);
                }
            }
        }

        // ============================
        // Unread Messages Methods
        // ============================

        public int GetUnreadMessagesCount(int userId)
        {
            return _context.ChatMessages
                .Count(m => m.ReceiverId == userId && !m.IsRead);
        }

        public async Task<int> GetUnreadMessagesCountAsync(int userId)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
        }

        public async Task MarkConversationAsReadAsync(int userId, int otherUserId)
        {
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && 
                           m.SenderId == otherUserId && 
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}

