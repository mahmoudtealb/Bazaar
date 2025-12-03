

namespace StudentBazaar.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IGenericRepository<Product> productRepo,
            UserManager<ApplicationUser> userManager)
        {
            _orderRepo = orderRepo;
            _listingRepo = listingRepo;
            _orderItemRepo = orderItemRepo;
            _productRepo = productRepo;
            _userManager = userManager;
        }

        private int GetCurrentUserId()
        {
            var idStr = _userManager.GetUserId(User);
            return int.Parse(idStr!);
        }

        // ===============================
        // 1) My Orders / All Orders
        // ===============================
        public async Task<IActionResult> Index()
        {
            IEnumerable<Order> orders;

            if (User.IsInRole("Admin"))
            {
                // ������ ���� �� ��������
                orders = await _orderRepo.GetAllAsync(includeWord: "OrderItems,OrderItems.Product,OrderItems.Product.Images,Buyer,Shipment");
            }
            else
            {
                // ������ ���� �������� ��
                var currentUserId = GetCurrentUserId();
                orders = await _orderRepo.GetAllAsync(
                    o => o.BuyerId == currentUserId,
                    includeWord: "OrderItems,OrderItems.Product,OrderItems.Product.Images,Buyer,Shipment"
                );
            }

            return View(orders);
        }

        // ===============================
        // 2) Details
        // ===============================
        public async Task<IActionResult> Details(int id)
        {
            var entity = await _orderRepo.GetFirstOrDefaultAsync(
                o => o.Id == id,
                includeWord: "OrderItems,OrderItems.Product,OrderItems.Product.Images,OrderItems.Product.Category,Listing,Listing.Product,Buyer,Shipment"
            );

            if (entity == null)
                return NotFound();

            // ������ �� ����� ����� �� ����
            if (!User.IsInRole("Admin") && entity.BuyerId != GetCurrentUserId())
                return Forbid();

            return View(entity);
            // �� ��� ������� View ��� Details ���� ���� ���� ���� ������
        }

        // ===============================
        // 3) Create (Admin ��� - ����)
        // ===============================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            // DropDown ��� Listings

            // DropDown ���������� (���� ����� ��� ����������)
            var users = _userManager.Users.ToList();
            ViewBag.BuyerList = users
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FullName
                })
                .ToList();

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order entity)
        {
            if (!ModelState.IsValid)
            {
                // ���� ���� ���� ��� ViewBags ����
                await FillDropDownsForCreateEdit(entity.ListingId, entity.BuyerId);
                return View(entity);
            }

            await _orderRepo.AddAsync(entity);
            await _orderRepo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 4) Edit (Admin)
        // ===============================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var existing = await _orderRepo.GetFirstOrDefaultAsync(o => o.Id == id);
            if (existing == null)
                return NotFound();

            await FillDropDownsForCreateEdit(existing.ListingId, existing.BuyerId);
            return View(existing);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order entity)
        {
            if (!ModelState.IsValid)
            {
                await FillDropDownsForCreateEdit(entity.ListingId, entity.BuyerId);
                return View(entity);
            }

            var existing = await _orderRepo.GetFirstOrDefaultAsync(o => o.Id == id);
            if (existing == null)
                return NotFound();

            existing.ListingId = entity.ListingId;
            existing.BuyerId = entity.BuyerId;
            existing.OrderDate = entity.OrderDate;
            existing.Status = entity.Status;
            existing.PaymentMethod = entity.PaymentMethod;
            existing.TotalAmount = entity.TotalAmount;
            existing.SiteCommission = entity.SiteCommission;
            existing.UpdatedAt = DateTime.Now;

            await _orderRepo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 5) Delete (Admin)
        // ===============================
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _orderRepo.GetFirstOrDefaultAsync(o => o.Id == id,
                includeWord: "OrderItems,OrderItems.Product,Buyer");
            if (entity == null)
                return NotFound();

            return View(entity);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entity = await _orderRepo.GetFirstOrDefaultAsync(o => o.Id == id);
            if (entity == null)
                return NotFound();

            _orderRepo.Remove(entity);
            await _orderRepo.SaveAsync();
            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // 6) ���� Buy �� ������
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int listingId)
        {
            var listing = await _listingRepo.GetFirstOrDefaultAsync(
                l => l.Id == listingId,
                includeWord: "Product"
            );

            if (listing == null)
                return NotFound();

            var currentUserId = GetCurrentUserId();

            // �� ���� ���� ���� ������ ����� �� ����:
            if (listing.SellerId == currentUserId)
                return BadRequest("You cannot buy your own listing.");

            var total = listing.Price;
            var commission = Math.Round(total * 0.05m, 2);   // 5% �����

            var order = new Order
            {
                ListingId = listing.Id,
                BuyerId = currentUserId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                PaymentMethod = PaymentMethod.CashOnDelivery,
                TotalAmount = total,
                SiteCommission = commission,
                CreatedAt = DateTime.Now
            };

            await _orderRepo.AddAsync(order);
            await _orderRepo.SaveAsync();

            // Create OrderItem for this listing
            var orderItem = new OrderItem
            {
                OrderId = order.Id,
                ProductId = listing.ProductId,
                ProductName = listing.Product?.Name ?? "Unknown Product",
                Price = listing.Price, // Use Listing.Price (the actual selling price)
                Quantity = 1,
                Subtotal = listing.Price, // Price * 1
                CreatedAt = DateTime.UtcNow
            };

            await _orderItemRepo.AddAsync(orderItem);

            // Mark product as Sold
            if (listing.Product != null)
            {
                listing.Product.IsSold = true;
                await _productRepo.SaveAsync();
            }

            // Mark listing as Sold
            listing.Status = ListingStatus.Sold;
            await _listingRepo.SaveAsync();

            // Save OrderItem
            await _orderItemRepo.SaveAsync();

            // Note: You'll need to inject IGenericRepository<OrderItem> to add the order item
            // For now, this is a placeholder - the Buy method should be updated to use OrderItems

            // ���� ��� ����� ���� ������� �� ���� (Sold �����)
            // listing.Status = ListingStatus.Sold;
            // await _listingRepo.SaveAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // Helper ���� ��� DropDowns
        // ===============================
        private async Task FillDropDownsForCreateEdit(int? selectedListingId = null, int? selectedBuyerId = null)
        {
            var listings = await _listingRepo.GetAllAsync(includeWord: "Product,Seller");
            ViewBag.ListingList = listings
                .Select(l => new SelectListItem
                {
                    Value = l.Id.ToString(),
                    Text = $"{l.Product?.Name ?? "Product"} - {l.Price:0.00}",
                    Selected = (selectedListingId.HasValue && l.Id == selectedListingId.Value)
                })
                .ToList();

            var users = _userManager.Users.ToList();
            ViewBag.BuyerList = users
                .Select(u => new SelectListItem
                {
                    Value = u.Id.ToString(),
                    Text = u.FullName,
                    Selected = (selectedBuyerId.HasValue && u.Id == selectedBuyerId.Value)
                })
                .ToList();
        }
    }
}
