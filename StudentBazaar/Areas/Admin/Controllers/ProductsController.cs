

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IHubContext<AdminHub> _hubContext;

        public ProductsController(
            ApplicationDbContext context,
            IGenericRepository<Product> productRepo,
            IHubContext<AdminHub> hubContext)
        {
            _context = context;
            _productRepo = productRepo;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index(string? search, int? categoryId, bool? isApproved, int page = 1, int pageSize = 20)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (isApproved.HasValue)
            {
                query = query.Where(p => p.IsApproved == isApproved.Value);
            }

            var totalCount = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(products);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Owner)
                .Include(p => p.Images)
                .Include(p => p.Listings)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? returnUrl = null)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index");
            }

            product.IsApproved = true;
            product.ApprovedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Mark related notifications as read for all admins
            var productLinkUrl = $"/Admin/Products/Details/{id}";
            var relatedNotifications = await _context.Notifications
                .Where(n => n.LinkUrl == productLinkUrl && !n.IsRead && n.Title == "New Product")
                .ToListAsync();

            if (relatedNotifications.Any())
            {
                foreach (var notification in relatedNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }
                await _context.SaveChangesAsync();
            }

            // Notify all admins that product was approved (this will trigger badge update)
            await _hubContext.Clients.Group("Admins").SendAsync("ProductApproved", id);
            
            // Also send AdminNotification to trigger immediate badge refresh
            await _hubContext.Clients.Group("Admins").SendAsync("AdminNotification", new { 
                title = "Product Approved", 
                message = $"Product '{product.Name}' has been approved", 
                type = "Success" 
            });
            TempData["Success"] = $"Product '{product.Name}' approved successfully. It is now visible to all users.";

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsApproved = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product rejected.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsSold(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsSold = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product marked as sold.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsUnsold(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsSold = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product marked as unsold.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakeFeatured(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            product.IsFeatured = !product.IsFeatured;
            await _context.SaveChangesAsync();

            TempData["Success"] = product.IsFeatured ? "Product featured." : "Product unfeatured.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product deleted.";
            return RedirectToAction("Index");
        }
    }
}

