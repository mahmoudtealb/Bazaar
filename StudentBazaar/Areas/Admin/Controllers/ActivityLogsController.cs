

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ActivityLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? action, int? userId, int page = 1, int pageSize = 50)
        {
            var query = _context.ActivityLogs
                .Include(a => a.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action.Contains(action));
            }

            if (userId.HasValue)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(logs);
        }
    }
}

