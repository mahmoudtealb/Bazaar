using StudentBazaar.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentBazaar.Web.Models;

namespace StudentBazaar.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IGenericRepository<Product> _repo;
        private readonly IGenericRepository<ProductCategory> _categoryRepo;
        private readonly IGenericRepository<StudyYear> _studyYearRepo;
        private readonly IWebHostEnvironment _env;

        public ProductController(
            IGenericRepository<Product> repo,
            IGenericRepository<ProductCategory> categoryRepo,
            IGenericRepository<StudyYear> studyYearRepo,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _categoryRepo = categoryRepo;
            _studyYearRepo = studyYearRepo;
            _env = env;
        }

        // GET: Product
        public async Task<IActionResult> Index()
        {
            var products = await _repo.GetAllAsync(includeWord: "Category,StudyYear,Images,Listings,Ratings");
            return View(products);
        }

        // GET: Product/Create
        public async Task<IActionResult> Create()
        {
            var model = await PopulateCreateViewModel();
            return View(model);
        }

        // POST: Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model = await PopulateCreateViewModel(model.Product);
                return View(model);
            }

            var product = model.Product;
            product.Images = new List<ProductImage>();

            // مسار wwwroot/uploads
            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            // رفع الصور
            if (model.Files != null && model.Files.Any())
            {
                foreach (var file in model.Files)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadRoot, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    product.Images.Add(new ProductImage
                    {
                        ImageUrl = "/uploads/" + fileName,
                        IsMainImage = false
                    });
                }
            }

            await _repo.AddAsync(product);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(p => p.Id == id, includeWord: "Images");
            if (existing == null)
                return NotFound();

            var model = await PopulateCreateViewModel(existing);
            return View(model);
        }

        // POST: Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model = await PopulateCreateViewModel(model.Product);
                return View(model);
            }

            var existing = await _repo.GetFirstOrDefaultAsync(p => p.Id == id, includeWord: "Images");
            if (existing == null)
                return NotFound();

            existing.Name = model.Product.Name;
            existing.CategoryId = model.Product.CategoryId;
            existing.StudyYearId = model.Product.StudyYearId;
            existing.UpdatedAt = DateTime.Now;

            var uploadRoot = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            if (model.Files != null && model.Files.Any())
            {
                foreach (var file in model.Files)
                {
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var filePath = Path.Combine(uploadRoot, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    existing.Images.Add(new ProductImage
                    {
                        ProductId = existing.Id,
                        ImageUrl = "/uploads/" + fileName,
                        IsMainImage = false
                    });
                }
            }

            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Product/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(p => p.Id == id);
            if (entity == null)
                return NotFound();

            return View(entity);
        }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(p => p.Id == id);
            if (entity == null)
                return NotFound();

            _repo.Remove(entity);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // ======= HELPERS =======
        private async Task<ProductCreateViewModel> PopulateCreateViewModel(Product? product = null)
        {
            var categories = (await _categoryRepo.GetAllAsync())
                .Select(c => new SelectListItem(c.CategoryName, c.Id.ToString()));

            var studyYears = (await _studyYearRepo.GetAllAsync())
                .Select(s => new SelectListItem(s.YearName, s.Id.ToString()));

            return new ProductCreateViewModel
            {
                Product = product ?? new Product(),
                Categories = categories,
                StudyYears = studyYears
            };
        }
    }
}
