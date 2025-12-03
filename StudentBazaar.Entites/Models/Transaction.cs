

namespace StudentBazaar.Entities.Models
{
    public class Transaction : BaseEntity
    {
        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public Order Order { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // "Pending", "Completed", "Failed", "Refunded"

        [MaxLength(500)]
        public string? TransactionId { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}

