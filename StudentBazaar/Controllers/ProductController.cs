using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StudentBazaar.Web.Models;
using StudentBazaar.Web.Repositories;
using StudentBazaar.Web.ViewModels;

namespace StudentBazaar.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IGenericRepository<Product> _repo;
        private readonly IGenericRepository<ProductCategory> _categoryRepo;
        private readonly IGenericRepository<ProductImage> _imageRepo;
        private readonly IWebHostEnvironment _env;

        public ProductController(
            IGenericRepository<Product> repo,
            IGenericRepository<ProductCategory> categoryRepo,
            IGenericRepository<ProductImage> imageRepo,
            IWebHostEnvironment env)
        {
            _repo = repo;
            _categoryRepo = categoryRepo;
            _imageRepo = imageRepo;
            _env = env;
        }

        // INDEX
        public async Task<IActionResult> Index()
        {
            var products = await _repo.GetAllAsync(includeWord: "Category,Images,Listings,Ratings");
            return View(products);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var product = await _repo.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeWord: "Category,Images,Listings,Ratings"
            );

            if (product == null)
                return NotFound();

            return View(product);
        }

        // CREATE GET
        public async Task<IActionResult> Create()
        {
            var model = await PopulateCreateViewModel();
            return View(model);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (!model.Product.CategoryId.HasValue || model.Product.CategoryId <= 0)
                ModelState.AddModelError("Product.CategoryId", "Please select a category.");

            if (!ModelState.IsValid)
            {
                model = await PopulateCreateViewModel(model.Product);
                return View(model);
            }

            var product = model.Product;
            product.CreatedAt = DateTime.UtcNow;

            await _repo.AddAsync(product);
            await _repo.SaveAsync();

            await HandleUploadedFiles(product, model.Files);

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(p => p.Id == id, includeWord: "Category,Images");
            if (existing == null) return NotFound();

            var model = await PopulateCreateViewModel(existing);
            return View(model);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateViewModel model)
        {
            if (model == null || model.Product == null)
                return BadRequest();

            if (!model.Product.CategoryId.HasValue || model.Product.CategoryId <= 0)
                ModelState.AddModelError("Product.CategoryId", "Please select a category.");

            if (model.Product.Price <= 0)
                ModelState.AddModelError("Product.Price", "Price must be greater than 0.");

            if (!ModelState.IsValid)
            {
                model = await PopulateCreateViewModel(model.Product);
                return View(model);
            }

            var existing = await _repo.GetFirstOrDefaultAsync(p => p.Id == id, includeWord: "Category,Images");
            if (existing == null) return NotFound();

            existing.Name = model.Product.Name;
            existing.CategoryId = model.Product.CategoryId;
            existing.Price = model.Product.Price;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repo.SaveAsync();

            await HandleUploadedFiles(existing, model.Files);

            return RedirectToAction(nameof(Index));
        }

        // DELETE GET
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _repo.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeWord: "Category,Images"
            );

            if (product == null)
                return NotFound();

            return View(product);
        }

        // DELETE POST CONFIRMED
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _repo.GetFirstOrDefaultAsync(p => p.Id == id, includeWord: "Images");
            if (product == null) return NotFound();

            if (product.Images != null && product.Images.Any())
            {
                foreach (var img in product.Images.ToList())
                {
                    var filePath = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    _imageRepo.Remove(img);
                }

                await _imageRepo.SaveAsync();
            }

            _repo.Remove(product);
            await _repo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // DELETE IMAGE AJAX FIXED
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage([FromBody] ImageRequest request)
        {
            var img = await _imageRepo.GetFirstOrDefaultAsync(i => i.Id == request.ImageId);
            if (img == null) return NotFound();

            var filePath = Path.Combine(_env.WebRootPath, img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _imageRepo.Remove(img);
            await _imageRepo.SaveAsync();

            return Ok();
        }

        // SET MAIN IMAGE AJAX FIXED
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMainImage([FromBody] ImageRequest request)
        {
            var img = await _imageRepo.GetFirstOrDefaultAsync(i => i.Id == request.ImageId);
            if (img == null) return NotFound();

            var allImages = await _imageRepo.GetAllAsync(i => i.ProductId == img.ProductId);

            foreach (var im in allImages)
                im.IsMainImage = false;

            img.IsMainImage = true;

            await _imageRepo.SaveAsync();

            return Ok();
        }

        // FILE UPLOAD HANDLER
        private async Task HandleUploadedFiles(Product product, IEnumerable<IFormFile>? files)
        {
            if (files == null || !files.Any()) return;

            var uploadRoot = Path.Combine(_env.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadRoot))
                Directory.CreateDirectory(uploadRoot);

            bool isFirst = product.Images == null || !product.Images.Any();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                {
                    ModelState.AddModelError("Files", $"File {file.FileName} is not a valid image format.");
                    continue;
                }

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadRoot, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await file.CopyToAsync(stream);

                var img = new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = "/images/products/" + fileName,
                    IsMainImage = isFirst
                };

                await _imageRepo.AddAsync(img);
                isFirst = false;
            }

            await _imageRepo.SaveAsync();
        }

        // POPULATE VM
        private async Task<ProductCreateViewModel> PopulateCreateViewModel(Product? product = null)
        {
            var allCategories = await _categoryRepo.GetAllAsync();
            var categories = allCategories
                .Select(c => new SelectListItem
                {
                    Text = c.CategoryName,
                    Value = c.Id.ToString(),
                    Selected = product != null && product.CategoryId.HasValue && product.CategoryId == c.Id
                })
                .ToList();

            return new ProductCreateViewModel
            {
                Product = product ?? new Product(),
                Categories = categories
            };
        }
    }
}
