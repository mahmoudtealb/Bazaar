

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UniversityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UniversityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/University
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
        {
            var query = _context.Universities
                .Include(u => u.Colleges)
                .Include(u => u.Users)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.UniversityName.Contains(search) || u.Location.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var universities = await query
                .OrderBy(u => u.UniversityName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(universities);
        }

        // GET: Admin/University/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var university = await _context.Universities
                .Include(u => u.Colleges)
                .Include(u => u.Users)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (university == null)
            {
                return NotFound();
            }

            return View(university);
        }

        // GET: Admin/University/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/University/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UniversityName,Location")] University university)
        {
            if (ModelState.IsValid)
            {
                university.CreatedAt = DateTime.UtcNow;
                _context.Add(university);
                await _context.SaveChangesAsync();
                TempData["Success"] = "University created successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(university);
        }

        // GET: Admin/University/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var university = await _context.Universities.FindAsync(id);
            if (university == null)
            {
                return NotFound();
            }
            return View(university);
        }

        // POST: Admin/University/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UniversityName,Location,CreatedAt")] University university)
        {
            if (id != university.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    university.UpdatedAt = DateTime.UtcNow;
                    _context.Update(university);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "University updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UniversityExists(university.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(university);
        }

        // GET: Admin/University/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var university = await _context.Universities
                .Include(u => u.Colleges)
                .Include(u => u.Users)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (university == null)
            {
                return NotFound();
            }

            return View(university);
        }

        // POST: Admin/University/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var university = await _context.Universities
                .Include(u => u.Colleges)
                .Include(u => u.Users)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (university == null)
            {
                TempData["Error"] = "University not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if university has users
            if (university.Users != null && university.Users.Any())
            {
                TempData["Error"] = $"Cannot delete university '{university.UniversityName}' because it has {university.Users.Count} associated user(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.Universities.Remove(university);
            await _context.SaveChangesAsync();
            TempData["Success"] = "University deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool UniversityExists(int id)
        {
            return _context.Universities.Any(e => e.Id == id);
        }
    }
}

