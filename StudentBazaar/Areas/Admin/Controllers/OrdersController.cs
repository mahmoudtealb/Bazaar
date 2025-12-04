
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
            
            // Total Revenue: مجموع أسعار جميع المنتجات
            ViewBag.TotalRevenue = await _context.Products
                .SumAsync(p => (decimal?)p.Price) ?? 0;
            
            // Monthly Revenue: مجموع أسعار المنتجات المضافة في الشهر الحالي
            var currentYear = DateTime.UtcNow.Year;
            var currentMonth = DateTime.UtcNow.Month;
            ViewBag.MonthlyRevenue = await _context.Products
                .Where(p => p.CreatedAt.Year == currentYear && p.CreatedAt.Month == currentMonth)
                .SumAsync(p => (decimal?)p.Price) ?? 0;

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index");
            }

            // Validate status transition
            bool isValidTransition = order.Status switch
            {
                OrderStatus.Pending => newStatus == OrderStatus.Confirmed || newStatus == OrderStatus.Cancelled,
                OrderStatus.Confirmed => newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Cancelled,
                OrderStatus.Shipped => newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Cancelled,
                OrderStatus.Delivered => newStatus == OrderStatus.Completed,
                OrderStatus.Completed => false, // Cannot change completed orders
                OrderStatus.Cancelled => false, // Cannot change cancelled orders
                _ => false
            };

            if (!isValidTransition)
            {
                TempData["Error"] = $"Invalid status transition from {order.Status} to {newStatus}.";
                return RedirectToAction("Details", new { id });
            }

            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // If marking as Delivered or Completed, update product status
            if (newStatus == OrderStatus.Delivered || newStatus == OrderStatus.Completed)
            {
                if (order.OrderItems != null)
                {
                    foreach (var item in order.OrderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.IsSold = true;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            string statusMessage = newStatus switch
            {
                OrderStatus.Confirmed => "Order confirmed successfully.",
                OrderStatus.Shipped => "Order marked as shipped.",
                OrderStatus.Delivered => "Order marked as delivered. Customer can now rate the product.",
                OrderStatus.Completed => "Order completed successfully.",
                OrderStatus.Cancelled => "Order cancelled.",
                _ => "Order status updated."
            };

            TempData["Success"] = statusMessage;
            return RedirectToAction("Details", new { id });
        }
    }
}

