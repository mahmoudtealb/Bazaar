using StudentBazaar.DataAccess;
using StudentBazaar.Entities.Models;
using StudentBazaar.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace StudentBazaar.Web.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(int userId, string title, string message, string? type = null, string? linkUrl = null);
        Task BroadcastToAdminsAsync(string title, string message, string? type = null, string? linkUrl = null);
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
                LinkUrl = linkUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send SignalR notification
            await _hubContext.Clients.User(userId.ToString()).SendAsync("AdminNotification", new
            {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                linkUrl = notification.LinkUrl,
                createdAt = notification.CreatedAt
            });
        }

        public async Task BroadcastToAdminsAsync(string title, string message, string? type = "Info", string? linkUrl = null)
        {
            var adminUsers = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && 
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Admin")))
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var admin in adminUsers)
            {
                var notification = new Notification
                {
                    UserId = admin.Id,
                    Title = title,
                    Message = message,
                    Type = type,
                    LinkUrl = linkUrl,
                    CreatedAt = now
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            // Broadcast to all admins via SignalR
            await _hubContext.Clients.Group("Admins").SendAsync("AdminNotification", new
            {
                title = title,
                message = message,
                type = type,
                linkUrl = linkUrl
            });
        }
    }
}

