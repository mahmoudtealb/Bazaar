

namespace StudentBazaar.Web.Controllers
{
    public class UniversityController : Controller
    {
        private readonly IGenericRepository<University> _repo;

        public UniversityController(IGenericRepository<University> repo)
        {
            _repo = repo;
        }

        // GET: University
        public async Task<IActionResult> Index()
        {
            // جلب كل الجامعات مع Colleges و Users لو موجود
            var universities = await _repo.GetAllAsync(includeWord: "Colleges,Users");
            return View(universities);
        }

        // GET: University/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var university = await _repo.GetFirstOrDefaultAsync(
                u => u.Id == id,
                includeWord: "Colleges,Users"
            );

            if (university == null) return NotFound();

            return View(university);
        }

        // GET: University/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: University/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(University university)
        {
            if (!ModelState.IsValid)
            {
                return View(university);
            }

            await _repo.AddAsync(university);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: University/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var university = await _repo.GetFirstOrDefaultAsync(u => u.Id == id);
            if (university == null) return NotFound();

            return View(university);
        }

        // POST: University/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, University university)
        {
            if (!ModelState.IsValid)
            {
                return View(university);
            }

            var existing = await _repo.GetFirstOrDefaultAsync(u => u.Id == id);
            if (existing == null) return NotFound();

            existing.UniversityName = university.UniversityName;
            existing.UpdatedAt = DateTime.Now;

           
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: University/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var university = await _repo.GetFirstOrDefaultAsync(
                u => u.Id == id,
                includeWord: "Colleges,Users"
            );

            if (university == null) return NotFound();

            return View(university);
        }

        // POST: University/Delete/5
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var university = await _repo.GetFirstOrDefaultAsync(u => u.Id == id);
            if (university == null) return NotFound();

            _repo.Remove(university);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: University/CreateAjax
        [HttpPost]
        [Route("University/CreateAjax")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateAjax(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "University name is required." });
            }

            // Check if university with same name already exists
            var existingUniversity = await _repo.GetFirstOrDefaultAsync(u => u.UniversityName.ToLower() == name.Trim().ToLower());
            if (existingUniversity != null)
            {
                return Json(new { success = false, message = "A university with this name already exists." });
            }

            // Create new university
            var newUniversity = new University
            {
                UniversityName = name.Trim(),
                Location = "Not specified", // Default location since it's required
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(newUniversity);
            await _repo.SaveAsync();

            // Return JSON with id and name
            return Json(new { success = true, id = newUniversity.Id, name = newUniversity.UniversityName });
        }
    }
}
