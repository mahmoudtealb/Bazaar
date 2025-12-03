

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(bool? resolved, string? targetType, int page = 1, int pageSize = 20)
        {
            var query = _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ResolvedBy)
                .AsQueryable();

            if (resolved.HasValue)
            {
                query = query.Where(r => r.Resolved == resolved.Value);
            }

            if (!string.IsNullOrEmpty(targetType))
            {
                query = query.Where(r => r.TargetType == targetType);
            }

            var totalCount = await query.CountAsync();
            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(reports);
        }

        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ResolvedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
                return NotFound();

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id, string resolution)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            report.Resolved = true;
            report.Resolution = resolution;
            report.ResolvedById = currentUser.Id;
            report.ResolvedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Report resolved.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(int reportId, int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.IsSuspended = true;
            await _context.SaveChangesAsync();

            var report = await _context.Reports.FindAsync(reportId);
            if (report != null)
            {
                report.Resolved = true;
                report.Resolution = "User blocked due to report.";
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "User blocked.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int reportId, int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            var report = await _context.Reports.FindAsync(reportId);
            if (report != null)
            {
                report.Resolved = true;
                report.Resolution = "Product removed due to report.";
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Product removed.";
            return RedirectToAction("Index");
        }
    }
}

