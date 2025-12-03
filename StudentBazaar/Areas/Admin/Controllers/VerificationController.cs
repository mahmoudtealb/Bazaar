

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VerificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public VerificationController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(bool? approved, int page = 1, int pageSize = 20)
        {
            var query = _context.StudentVerifications
                .Include(v => v.User)
                .Include(v => v.ApprovedBy)
                .AsQueryable();

            if (approved.HasValue)
            {
                query = query.Where(v => v.Approved == approved.Value);
            }

            var totalCount = await query.CountAsync();
            var verifications = await query
                .OrderByDescending(v => v.RequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(verifications);
        }

        public async Task<IActionResult> Details(int id)
        {
            var verification = await _context.StudentVerifications
                .Include(v => v.User)
                .Include(v => v.ApprovedBy)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (verification == null)
                return NotFound();

            return View(verification);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var verification = await _context.StudentVerifications
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (verification == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return Unauthorized();

            verification.Approved = true;
            verification.ApprovedAt = DateTime.UtcNow;
            verification.ApprovedById = currentUser.Id;

            verification.User.IsVerified = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Verification approved.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string rejectionReason)
        {
            var verification = await _context.StudentVerifications.FindAsync(id);
            if (verification == null)
                return NotFound();

            verification.Approved = false;
            verification.RejectionReason = rejectionReason;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Verification rejected.";
            return RedirectToAction("Index");
        }
    }
}

