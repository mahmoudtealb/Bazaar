using System.ComponentModel;


namespace StudentBazaar.Web.Models
{
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(250)]
        [DisplayName("Product Name")]
        public string Name { get; set; } = string.Empty;

        // ==========================
        // 🔗 Foreign Keys
        // ==========================

        [Required]
        [DisplayName("Category")]
        public int CategoryId { get; set; }

        [Required]
        [DisplayName("Study Year")]
        public int StudyYearId { get; set; }

        // ==========================
        // Navigation Properties
        // ==========================

        [ForeignKey(nameof(CategoryId))]
        public ProductCategory Category { get; set; } = null!;

        [ForeignKey(nameof(StudyYearId))]
        public StudyYear StudyYear { get; set; } = null!;

        // ==========================
        // Reverse Relationships
        // ==========================

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public ICollection<Listing> Listings { get; set; } = new List<Listing>();

        public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}
