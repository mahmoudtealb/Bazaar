
namespace StudentBazaar.Entities.Models
{
    public class ActivityLog : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        public int? UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        [MaxLength(50)]
        public string? EntityType { get; set; }

        public int? EntityId { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }
    }
}

