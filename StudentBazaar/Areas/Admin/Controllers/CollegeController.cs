

namespace StudentBazaar.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CollegeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CollegeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/College
        public async Task<IActionResult> Index(string? search, int? universityId, int page = 1, int pageSize = 20)
        {
            var query = _context.Colleges
                .Include(c => c.University)
                .Include(c => c.Users)
                .Include(c => c.Majors)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.CollegeName.Contains(search));
            }

            if (universityId.HasValue)
            {
                query = query.Where(c => c.UniversityId == universityId.Value);
            }

            var totalCount = await query.CountAsync();
            var colleges = await query
                .OrderBy(c => c.CollegeName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Universities = await _context.Universities.OrderBy(u => u.UniversityName).ToListAsync();
            ViewBag.SelectedUniversityId = universityId;
            ViewBag.TotalCount = totalCount;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(colleges);
        }

        // GET: Admin/College/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var college = await _context.Colleges
                .Include(c => c.University)
                .Include(c => c.Users)
                .Include(c => c.Majors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            return View(college);
        }

        // GET: Admin/College/Create
        public async Task<IActionResult> Create()
        {
            ViewData["UniversityId"] = new SelectList(await _context.Universities.OrderBy(u => u.UniversityName).ToListAsync(), "Id", "UniversityName");
            return View();
        }

        // POST: Admin/College/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CollegeName,UniversityId")] College college)
        {
            if (ModelState.IsValid)
            {
                college.CreatedAt = DateTime.UtcNow;
                _context.Add(college);
                await _context.SaveChangesAsync();
                TempData["Success"] = "College created successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["UniversityId"] = new SelectList(await _context.Universities.OrderBy(u => u.UniversityName).ToListAsync(), "Id", "UniversityName", college.UniversityId);
            return View(college);
        }

        // GET: Admin/College/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var college = await _context.Colleges.FindAsync(id);
            if (college == null)
            {
                return NotFound();
            }
            ViewData["UniversityId"] = new SelectList(await _context.Universities.OrderBy(u => u.UniversityName).ToListAsync(), "Id", "UniversityName", college.UniversityId);
            return View(college);
        }

        // POST: Admin/College/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CollegeName,UniversityId,CreatedAt")] College college)
        {
            if (id != college.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    college.UpdatedAt = DateTime.UtcNow;
                    _context.Update(college);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "College updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CollegeExists(college.Id))
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
            ViewData["UniversityId"] = new SelectList(await _context.Universities.OrderBy(u => u.UniversityName).ToListAsync(), "Id", "UniversityName", college.UniversityId);
            return View(college);
        }

        // GET: Admin/College/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var college = await _context.Colleges
                .Include(c => c.University)
                .Include(c => c.Users)
                .Include(c => c.Majors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (college == null)
            {
                return NotFound();
            }

            return View(college);
        }

        // POST: Admin/College/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var college = await _context.Colleges
                .Include(c => c.Users)
                .Include(c => c.Majors)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (college == null)
            {
                TempData["Error"] = "College not found.";
                return RedirectToAction(nameof(Index));
            }

            // Check if college has users
            if (college.Users != null && college.Users.Any())
            {
                TempData["Error"] = $"Cannot delete college '{college.CollegeName}' because it has {college.Users.Count} associated user(s).";
                return RedirectToAction(nameof(Index));
            }

            _context.Colleges.Remove(college);
            await _context.SaveChangesAsync();
            TempData["Success"] = "College deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool CollegeExists(int id)
        {
            return _context.Colleges.Any(e => e.Id == id);
        }
    }
}

