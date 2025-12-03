

namespace StudentBazaar.Entities.Models
{
    public class ProductImage : BaseEntity
    {
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsMainImage { get; set; } = false;

        [Required]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}
