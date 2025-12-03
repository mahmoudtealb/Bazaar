
using System.ComponentModel;


namespace StudentBazaar.Entities.Models
{
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(250)]
        [DisplayName("Product Name")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("Category")]
        [Required(ErrorMessage = "Please select a category.")]
        public int? CategoryId { get; set; }  // تم تعديل int إلى int? لتفادي مشكلة Required

        [Required]
        [DisplayName("Price")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between 0.01 and 999999.99")]
        public decimal Price { get; set; }

        public int? OwnerId { get; set; }

        public ApplicationUser? Owner { get; set; }

        // SellerId as string for filtering products by seller
        [MaxLength(450)]
        public string? SellerId { get; set; }

        // ==========================
        // 🔹 Admin Management
        // ==========================
        public bool IsApproved { get; set; } = false;

        public DateTime? ApprovedAt { get; set; }

        public bool IsSold { get; set; } = false;

        public bool IsFeatured { get; set; } = false;

        [ForeignKey(nameof(CategoryId))]
        public ProductCategory? Category { get; set; } 

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
