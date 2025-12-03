
namespace StudentBazaar.Web.Controllers
{
    public class CollegeController : Controller
    {
        private readonly IGenericRepository<College> _repo;
        private readonly IGenericRepository<University> _universityRepo;

        public CollegeController(
            IGenericRepository<College> repo,
            IGenericRepository<University> universityRepo)
        {
            _repo = repo;
            _universityRepo = universityRepo;
        }

        // 📚 GET: College (Read - All)
        public async Task<IActionResult> Index()
        {
            var colleges = await _repo.GetAllAsync(includeWord: "University");
            return View(colleges);
        }

        // 🔎 GET: College/Details/5 (Read - Single)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return BadRequest();

            var college = await _repo.GetFirstOrDefaultAsync(
                c => c.Id == id, includeWord: "University,Users,Majors");

            if (college == null) return NotFound();

            return View(college);
        }

        // ➕ GET: College/Create
        public async Task<IActionResult> Create()
        {
            await PopulateUniversitiesDropDown();
            return View();
        }

        // 💾 POST: College/Create (Create)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(College college)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUniversitiesDropDown(college.UniversityId);
                return View(college);
            }

            await _repo.AddAsync(college);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // ✏️ GET: College/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            var college = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (college == null) return NotFound();

            await PopulateUniversitiesDropDown(college.UniversityId);
            return View(college);
        }

        // 🔄 POST: College/Edit/5 (Update)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, College college)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUniversitiesDropDown(college.UniversityId);
                return View(college);
            }

            var existing = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (existing == null) return NotFound();

            // Update tracked entity properties
            existing.CollegeName = college.CollegeName;
            existing.UniversityId = college.UniversityId;
            existing.UpdatedAt = DateTime.Now;

            // Note: The controller handles the update directly on the tracked entity,
            // then calls SaveAsync() from the generic repository.
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // 🗑️ GET: College/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            var college = await _repo.GetFirstOrDefaultAsync(
                c => c.Id == id, includeWord: "University");

            if (college == null) return NotFound();

            return View(college);
        }

        // ❌ POST: College/Delete/5 (Delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var college = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (college == null) return NotFound();

            _repo.Remove(college);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // 🛠️ Helper method to load universities for dropdown
        private async Task PopulateUniversitiesDropDown(int? selectedId = null)
        {
            var universities = await _universityRepo.GetAllAsync();
            ViewBag.Universities = new SelectList(
                universities ?? new List<University>(),
                "Id",
                "UniversityName",
                selectedId
            );
        }

        // POST: College/CreateAjax
        [HttpPost]
        [Route("College/CreateAjax")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateAjax(string name, int? universityId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "College name is required." });
            }

            // Use provided universityId or get the first university as default
            int finalUniversityId;
            if (universityId.HasValue && universityId.Value > 0)
            {
                var university = await _universityRepo.GetFirstOrDefaultAsync(u => u.Id == universityId.Value);
                if (university == null)
                {
                    return Json(new { success = false, message = "Invalid university selected." });
                }
                finalUniversityId = universityId.Value;
            }
            else
            {
                var firstUniversity = await _universityRepo.GetFirstOrDefaultAsync(u => true);
                if (firstUniversity == null)
                {
                    return Json(new { success = false, message = "No university found. Please create a university first." });
                }
                finalUniversityId = firstUniversity.Id;
            }

            // Check if college with same name already exists in the same university
            var existingCollege = await _repo.GetFirstOrDefaultAsync(c => 
                c.CollegeName.ToLower() == name.Trim().ToLower() && 
                c.UniversityId == finalUniversityId);
            if (existingCollege != null)
            {
                return Json(new { success = false, message = "A college with this name already exists in this university." });
            }

            // Create new college
            var newCollege = new College
            {
                CollegeName = name.Trim(),
                UniversityId = finalUniversityId,
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(newCollege);
            await _repo.SaveAsync();

            // Return JSON with id and name
            return Json(new { success = true, id = newCollege.Id, name = newCollege.CollegeName });
        }
    }
}
