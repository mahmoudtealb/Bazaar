

namespace StudentBazaar.Entities.Models
{
    public class OrderItem : BaseEntity
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(250)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Subtotal { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}

