

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ChatMonitoringController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatMonitoringController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? productId, int? userId, string? search, int page = 1, int pageSize = 50)
        {
            var query = _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Product)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(m => m.ProductId == productId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(m => m.SenderId == userId.Value || m.ReceiverId == userId.Value);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(m => 
                    m.Content.Contains(search) ||
                    (m.Sender != null && m.Sender.FullName.Contains(search)) ||
                    (m.Receiver != null && m.Receiver.FullName.Contains(search)) ||
                    (m.Product != null && m.Product.Name.Contains(search))
                );
            }

            var totalCount = await query.CountAsync();
            var messages = await query
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.Search = search;

            return View(messages);
        }

        public async Task<IActionResult> Conversation(int productId, int userId1, int userId2)
        {
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Include(m => m.Product)
                .Where(m => m.ProductId == productId &&
                           ((m.SenderId == userId1 && m.ReceiverId == userId2) ||
                            (m.SenderId == userId2 && m.ReceiverId == userId1)))
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            ViewBag.ProductId = productId;
            ViewBag.UserId1 = userId1;
            ViewBag.UserId2 = userId2;

            return View(messages);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.IsSuspended = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "User blocked.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMessage(int id)
        {
            var message = await _context.ChatMessages.FindAsync(id);
            if (message == null)
                return NotFound();

            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Message removed.";
            return RedirectToAction("Index");
        }
    }
}

