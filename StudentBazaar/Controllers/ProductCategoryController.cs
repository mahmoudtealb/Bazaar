

namespace StudentBazaar.Web.Controllers
{
    public class ProductCategoryController : Controller
    {
        private readonly IGenericRepository<ProductCategory> _repo;

        public ProductCategoryController(IGenericRepository<ProductCategory> repo)
        {
            _repo = repo;
        }

        // GET: ProductCategory
        public async Task<IActionResult> Index()
        {
            var categories = await _repo.GetAllAsync(includeWord: "Products");
            return View(categories);
        }

        // GET: ProductCategory/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(c => c.Id == id, includeWord: "Products");
            if (entity == null)
                return NotFound();

            return View(entity);
        }

        // GET: ProductCategory/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ProductCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory entity)
        {
            if (!ModelState.IsValid)
                return View(entity);

            await _repo.AddAsync(entity);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: ProductCategory/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
                return NotFound();

            return View(existing);
        }

        // POST: ProductCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCategory entity)
        {
            if (!ModelState.IsValid)
                return View(entity);

            var existing = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (existing == null)
                return NotFound();

            existing.CategoryName = entity.CategoryName;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: ProductCategory/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (entity == null)
                return NotFound();

            return View(entity);
        }

        // POST: ProductCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(c => c.Id == id);
            if (entity == null)
                return NotFound();

            _repo.Remove(entity);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: Category/CreateAjax
        [HttpPost]
        [Route("Category/CreateAjax")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateAjax(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Category name is required." });
            }

            // Check if category with same name already exists
            var existingCategory = await _repo.GetFirstOrDefaultAsync(c => c.CategoryName.ToLower() == name.Trim().ToLower());
            if (existingCategory != null)
            {
                return Json(new { success = false, message = "A category with this name already exists." });
            }

            // Create new category
            var newCategory = new ProductCategory
            {
                CategoryName = name.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(newCategory);
            await _repo.SaveAsync();

            // Return JSON with id and name
            return Json(new { success = true, id = newCategory.Id, name = newCategory.CategoryName });
        }
    }
}
