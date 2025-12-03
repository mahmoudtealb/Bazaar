

namespace StudentBazaar.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IGenericRepository<Product> _repo;
        private readonly IGenericRepository<ProductCategory> _categoryRepo;
        private readonly IGenericRepository<ProductImage> _imageRepo;
        private readonly IGenericRepository<College> _collegeRepo;
        private readonly IGenericRepository<University> _universityRepo;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(
            IGenericRepository<Product> repo,
            IGenericRepository<ProductCategory> categoryRepo,
            IGenericRepository<ProductImage> imageRepo,
            IGenericRepository<College> collegeRepo,
            IGenericRepository<University> universityRepo,
            IWebHostEnvironment env,
            UserManager<ApplicationUser> userManager
        )
        {
            _repo = repo;
            _categoryRepo = categoryRepo;
            _imageRepo = imageRepo;
            _collegeRepo = collegeRepo;
            _universityRepo = universityRepo;
            _env = env;
            _userManager = userManager;
        }

        // ============================
        // Helpers
        // ============================

        private int GetCurrentUserId()
        {
            var idStr = _userManager.GetUserId(User);
            return int.Parse(idStr!);
        }

        private bool CanManage(Product product)
        {
            // Admin يقدر يدير أي منتج
            if (User.IsInRole("Admin")) return true;

            if (!(User.Identity?.IsAuthenticated ?? false))
                return false;

            var currentUserId = GetCurrentUserId();
            return product.OwnerId.HasValue && product.OwnerId.Value == currentUserId;
        }

        // ============================
        // PUBLIC: LIST & DETAILS
        // ============================

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? q = null, int? collegeId = null)
        {
            var products = await _repo.GetAllAsync(
                includeWord: "Category,Images,Listings,Ratings,Owner"
            );

            // Only show approved products to regular users (admins can see all)
            if (!(User.IsInRole("Admin")))
            {
                products = products.Where(p => p.IsApproved == true && !p.IsSold);
            }

            // Exclude current user's products if authenticated (Products for Buy)
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var userIdStr = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    products = products.Where(p => p.SellerId == null || p.SellerId != userIdStr);
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.Trim();
                products = products.Where(p =>
                    p.Name.Contains(qLower, StringComparison.OrdinalIgnoreCase) ||
                    (p.Owner != null && p.Owner.College != null && p.Owner.College.CollegeName.Contains(qLower, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (collegeId.HasValue)
                products = products.Where(p => p.Owner != null && p.Owner.CollegeId == collegeId.Value);

            var colleges = await _collegeRepo.GetAllAsync();
            ViewBag.Colleges = colleges;
            ViewBag.CurrentQuery = q;
            ViewBag.CurrentCollegeId = collegeId;

            return View(products);
        }

        [HttpGet]
        public Task<IActionResult> Search(string? q = null, int? collegeId = null)
        {
            return Index(q, collegeId);
        }

        // ============================
        // MY PRODUCTS (Seller's own products)
        // ============================

        [Authorize]
        public async Task<IActionResult> MyProducts(string? q = null)
        {
            var userIdStr = _userManager.GetUserId(User);
            
            var products = await _repo.GetAllAsync(
                includeWord: "Category,Images,Listings,Ratings,Owner"
            );

            // Show only current user's products (Products for Sale)
            if (!string.IsNullOrEmpty(userIdStr))
            {
                products = products.Where(p => p.SellerId == userIdStr);
            }
            else
            {
                products = products.Where(p => false); // No products if user ID is null
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var qLower = q.Trim();
                products = products.Where(p =>
                    p.Name.Contains(qLower, StringComparison.OrdinalIgnoreCase)
                );
            }

            ViewBag.CurrentQuery = q;
            return View(products);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _repo.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeWord: "Category,Images,Listings,Ratings,Owner"
            );

            if (product == null)
                return NotFound();

            // Check if user can view this product
            // Only approved products are visible to regular users (unless they're the owner or admin)
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var currentUserId = GetCurrentUserId();
                var isOwner = product.OwnerId.HasValue && product.OwnerId.Value == currentUserId;
                var isAdmin = User.IsInRole("Admin");
                
                // If product is not approved and user is not owner/admin, return NotFound
                if (!product.IsApproved && !isOwner && !isAdmin)
                {
                    return NotFound();
                }
                
                ViewBag.CanManage = (isAdmin || isOwner);
            }
            else
            {
                // Anonymous users can only see approved products
                if (!product.IsApproved)
                {
                    return NotFound();
                }
                ViewBag.CanManage = false;
            }

            // التحقق من وجود Listing متاح (Available)
            var hasAvailableListing = product.Listings != null && 
                                     product.Listings.Any(l => l.Status == ListingStatus.Available);
            ViewBag.HasAvailableListing = hasAvailableListing;

            return View(product);
        }

        // ============================
        // CREATE
        // ============================

        [Authorize]
        public async Task<IActionResult> Create()
        {
            var model = await PopulateCreateViewModel();
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel model)
        {
            if (model.Product == null || model.Product.CategoryId == null || model.Product.CategoryId <= 0)
                ModelState.AddModelError("Product.CategoryId", "Please select a category.");

            if (!ModelState.IsValid)
            {
                model = await PopulateCreateViewModel(model.Product);
                return View(model);
            }

            var product = model.Product;
            product.CreatedAt = DateTime.UtcNow;

            // ربط المنتج بصاحب الحساب الحالي
            var userId = GetCurrentUserId();
            product.OwnerId = userId;
            
            // Save SellerId as string
            var userIdStr = _userManager.GetUserId(User);
            product.SellerId = userIdStr;

            // Set product as Pending (requires admin approval)
            product.IsApproved = false;
            product.IsSold = false;
            product.IsFeatured = false;

            await _repo.AddAsync(product);
            await _repo.SaveAsync();

            await HandleUploadedFiles(product, model.Files);

            // Send notification to admins about new product
            try
            {
                var notificationService = HttpContext.RequestServices.GetRequiredService<StudentBazaar.Web.Services.INotificationService>();
                await notificationService.BroadcastToAdminsAsync(
                    "New Product",
                    "New Product",
                    "Info",
                    $"/Admin/Products/Details/{product.Id}"
                );
            }
            catch (Exception ex)
            {
                // Log error but don't fail the product creation
                System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
            }

            TempData["Success"] = "Product created successfully! Your product is pending admin approval and will be visible to other users once approved.";
            return RedirectToAction(nameof(MyProducts));
        }

        // ============================
        // EDIT
        // ============================

        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeWord: "Category,Images"
            );

            if (existing == null)
                return NotFound();

            if (!CanManage(existing))
                return Forbid();

            var model = await PopulateCreateViewModel(existing);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCreateViewModel model)
        {
            if (model == null || model.Product == null)
                return BadRequest();

            if (model.Product.CategoryId == null || model.Product.CategoryId <= 0)
                ModelState.AddModelError("Product.CategoryId", "Please select a category.");

            if (model.Product.Price <= 0)
                ModelState.AddModelError("Product.Price", "Price must be greater than 0.");

            if (!ModelState.IsValid)
            {
                model = await PopulateCreateViewModel(model.Product);
                return View(model);
            }

            var existing = await _repo.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeWord: "Category,Images"
            );

            if (existing == null)
                return NotFound();

            if (!CanManage(existing))
                return Forbid();

            existing.Name = model.Product.Name;
            existing.CategoryId = model.Product.CategoryId;
            existing.Price = model.Product.Price;
            existing.UpdatedAt = DateTime.UtcNow;
            
            // Ensure SellerId is set if not already set
            if (string.IsNullOrEmpty(existing.SellerId))
            {
                var userIdStr = _userManager.GetUserId(User);
                existing.SellerId = userIdStr;
            }

            await _repo.SaveAsync();
            await HandleUploadedFiles(existing, model.Files);

            return RedirectToAction(nameof(Index));
        }

        // ============================
        // DELETE
        // ============================

        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _repo.GetFirstOrDefaultAsync(
                p => p.Id == id,
                includeWord: "Category,Images"
            );

            if (product == null)
                return NotFound();

            if (!CanManage(product))
                return Forbid();

            return View(product);
        }

        [Authorize]
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var product = await _repo.GetFirstOrDefaultAsync(
                    p => p.Id == id,
                    includeWord: "Images,Listings"
                );

                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (!CanManage(product))
                    return Forbid();

                // Delete product images and files
                if (product.Images != null && product.Images.Any())
                {
                    foreach (var img in product.Images.ToList())
                    {
                        var filePath = Path.Combine(
                            _env.WebRootPath,
                            img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
                        );

                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);

                        _imageRepo.Remove(img);
                    }

                    await _imageRepo.SaveAsync();
                }

                // Delete the product
                // This will cascade delete:
                // - Listings (configured as Cascade)
                // - ShoppingCartItems (configured as Cascade via Listing)
                _repo.Remove(product);
                await _repo.SaveAsync();

                TempData["Success"] = $"Product '{product.Name}' has been deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while deleting the product: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================
        // IMAGES: DELETE / SET MAIN
        // ============================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage([FromBody] ImageRequest request)
        {
            var img = await _imageRepo.GetFirstOrDefaultAsync(i => i.Id == request.ImageId);
            if (img == null) return NotFound();

            var product = await _repo.GetFirstOrDefaultAsync(p => p.Id == img.ProductId);
            if (product == null || !CanManage(product))
                return Forbid();

            var filePath = Path.Combine(
                _env.WebRootPath,
                img.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
            );

            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            _imageRepo.Remove(img);
            await _imageRepo.SaveAsync();

            return Ok();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetMainImage([FromBody] ImageRequest request)
        {
            var img = await _imageRepo.GetFirstOrDefaultAsync(i => i.Id == request.ImageId);
            if (img == null) return NotFound();

            var product = await _repo.GetFirstOrDefaultAsync(
                p => p.Id == img.ProductId,
                includeWord: "Images"
            );

            if (product == null || !CanManage(product))
                return Forbid();

            foreach (var im in product.Images)
                im.IsMainImage = false;

            img.IsMainImage = true;

            await _imageRepo.SaveAsync();

            return Ok();
        }

        // ============================
        // FILE UPLOAD HANDLER
        // ============================

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

        // ============================
        // POPULATE VIEWMODEL
        // ============================

        private async Task<ProductCreateViewModel> PopulateCreateViewModel(Product? product = null)
        {
            var allCategories = await _categoryRepo.GetAllAsync();
            var categories = allCategories
                .Select(c => new SelectListItem
                {
                    Text = c.CategoryName,
                    Value = c.Id.ToString(),
                    Selected = (product != null && product.CategoryId == c.Id)
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
