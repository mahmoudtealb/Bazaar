using StudentBazaar.Entities.Models;

namespace StudentBazaar.Entities.Models
{
    public class Cart : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Required]
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}

