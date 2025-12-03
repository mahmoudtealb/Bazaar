

namespace StudentBazaar.Entities.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        // ==========================
        // 🔹 Basic Info
        // ==========================
        [Required]
        [MaxLength(100)]
        [PersonalData]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(250)]
        [PersonalData]
        public string Address { get; set; } = string.Empty;

        [MaxLength(500)]
        [PersonalData]
        public string? ProfilePictureUrl { get; set; }

        // ==========================
        // 🔹 Admin & Verification
        // ==========================
        public int TrustScore { get; set; } = 0;

        public bool IsVerified { get; set; } = false;

        public bool IsSuspended { get; set; } = false;

        public DateTime? SuspendedUntil { get; set; }

        // ==========================
        // 🔹 Timestamps
        // ==========================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ==========================
        // 🔹 University & College (Optional for Admin)
        // ==========================
        public int? UniversityId { get; set; }   // ❗ Nullable now

        [ForeignKey(nameof(UniversityId))]
        public University? University { get; set; }  // ❗ Nullable navigation

        public int? CollegeId { get; set; }      // ❗ Nullable now

        [ForeignKey(nameof(CollegeId))]
        public College? College { get; set; }    // ❗ Nullable navigation

        // ==========================
        // 🔹 Reverse Relationships
        // ==========================
        public ICollection<Listing> ListingsPosted { get; set; } = new List<Listing>();
        public ICollection<Order> OrdersPlaced { get; set; } = new List<Order>();
        public ICollection<Rating> RatingsGiven { get; set; } = new List<Rating>();
        public ICollection<Shipment> ShipmentsHandled { get; set; } = new List<Shipment>();
        public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();

        public ICollection<ChatMessage> MessagesSent { get; set; } = new List<ChatMessage>();
        public ICollection<ChatMessage> MessagesReceived { get; set; } = new List<ChatMessage>();
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

        // Admin & Management
        public ICollection<Report> ReportsSubmitted { get; set; } = new List<Report>();
        public ICollection<Report> ReportsResolved { get; set; } = new List<Report>();
        public ICollection<StudentVerification> Verifications { get; set; } = new List<StudentVerification>();
        public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
