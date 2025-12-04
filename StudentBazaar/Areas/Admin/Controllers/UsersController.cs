using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentBazaar.DataAccess;
using StudentBazaar.Entities.Models;

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(users);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.University)
                .Include(u => u.College)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = userRoles;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(int id, int? days)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsSuspended = true;
            if (days.HasValue)
            {
                user.SuspendedUntil = DateTime.UtcNow.AddDays(days.Value);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "User suspended.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsSuspended = false;
            user.SuspendedUntil = null;
            await _context.SaveChangesAsync();

            TempData["Success"] = "User activated.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTrustScore(int id, int score)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.TrustScore = Math.Max(0, Math.Min(100, score));
            await _context.SaveChangesAsync();

            TempData["Success"] = "Trust score updated.";
            return RedirectToAction("Details", new { id });
        }

        // ======================
        // Block User
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(int id, string? reason)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            // لا يمكن حظر الأدمن
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains("Admin"))
            {
                TempData["Error"] = "Cannot block an admin user.";
                return RedirectToAction("Details", new { id });
            }

            var currentAdminId = _userManager.GetUserId(User);
            if (int.TryParse(currentAdminId, out int adminId))
            {
                user.BlockedByUserId = adminId;
            }

            user.IsBlocked = true;
            user.BlockReason = reason ?? "Violation of terms and conditions";
            user.BlockedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.FullName}' has been blocked successfully.";
            return RedirectToAction("Details", new { id });
        }

        // ======================
        // Unblock User
        // ======================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.IsBlocked = false;
            user.BlockReason = null;
            user.BlockedAt = null;
            user.BlockedByUserId = null;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"User '{user.FullName}' has been unblocked successfully.";
            return RedirectToAction("Details", new { id });
        }

        public async Task<IActionResult> ExportCsv()
        {
            var users = await _context.Users.ToListAsync();
            var csv = "Id,Email,FullName,TrustScore,IsVerified,IsSuspended,IsBlocked,BlockReason,BlockedAt,CreatedAt\n";
            csv += string.Join("\n", users.Select(u => 
                $"{u.Id},{u.Email},{u.FullName},{u.TrustScore},{u.IsVerified},{u.IsSuspended},{u.IsBlocked},\"{u.BlockReason ?? ""}\",{u.BlockedAt?.ToString("yyyy-MM-dd HH:mm") ?? ""},{u.CreatedAt}"));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "users.csv");
        }
    }
}

