
namespace StudentBazaar.Web.Models
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new List<CartItemViewModel>();

        // Customer Information
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid email address format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string EmailAddress { get; set; } = string.Empty;

        // Shipping Information
        [Display(Name = "Country")]
        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country cannot exceed 100 characters")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "State / Province")]
        [Required(ErrorMessage = "State/Province is required")]
        [StringLength(100, ErrorMessage = "State/Province cannot exceed 100 characters")]
        public string StateProvince { get; set; } = string.Empty;

        [Display(Name = "City")]
        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; } = string.Empty;

        [Display(Name = "Full Address")]
        [Required(ErrorMessage = "Full address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string FullAddress { get; set; } = string.Empty;

        // Shipping Option
        [Display(Name = "Shipping Option")]
        [Required(ErrorMessage = "Please select a shipping option")]
        public string ShippingOption { get; set; } = "Free"; // Default to Free

        // Payment Method
        [Display(Name = "Payment Method")]
        [Required(ErrorMessage = "Please select a payment method")]
        public string SelectedPaymentMethod { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal ShippingCost { get; set; } = 0; // Cost for paid shipping
        public decimal Total { get; set; }
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }
}