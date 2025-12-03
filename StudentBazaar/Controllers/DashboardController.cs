
namespace StudentBazaar.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            IGenericRepository<Product> productRepo,
            IGenericRepository<Order> orderRepo,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get real data from database
            var allProducts = await _productRepo.GetAllAsync();
            var allOrders = await _orderRepo.GetAllAsync();
            var productsCount = allProducts.Count();
            var ordersCount = allOrders.Count();
            var usersCount = _context.Users.Count();
            
            // Calculate total revenue from delivered orders
            var deliveredOrders = allOrders.Where(
                o => o.Status == OrderStatus.Delivered
            );
            var revenue = deliveredOrders.Any() ? deliveredOrders.Sum(o => o.TotalAmount) : 0m;

            ViewBag.UsersCount = usersCount;
            ViewBag.ProductsCount = productsCount;
            ViewBag.OrdersCount = ordersCount;
            ViewBag.Revenue = revenue;

            return View();
        }
    }
}
