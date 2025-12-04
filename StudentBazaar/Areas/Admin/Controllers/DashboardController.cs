

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<AdminHub> _hubContext;

        public DashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<AdminHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(),
                TotalSold = await _context.Products.Where(p => p.IsSold).CountAsync(),
                TotalMessages = await _context.ChatMessages.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                PendingReports = await _context.Reports.Where(r => !r.Resolved).CountAsync(),
                PendingVerifications = await _context.StudentVerifications.Where(v => !v.Approved).CountAsync(),
                TotalRevenue = await _context.Products
                    .SumAsync(p => p.Price),
                RecentProducts = await _context.Products
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync(),
                RecentOrders = await _context.Orders
    .Include(o => o.Buyer)
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.Product)
    .Include(o => o.Listing)
        .ThenInclude(l => l.Product)   // ✅ من غير شرط
    .OrderByDescending(o => o.OrderDate)
    .Take(5)
    .ToListAsync(),

                RecentReports = await _context.Reports
                    .Include(r => r.Reporter)
                    .Where(r => !r.Resolved)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .ToListAsync()
            };

            // Add new statistics for Universities, Colleges, and Categories
            ViewBag.TotalUniversities = await _context.Universities.CountAsync();
            ViewBag.TotalColleges = await _context.Colleges.CountAsync();
            ViewBag.TotalCategories = await _context.ProductCategories.CountAsync();

            // Monthly Revenue Data (last 12 months)
            var monthlyRevenue = new List<decimal>();
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            
            for (int i = 11; i >= 0; i--)
            {
                var targetDate = DateTime.UtcNow.AddMonths(-i);
                var revenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed &&
                               o.OrderDate.Year == targetDate.Year &&
                               o.OrderDate.Month == targetDate.Month)
                    .SumAsync(o => o.TotalAmount);
                monthlyRevenue.Add(revenue);
            }
            ViewBag.MonthlyRevenue = monthlyRevenue;

            // Top Categories Data (categories with product counts)
            var topCategories = await _context.ProductCategories
                .Include(c => c.Products)
                .Select(c => new
                {
                    Name = c.CategoryName,
                    Count = c.Products.Count
                })
                .Where(c => c.Count > 0)
                .OrderByDescending(c => c.Count)
                .Take(10)
                .ToListAsync();

            ViewBag.TopCategoriesLabels = topCategories.Select(c => c.Name).ToList();
            ViewBag.TopCategoriesData = topCategories.Select(c => c.Count).ToList();

            return View(model);
        }
    }
}

