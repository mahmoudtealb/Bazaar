
namespace StudentBazaar.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<College> _collegeRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            IGenericRepository<Product> productRepo, 
            IGenericRepository<College> collegeRepo,
            UserManager<ApplicationUser> userManager)
        {
            _productRepo = productRepo;
            _collegeRepo = collegeRepo;
            _userManager = userManager;
        }

        // الصفحة الرئيسية - Landing Page
        public async Task<IActionResult> Index(string? q = null, int? collegeId = null)
        {
            var products = await _productRepo.GetAllAsync(
                includeWord: "Category,Images,Ratings,Owner"
            );

            // Only show approved products to regular users (admins can see all)
            if (!(User.IsInRole("Admin")))
            {
                products = products.Where(p => p.IsApproved == true && !p.IsSold);
            }

            // Exclude current user's products if authenticated (Products for Buy)
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdStr = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    products = products.Where(p => p.SellerId == null || p.SellerId != userIdStr);
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.Trim();
                products = products.Where(p =>
                    p.Name.Contains(qLower, StringComparison.OrdinalIgnoreCase) ||
                    (p.Owner != null && p.Owner.College != null && p.Owner.College.CollegeName.Contains(qLower, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (collegeId.HasValue)
                products = products.Where(p => p.Owner != null && p.Owner.CollegeId == collegeId.Value);

            var colleges = await _collegeRepo.GetAllAsync();
            ViewBag.Colleges = colleges;
            ViewBag.CurrentQuery = q;
            ViewBag.CurrentCollegeId = collegeId;

            return View(products);
        }

        [HttpGet]
        public Task<IActionResult> Search(string? q = null, int? collegeId = null)
        {
            return Index(q, collegeId);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
