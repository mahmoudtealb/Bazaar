using StudentBazaar.Entities.Models;

namespace StudentBazaar.Entities.Models
{
    public class StudentVerification : BaseEntity
    {
        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string StudentIdNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? DocumentUrl { get; set; }

        public bool Approved { get; set; } = false;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ApprovedAt { get; set; }

        public int? ApprovedById { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        public ApplicationUser? ApprovedBy { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }
    }
}

