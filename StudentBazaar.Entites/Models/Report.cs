using StudentBazaar.Entities.Models;

namespace StudentBazaar.Entities.Models
{
    public class Report : BaseEntity
    {
        [Required]
        public int ReporterId { get; set; }

        [ForeignKey(nameof(ReporterId))]
        public ApplicationUser Reporter { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string TargetType { get; set; } = string.Empty; // "Product" | "User" | "Message"

        public int? TargetId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public bool Resolved { get; set; } = false;

        [MaxLength(2000)]
        public string? Resolution { get; set; }

        public int? ResolvedById { get; set; }

        [ForeignKey(nameof(ResolvedById))]
        public ApplicationUser? ResolvedBy { get; set; }

        public DateTime? ResolvedAt { get; set; }
    }
}

