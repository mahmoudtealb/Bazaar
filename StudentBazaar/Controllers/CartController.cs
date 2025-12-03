

namespace StudentBazaar.Web.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IShoppingCartItemRepository _cartRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            IShoppingCartItemRepository cartRepo,
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Product> productRepo,
            UserManager<ApplicationUser> userManager)
        {
            _cartRepo = cartRepo;
            _listingRepo = listingRepo;
            _productRepo = productRepo;
            _userManager = userManager;
        }

        // =======================
        // Get current user ID
        // =======================
        private int GetCurrentUserId()
        {
            var userIdStr = _userManager.GetUserId(User);
            return int.Parse(userIdStr!);
        }

        // =======================
        // View Cart
        // =======================
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var items = await _cartRepo.GetAllAsync(c => c.UserId == userId, includeWord: "Listing,Listing.Product,Listing.Product.Images");
            var userItems = items.ToList();
            return View(userItems);
        }

        // =======================
        // Add to Cart
        // =======================
        [HttpPost]
        public async Task<IActionResult> AddToCart(int listingId, int quantity = 1)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // جلب Listing مع Product للتأكد من وجوده
                var listing = await _listingRepo.GetFirstOrDefaultAsync(
                    l => l.Id == listingId,
                    includeWord: "Product");
                
                if (listing == null)
                {
                    TempData["Error"] = "The listing you're trying to add is not found.";
                    return RedirectToAction("Index");
                }

                // التحقق من أن Listing متاح
                if (listing.Status != ListingStatus.Available)
                {
                    TempData["Error"] = "This item is no longer available for purchase.";
                    return RedirectToAction("Index");
                }

                // التحقق من أن المنتج موافق عليه (Approved) وليس مباع (Sold)
                if (listing.Product != null)
                {
                    if (!listing.Product.IsApproved)
                    {
                        TempData["Error"] = "This product is pending approval and cannot be purchased yet.";
                        return RedirectToAction("Index");
                    }

                    if (listing.Product.IsSold)
                    {
                        TempData["Error"] = "This product has already been sold.";
                        return RedirectToAction("Index");
                    }
                }

                // التحقق من الكمية
                if (quantity < 1)
                {
                    quantity = 1;
                }

                // البحث عن عنصر موجود في السلة
                var existing = await _cartRepo.GetFirstOrDefaultAsync(
                    c => c.UserId == userId && c.ListingId == listingId);
                
                if (existing != null)
                {
                    // تحديث الكمية إذا كان العنصر موجود
                    existing.Quantity += quantity;
                    // التأكد من أن الكمية لا تتجاوز الحد الأقصى
                    if (existing.Quantity > 10)
                    {
                        existing.Quantity = 10;
                    }
                    _cartRepo.Update(existing);
                }
                else
                {
                    // إضافة عنصر جديد
                    var cartItem = new ShoppingCartItem
                    {
                        UserId = userId,
                        ListingId = listingId,
                        Quantity = quantity
                    };
                    await _cartRepo.AddAsync(cartItem);
                }

                await _cartRepo.SaveAsync();
                TempData["Success"] = $"'{listing.Product?.Name ?? "Item"}' added to cart successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while adding the item to cart. Please try again.";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddProductToCart(int productId, int quantity = 1)
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // جلب المنتج للتأكد من وجوده
                var product = await _productRepo.GetFirstOrDefaultAsync(p => p.Id == productId);
                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return RedirectToAction("Index", "Product");
                }

                // التحقق من أن المنتج موافق عليه (Approved) وليس مباع (Sold)
                if (!product.IsApproved)
                {
                    TempData["Error"] = "This product is pending approval and cannot be purchased yet.";
                    return RedirectToAction("Index", "Product");
                }

                if (product.IsSold)
                {
                    TempData["Error"] = "This product has already been sold.";
                    return RedirectToAction("Index", "Product");
                }

                // البحث عن Listing متاح (Available) أولاً
                var listing = await _listingRepo.GetFirstOrDefaultAsync(
                    l => l.ProductId == productId && l.Status == ListingStatus.Available);
                
                // إذا لم يكن هناك Listing متاح، ابحث عن أي Listing
                if (listing == null)
                {
                    listing = await _listingRepo.GetFirstOrDefaultAsync(
                        l => l.ProductId == productId);
                }

                // إذا لم يكن هناك Listing على الإطلاق، قم بإنشاء واحد تلقائياً
                if (listing == null)
                {
                    // إنشاء Listing جديد تلقائياً
                    listing = new Listing
                    {
                        ProductId = productId,
                        SellerId = product.OwnerId ?? userId, // استخدام Owner المنتج أو المستخدم الحالي
                        Price = product.Price,
                        Condition = ListingCondition.New,
                        Description = $"Auto-generated listing for {product.Name}",
                        Status = ListingStatus.Available,
                        PostingDate = DateTime.UtcNow
                    };

                    await _listingRepo.AddAsync(listing);
                    await _listingRepo.SaveAsync();
                }
                else if (listing.Status != ListingStatus.Available)
                {
                    // إذا كان Listing موجود لكن غير متاح، قم بتحديث حالته إلى Available
                    listing.Status = ListingStatus.Available;
                    _listingRepo.Update(listing);
                    await _listingRepo.SaveAsync();
                }

                // التحقق من أن الكمية صحيحة
                if (quantity < 1)
                {
                    quantity = 1;
                }

                return await AddToCart(listing.Id, quantity);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while adding the product to cart. Please try again.";
                return RedirectToAction("Index", "Product");
            }
        }

        // =======================
        // Remove from Cart
        // =======================
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var item = await _cartRepo.GetFirstOrDefaultAsync(c => c.Id == id);
                
                if (item == null)
                {
                    TempData["Error"] = "Item not found in cart.";
                    return RedirectToAction("Index");
                }
                
                if (item.UserId != userId)
                {
                    return Forbid();
                }

                _cartRepo.Remove(item);
                await _cartRepo.SaveAsync();
                TempData["Success"] = "Item removed from cart successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while removing the item. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // =======================
        // Update Quantity
        // =======================
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            try
            {
                var userId = GetCurrentUserId();
                var item = await _cartRepo.GetFirstOrDefaultAsync(c => c.Id == id, includeWord: "Listing");
                
                if (item == null)
                {
                    return Json(new { success = false, message = "Item not found in cart." });
                }
                
                if (item.UserId != userId)
                {
                    return Json(new { success = false, message = "Unauthorized." });
                }

                // التحقق من الكمية
                if (quantity < 1)
                {
                    quantity = 1;
                }
                else if (quantity > 10)
                {
                    quantity = 10;
                }

                item.Quantity = quantity;
                _cartRepo.Update(item);
                await _cartRepo.SaveAsync();
                
                // Calculate item total
                var itemTotal = (item.Listing?.Price ?? 0) * quantity;
                
                return Json(new { 
                    success = true, 
                    quantity = quantity,
                    itemTotal = itemTotal.ToString("F2")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while updating quantity. Please try again." });
            }
        }

        // =======================
        // Checkout page
        // =======================
        public async Task<IActionResult> Checkout()
        {
            var userId = GetCurrentUserId();
            var items = await _cartRepo.GetAllAsync(c => c.UserId == userId, includeWord: "Listing,Listing.Product");
            var userItems = items.ToList();

            if (!userItems.Any())
            {
                TempData["Error"] = "Your cart is empty!";
                return RedirectToAction("Index");
            }

            var total = userItems.Sum(i => i.Listing.Price * i.Quantity);
            ViewBag.Total = total;
            return RedirectToAction("Index", "Checkout");
        }

        // =======================
        // Confirm Checkout
        // =======================
        [HttpPost]
        public async Task<IActionResult> ConfirmCheckout()
        {
            var userId = GetCurrentUserId();
            var items = await _cartRepo.GetAllAsync(c => c.UserId == userId);
            _cartRepo.RemoveRange(items);
            await _cartRepo.SaveAsync();
            TempData["Success"] = "Checkout completed successfully!";
            return RedirectToAction("Index");
        }
    }
}