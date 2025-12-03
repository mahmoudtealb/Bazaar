
namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 20)
        {
            var query = _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Listing)
                    .ThenInclude(l => l != null ? l.Product : null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<OrderStatus>(status, out var orderStatus))
                {
                    query = query.Where(o => o.Status == orderStatus);
                }
            }

            if (fromDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= toDate.Value);
            }

            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed)
                .SumAsync(o => o.TotalAmount);
            ViewBag.MonthlyRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed && o.OrderDate.Month == DateTime.UtcNow.Month)
                .SumAsync(o => o.TotalAmount);

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Buyer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Listing)
                    .ThenInclude(l => l != null ? l.Product : null)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }
    }
}

