
using StudentBazaar.Web.Models;

namespace StudentBazaar.Web.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IShoppingCartItemRepository _cartRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(
            IConfiguration configuration, 
            ILogger<CheckoutController> logger, 
            IShoppingCartItemRepository cartRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IGenericRepository<Product> productRepo,
            IGenericRepository<Listing> listingRepo,
            UserManager<ApplicationUser> userManager)
        {
            _configuration = configuration;
            _logger = logger;
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _productRepo = productRepo;
            _listingRepo = listingRepo;
            _userManager = userManager;
        }

        private int GetCurrentUserId()
        {
            var userIdStr = _userManager.GetUserId(User);
            return int.Parse(userIdStr!);
        }

        // GET: Checkout page
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = int.Parse(userIdStr);

            var items = await _cartRepo.GetAllAsync(c => c.UserId == userId, includeWord: "Listing,Listing.Product,Listing.Product.Images,Listing.Product.Category");
            var itemsList = items.ToList();

            // Check if cart is empty
            if (!itemsList.Any())
            {
                TempData["Error"] = "Your cart is empty. Please add items to your cart before checkout.";
                return RedirectToAction("Index", "Cart");
            }

            var cartItems = itemsList.Select(i => new CartItemViewModel
            {
                ProductId = i.Listing?.ProductId ?? 0,
                ProductName = i.Listing?.Product?.Name ?? "Unknown Product",
                CategoryName = i.Listing?.Product?.Category?.CategoryName ?? string.Empty,
                Price = i.Listing?.Price ?? 0,
                Quantity = i.Quantity,
                ImageUrl = (i.Listing?.Product?.Images != null && i.Listing.Product.Images.Any())
                    ? (i.Listing.Product.Images.FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? i.Listing.Product.Images.First().ImageUrl)
                    : string.Empty
            }).ToList();

            var subtotal = cartItems.Sum(x => x.Price * x.Quantity);
            var shipping = 0m; // Default to free shipping
            var model = new CheckoutViewModel
            {
                CartItems = cartItems,
                Subtotal = subtotal,
                Shipping = shipping,
                ShippingOption = "Free",
                Tax = 0,
                Total = subtotal + shipping
            };

            // Check if Stripe is configured
            var stripeKey = _configuration["Stripe:SecretKey"];
            ViewBag.StripeConfigured = IsValidStripeKey(stripeKey);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompletePayment(CheckoutViewModel model)
        {
            // Calculate shipping cost based on selected option
            var shippingCost = model.ShippingOption == "Express" ? 50m : 0m;
            model.Shipping = shippingCost;
            model.ShippingCost = shippingCost;

            // Re-populate cart items if validation fails
            if (!ModelState.IsValid)
            {
                var userIdStr = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    var userId = int.Parse(userIdStr);
                    var items = await _cartRepo.GetAllAsync(c => c.UserId == userId, includeWord: "Listing,Listing.Product,Listing.Product.Images,Listing.Product.Category");
                    var itemsList = items.ToList();
                    
                    var cartItems = itemsList.Select(i => new CartItemViewModel
                    {
                        ProductId = i.Listing?.ProductId ?? 0,
                        ProductName = i.Listing?.Product?.Name ?? "Unknown Product",
                        CategoryName = i.Listing?.Product?.Category?.CategoryName ?? string.Empty,
                        Price = i.Listing?.Price ?? 0,
                        Quantity = i.Quantity,
                        ImageUrl = (i.Listing?.Product?.Images != null && i.Listing.Product.Images.Any())
                            ? (i.Listing.Product.Images.FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? i.Listing.Product.Images.First().ImageUrl)
                            : string.Empty
                    }).ToList();

                    var subtotal = cartItems.Sum(x => x.Price * x.Quantity);
                    var shipping = model.ShippingOption == "Express" ? 50m : 0m;
                    model.CartItems = cartItems;
                    model.Subtotal = subtotal;
                    model.Shipping = shipping;
                    model.ShippingCost = shipping;
                    model.Tax = 0;
                    model.Total = subtotal + shipping;
                }

                var stripeKey = _configuration["Stripe:SecretKey"];
                ViewBag.StripeConfigured = IsValidStripeKey(stripeKey);
                return View("Index", model);
            }

            if (model.SelectedPaymentMethod == "Card")
            {
                // Stripe.net package is not installed, so card payment is unavailable
                _logger.LogWarning("Card payment selected but Stripe.net package is not installed.");
                TempData["Error"] = "Credit card payment is currently unavailable. Please use PayPal or Vodafone Cash.";
                return RedirectToAction("Index");
            }
            else if (model.SelectedPaymentMethod == "PayPal")
            {
                // Redirect to PayPal checkout
                var domain = $"{Request.Scheme}://{Request.Host}";
                var totalAmount = model.Total;
                var businessEmail = _configuration["PayPal:BusinessEmail"] ?? "sb-merchant@business.example.com"; // PayPal sandbox email
                var returnUrl = $"{domain}/Checkout/Success?paymentMethod=PayPal";
                var cancelUrl = $"{domain}/Checkout/Cancel?paymentMethod=PayPal";

                // Build PayPal checkout URL
                var paypalUrl = $"https://www.sandbox.paypal.com/checkoutnow?token=PAYMENT_TOKEN&amount={totalAmount:F2}&currency=USD&return={Uri.EscapeDataString(returnUrl)}&cancel_return={Uri.EscapeDataString(cancelUrl)}";

                // For production, you would create a PayPal order via API first
                // This is a simplified redirect approach
                // Redirect to a PayPal payment page
                TempData["PayPalAmount"] = totalAmount.ToString("F2");
                TempData["PayPalSubtotal"] = model.Subtotal.ToString("F2");
                TempData["PayPalShipping"] = model.Shipping.ToString("F2");
                TempData["PayPalReturnUrl"] = returnUrl;
                TempData["PayPalCancelUrl"] = cancelUrl;
                return RedirectToAction("PayPalPayment");
            }
            else if (model.SelectedPaymentMethod == "VodafoneCash")
            {
                // Redirect to Vodafone Cash payment page
                var amount = model.Total.ToString("F2");
                var items = string.Join(", ", model.CartItems.Select(x => $"{x.ProductName} (x{x.Quantity})"));
                
                TempData["VodafoneCashAmount"] = amount;
                TempData["VodafoneCashSubtotal"] = model.Subtotal.ToString("F2");
                TempData["VodafoneCashShipping"] = model.Shipping.ToString("F2");
                TempData["VodafoneCashItems"] = items;
                
                // Keep TempData for next request
                TempData.Keep("VodafoneCashAmount");
                TempData.Keep("VodafoneCashSubtotal");
                TempData.Keep("VodafoneCashShipping");
                TempData.Keep("VodafoneCashItems");
                
                return RedirectToAction("VodafoneCashPayment");
            }
            else if (model.SelectedPaymentMethod == "CashOnDelivery")
            {
                // Create order for Cash on Delivery
                var orderId = await CreateOrderFromCart(PaymentMethod.CashOnDelivery);
                if (orderId.HasValue)
                {
                    return RedirectToAction("Success", new { paymentMethod = "CashOnDelivery", orderId = orderId.Value });
                }
                else
                {
                    TempData["Error"] = "Failed to create order. Please try again.";
                    return RedirectToAction("Index");
                }
            }
            else if (model.SelectedPaymentMethod == "BankTransfer")
            {
                // Create order for Bank Transfer
                var orderId = await CreateOrderFromCart(PaymentMethod.BankTransfer);
                if (orderId.HasValue)
                {
                    return RedirectToAction("Success", new { paymentMethod = "BankTransfer", orderId = orderId.Value });
                }
                else
                {
                    TempData["Error"] = "Failed to create order. Please try again.";
                    return RedirectToAction("Index");
                }
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Success(string paymentMethod = null, int? orderId = null)
        {
            // If orderId is provided, the order was already created
            // If not, create it now (for PayPal and VodafoneCash that redirect here)
            if (!orderId.HasValue)
            {
                PaymentMethod paymentMethodEnum = paymentMethod switch
                {
                    "PayPal" => PaymentMethod.PayPal,
                    "VodafoneCash" => PaymentMethod.VodafoneCash,
                    _ => PaymentMethod.CashOnDelivery
                };

                orderId = await CreateOrderFromCart(paymentMethodEnum);
            }

            ViewBag.Message = "Payment Successful 🎉";
            ViewBag.PaymentMethod = paymentMethod ?? "Unknown";
            ViewBag.OrderId = orderId;
            
            return View();
        }

        [HttpGet]
        public IActionResult Cancel()
        {
            ViewBag.Message = "Payment Cancelled ❌";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PayPalPayment()
        {
            var amount = TempData["PayPalAmount"]?.ToString();
            var subtotal = TempData["PayPalSubtotal"]?.ToString();
            var shipping = TempData["PayPalShipping"]?.ToString();
            var returnUrl = TempData["PayPalReturnUrl"]?.ToString();
            var cancelUrl = TempData["PayPalCancelUrl"]?.ToString();
            List<CartItemViewModel> cartItemsList = new List<CartItemViewModel>();

            // If amount is not in TempData, calculate it from cart
            if (string.IsNullOrEmpty(amount) || amount == "0.00")
            {
                var userIdStr = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    var userId = int.Parse(userIdStr);
                    var cartItems = await _cartRepo.GetAllAsync(
                        c => c.UserId == userId, 
                        includeWord: "Listing,Listing.Product,Listing.Product.Images");
                    
                    var cartTotal = cartItems.Sum(i => (i.Listing?.Price ?? 0) * i.Quantity);
                    amount = cartTotal.ToString("F2");
                    subtotal = cartTotal.ToString("F2");
                    shipping = "0.00";

                    // Build cart items list
                    cartItemsList = cartItems.Select(i => new CartItemViewModel
                    {
                        ProductId = i.Listing?.ProductId ?? 0,
                        ProductName = i.Listing?.Product?.Name ?? "Unknown Product",
                        CategoryName = i.Listing?.Product?.Category?.CategoryName ?? string.Empty,
                        Price = i.Listing?.Price ?? 0,
                        Quantity = i.Quantity,
                        ImageUrl = (i.Listing?.Product?.Images != null && i.Listing.Product.Images.Any())
                            ? (i.Listing.Product.Images.FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? i.Listing.Product.Images.First().ImageUrl)
                            : string.Empty
                    }).ToList();
                }
                else
                {
                    amount = "0.00";
                    subtotal = "0.00";
                    shipping = "0.00";
                }
            }

            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = $"{Request.Scheme}://{Request.Host}/Checkout/Success?paymentMethod=PayPal";
            }

            if (string.IsNullOrEmpty(cancelUrl))
            {
                cancelUrl = $"{Request.Scheme}://{Request.Host}/Checkout/Cancel?paymentMethod=PayPal";
            }

            ViewBag.Amount = amount;
            ViewBag.Subtotal = subtotal ?? "0.00";
            ViewBag.Shipping = shipping ?? "0.00";
            ViewBag.CartItems = cartItemsList;
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.CancelUrl = cancelUrl;
            ViewBag.BusinessEmail = _configuration["PayPal:BusinessEmail"] ?? "sb-merchant@business.example.com";

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VodafoneCashPayment()
        {
            var amount = TempData["VodafoneCashAmount"]?.ToString();
            var subtotal = TempData["VodafoneCashSubtotal"]?.ToString();
            var shipping = TempData["VodafoneCashShipping"]?.ToString();
            var items = TempData["VodafoneCashItems"]?.ToString() ?? "";
            List<CartItemViewModel> cartItemsList = new List<CartItemViewModel>();

            // If amount is not in TempData, calculate it from cart
            if (string.IsNullOrEmpty(amount) || amount == "0.00")
            {
                var userIdStr = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    var userId = int.Parse(userIdStr);
                    var cartItems = await _cartRepo.GetAllAsync(
                        c => c.UserId == userId, 
                        includeWord: "Listing,Listing.Product,Listing.Product.Images,Listing.Product.Category");
                    
                    var cartTotal = cartItems.Sum(i => (i.Listing?.Price ?? 0) * i.Quantity);
                    amount = cartTotal.ToString("F2");
                    subtotal = cartTotal.ToString("F2");
                    shipping = "0.00";

                    // Build cart items list
                    cartItemsList = cartItems.Select(i => new CartItemViewModel
                    {
                        ProductId = i.Listing?.ProductId ?? 0,
                        ProductName = i.Listing?.Product?.Name ?? "Unknown Product",
                        CategoryName = i.Listing?.Product?.Category?.CategoryName ?? string.Empty,
                        Price = i.Listing?.Price ?? 0,
                        Quantity = i.Quantity,
                        ImageUrl = (i.Listing?.Product?.Images != null && i.Listing.Product.Images.Any())
                            ? (i.Listing.Product.Images.FirstOrDefault(img => img.IsMainImage)?.ImageUrl ?? i.Listing.Product.Images.First().ImageUrl)
                            : string.Empty
                    }).ToList();

                    // Build items list if not provided
                    if (string.IsNullOrEmpty(items))
                    {
                        items = string.Join(", ", cartItems.Select(i => 
                            $"{i.Listing?.Product?.Name ?? "Unknown"} (x{i.Quantity})"));
                    }
                }
                else
                {
                    amount = "0.00";
                    subtotal = "0.00";
                    shipping = "0.00";
                }
            }

            ViewBag.Amount = amount;
            ViewBag.Subtotal = subtotal ?? "0.00";
            ViewBag.Shipping = shipping ?? "0.00";
            ViewBag.Items = items;
            ViewBag.CartItems = cartItemsList;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CompleteVodafoneCashPayment(string phoneNumber)
        {
            // Simulate payment processing
            // In production, this would integrate with Vodafone Cash API
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["Error"] = "Please enter your Vodafone Cash phone number.";
                return RedirectToAction("VodafoneCashPayment");
            }

            // Here you would call Vodafone Cash API to process payment
            // For now, simulate successful payment
            _logger.LogInformation("Vodafone Cash payment initiated for phone: {PhoneNumber}", phoneNumber);

            // Create order for Vodafone Cash payment
            var orderId = await CreateOrderFromCart(PaymentMethod.VodafoneCash);
            if (orderId.HasValue)
            {
                return RedirectToAction("Success", new { paymentMethod = "VodafoneCash", orderId = orderId.Value });
            }
            else
            {
                TempData["Error"] = "Failed to create order. Please try again.";
                return RedirectToAction("VodafoneCashPayment");
            }
        }

        // Helper method to create order from cart
        private async Task<int?> CreateOrderFromCart(PaymentMethod paymentMethod)
        {
            try
            {
                var userId = GetCurrentUserId();

                // Get all cart items for the current user
                var cartItems = await _cartRepo.GetAllAsync(
                    c => c.UserId == userId,
                    includeWord: "Listing,Listing.Product");

                var itemsList = cartItems.ToList();

                if (!itemsList.Any())
                {
                    _logger.LogWarning("Attempted to create order with empty cart for user {UserId}", userId);
                    return null;
                }

                // Validate that all listings are still available
                foreach (var cartItem in itemsList)
                {
                    if (cartItem.Listing == null)
                    {
                        _logger.LogWarning("Cart item has null listing for user {UserId}", userId);
                        return null;
                    }

                    if (cartItem.Listing.Status != ListingStatus.Available)
                    {
                        _logger.LogWarning("Listing {ListingId} is no longer available for user {UserId}", 
                            cartItem.ListingId, userId);
                        return null;
                    }
                }

                // Calculate total amount
                var totalAmount = itemsList.Sum(i => (i.Listing?.Price ?? 0) * i.Quantity);
                var commission = Math.Round(totalAmount * 0.05m, 2); // 5% commission

                // Get the first listing for the Order (for backward compatibility)
                var firstListing = itemsList.FirstOrDefault()?.Listing;

                // Create new Order
                var order = new Order
                {
                    ListingId = firstListing?.Id, // Set first listing for backward compatibility
                    BuyerId = userId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    PaymentMethod = paymentMethod,
                    TotalAmount = totalAmount,
                    SiteCommission = commission,
                    CreatedAt = DateTime.UtcNow
                };

                await _orderRepo.AddAsync(order);
                await _orderRepo.SaveAsync(); // Save to get the Order Id

                // Convert cart items to order items
                // Create OrderItems list first, then add all at once
                var orderItems = new List<OrderItem>();
                
                foreach (var cartItem in itemsList)
                {
                    if (cartItem.Listing?.Product == null) continue;

                    // For each cart item, create OrderItem with Quantity = 1
                    // If cart item has quantity > 1, create multiple OrderItems
                    for (int i = 0; i < cartItem.Quantity; i++)
                    {
                        var orderItem = new OrderItem
                        {
                            OrderId = order.Id,
                            ProductId = cartItem.Listing.ProductId,
                            ProductName = cartItem.Listing.Product.Name,
                            Price = cartItem.Listing.Price, // Use Listing.Price (the actual selling price)
                            Quantity = 1, // Always 1 per OrderItem
                            Subtotal = cartItem.Listing.Price, // Price * 1
                            CreatedAt = DateTime.UtcNow
                        };

                        orderItems.Add(orderItem);
                    }

                    // Mark product as Sold
                    cartItem.Listing.Product.IsSold = true;

                    // Mark listing as Sold
                    cartItem.Listing.Status = ListingStatus.Sold;
                }

                // Add all OrderItems to the Order before saving
                foreach (var orderItem in orderItems)
                {
                    await _orderItemRepo.AddAsync(orderItem);
                }

                // Save all changes (order items, product status, listing status)
                await _orderItemRepo.SaveAsync();
                await _productRepo.SaveAsync();
                await _listingRepo.SaveAsync();

                // Clear the cart
                _cartRepo.RemoveRange(itemsList);
                await _cartRepo.SaveAsync();

                _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, userId);

                // Send notification to admins about new order
                try
                {
                    var notificationService = HttpContext.RequestServices.GetRequiredService<StudentBazaar.Web.Services.INotificationService>();
                    await notificationService.BroadcastToAdminsAsync(
                        "New Order",
                        "New Order",
                        "Success",
                        $"/Admin/Orders/Details/{order.Id}"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error sending order notification");
                }

                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from cart for user {UserId}", GetCurrentUserId());
                return null;
            }
        }

        private bool IsValidStripeKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Check if it's a placeholder
            if (key.Contains("your_real") ||
                key.Contains("placeholder") ||
                key.Contains("replace") ||
                key.EndsWith("_here"))
                return false;

            // Check if it starts with sk_ (secret key) or pk_ (publishable key)
            // For API operations, we need sk_
            if (!key.StartsWith("sk_"))
                return false;

            // Check minimum length (Stripe keys are typically 32+ characters after the prefix)
            if (key.Length < 20)
                return false;

            return true;
        }
    }
}