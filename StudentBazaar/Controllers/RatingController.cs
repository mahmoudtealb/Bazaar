using StudentBazaar.Entities.Repositories;
using Microsoft.AspNetCore.Identity;
using StudentBazaar.Entities.Models;

namespace StudentBazaar.Web.Controllers
{
    public class RatingController : Controller
    {
        private readonly IGenericRepository<Rating> _repo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public RatingController(
            IGenericRepository<Rating> repo,
            IGenericRepository<Product> productRepo,
            IGenericRepository<Order> orderRepo,
            UserManager<ApplicationUser> userManager)
        {
            _repo = repo;
            _productRepo = productRepo;
            _orderRepo = orderRepo;
            _userManager = userManager;
        }

        private int GetCurrentUserId()
        {
            var idStr = _userManager.GetUserId(User);
            return int.Parse(idStr!);
        }

        // GET: Rating
        public async Task<IActionResult> Index()
        {
            var ratings = await _repo.GetAllAsync(includeWord: "User,Product");
            return View(ratings);
        }

        // GET: Rating/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(r => r.Id == id, includeWord: "User,Product");
            if (entity == null)
                return NotFound();

            return View(entity);
        }

        // GET: Rating/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Rating/Create (من صفحة Product Details)
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productId, int stars, string? comment)
        {
            if (stars < 1 || stars > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5 stars.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            // التحقق من وجود المنتج
            var product = await _productRepo.GetFirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Product");
            }

            // التحقق من أن المستخدم لا يقيم منتجه الخاص
            var currentUserId = GetCurrentUserId();
            if (product.OwnerId.HasValue && product.OwnerId.Value == currentUserId)
            {
                TempData["Error"] = "You cannot rate your own product.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            // السماح بالتقييم فقط إذا كان المستخدم قد اشترى هذا المنتج واستلمه (Delivered أو Completed)
            var orders = await _orderRepo.GetAllAsync(
                o => o.BuyerId == currentUserId && 
                     (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Completed),
                includeWord: "OrderItems"
            );
            
            var hasPurchased = orders.Any(o => 
                o.OrderItems != null && 
                o.OrderItems.Any(oi => oi.ProductId == productId)
            );

            if (!hasPurchased)
            {
                TempData["Error"] = "You can only rate products you have purchased.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            // التحقق من أن المستخدم لم يقم بتقييم هذا المنتج من قبل
            var existingRating = await _repo.GetFirstOrDefaultAsync(
                r => r.ProductId == productId && r.UserId == currentUserId
            );

            if (existingRating != null)
            {
                // تحديث التقييم الموجود
                existingRating.Stars = stars;
                existingRating.Comment = comment;
                existingRating.UpdatedAt = DateTime.UtcNow;
                await _repo.SaveAsync();
                TempData["Success"] = "Your rating has been updated successfully.";
            }
            else
            {
                // إضافة تقييم جديد
                var rating = new Rating
                {
                    ProductId = productId,
                    UserId = currentUserId,
                    Stars = stars,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                };

                await _repo.AddAsync(rating);
                await _repo.SaveAsync();
                TempData["Success"] = "Thank you for your rating!";
            }

            return RedirectToAction("Details", "Product", new { id = productId });
        }

        // POST: Rating/Delete
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int ratingId, int productId)
        {
            var rating = await _repo.GetFirstOrDefaultAsync(r => r.Id == ratingId);
            if (rating == null)
            {
                TempData["Error"] = "Rating not found.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var currentUserId = GetCurrentUserId();
            
            // التحقق من أن المستخدم هو صاحب التقييم
            if (rating.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "You don't have permission to delete this rating.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            _repo.Remove(rating);
            await _repo.SaveAsync();
            
            TempData["Success"] = "Your rating has been deleted successfully.";
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        // GET: Rating/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var existing = await _repo.GetFirstOrDefaultAsync(r => r.Id == id);
            if (existing == null)
                return NotFound();

            return View(existing);
        }

        // POST: Rating/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Rating entity)
        {
            if (!ModelState.IsValid)
                return View(entity);

            var existing = await _repo.GetFirstOrDefaultAsync(r => r.Id == id);
            if (existing == null)
                return NotFound();

            existing.UserId = entity.UserId;
            existing.ProductId = entity.ProductId;
            existing.Stars = entity.Stars;
            existing.Comment = entity.Comment;
            existing.UpdatedAt = DateTime.Now;

            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Rating/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(r => r.Id == id);
            if (entity == null)
                return NotFound();

            return View(entity);
        }

        // POST: Rating/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _repo.GetFirstOrDefaultAsync(r => r.Id == id);
            if (entity == null)
                return NotFound();

            _repo.Remove(entity);
            await _repo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
