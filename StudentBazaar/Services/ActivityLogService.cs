

namespace StudentBazaar.Web.Services
{
    public interface IActivityLogService
    {
        Task LogActivityAsync(string action, int? userId, string? details = null, string? entityType = null, int? entityId = null);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActivityAsync(string action, int? userId, string? details = null, string? entityType = null, int? entityId = null)
        {
            var log = new ActivityLog
            {
                Action = action,
                UserId = userId,
                Details = details,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString()
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}

