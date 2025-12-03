
using System.ComponentModel;

namespace StudentBazaar.DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        // Core user-related
        public DbSet<ApplicationUser> Users { get; set; }

        // Academic structure
        public DbSet<University> Universities { get; set; }
        public DbSet<College> Colleges { get; set; }
        public DbSet<Major> Majors { get; set; }
    //    public DbSet<StudyYear> StudyYears { get; set; }

        // Product-related
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; } // إضافة: صور المنتج
        public DbSet<Listing> Listings { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        // E-commerce flow
        public DbSet<ShoppingCartItem> ShoppingCartItems { get; set; } // إضافة: سلة التسوق
        public DbSet<Cart> Carts { get; set; } // Cart entity
        public DbSet<OrderItem> OrderItems { get; set; } // Order items
                                                                       // This section defines the Fluent API configuration 
        public DbSet<ChatMessage> ChatMessages { get; set; }

        // Admin & Management
        public DbSet<Report> Reports { get; set; }
        public DbSet<StudentVerification> StudentVerifications { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // 🎓 العلاقات الأكاديمية (University / College / Major / StudyYear)
            // ==========================================

            modelBuilder.Entity<ChatMessage>()
    .HasOne(m => m.Product)
    .WithMany()
    .HasForeignKey(m => m.ProductId)
    .OnDelete(DeleteBehavior.Cascade);


            // University → Colleges (Cascade) / Users (Restrict)
            modelBuilder.Entity<University>()
                .HasMany(u => u.Colleges)
                .WithOne(c => c.University)
                .HasForeignKey(c => c.UniversityId)
                .OnDelete(DeleteBehavior.Cascade); // حذف الكليات إذا حُذفت الجامعة

            modelBuilder.Entity<University>()
                .HasMany(u => u.Users)
                .WithOne(u => u.University)
                .HasForeignKey(u => u.UniversityId)
                .OnDelete(DeleteBehavior.Restrict); // منع حذف الجامعة إذا بها مستخدمون

            // College → Users (Restrict) / Majors (Cascade)
            modelBuilder.Entity<College>()
                .HasMany(c => c.Users)
                .WithOne(u => u.College)
                .HasForeignKey(u => u.CollegeId)
                .OnDelete(DeleteBehavior.Restrict); // منع حذف الكلية لو بها طلاب

            modelBuilder.Entity<College>()
                .HasMany(c => c.Majors)
                .WithOne(m => m.College)
                .HasForeignKey(m => m.CollegeId)
                .OnDelete(DeleteBehavior.Cascade); // حذف التخصصات مع الكلية

            //// Major → StudyYears (Cascade)
            //modelBuilder.Entity<Major>()
            //    .HasMany(m => m.StudyYears)
            //    .WithOne(sy => sy.Major)
            //    .HasForeignKey(sy => sy.MajorId)
            //    .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // 🛍️ علاقات المنتج والبيع (Product / Listing / Rating / Category)
            // ==========================================

            // ProductCategory → Products (Cascade)
            modelBuilder.Entity<ProductCategory>()
                .HasMany(pc => pc.Products)
                .WithOne(p => p.Category)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product → Listings / Ratings / Images (Cascade)
            modelBuilder.Entity<Product>()
                .HasMany(p => p.Listings)
                .WithOne(l => l.Product)
                .HasForeignKey(l => l.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Ratings)
                .WithOne(r => r.Product)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasMany(p => p.Images)
                .WithOne(pi => pi.Product)
                .HasForeignKey(pi => pi.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // حذف صور المنتج مع المنتج

            // Listing → Orders (Cascade)
            modelBuilder.Entity<Listing>()
                .HasMany(l => l.Orders)
                .WithOne(o => o.Listing)
                .HasForeignKey(o => o.ListingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ShoppingCartItem → Listing (Cascade Delete)
            // Explicitly configure from ShoppingCartItem side to ensure CASCADE delete
            modelBuilder.Entity<ShoppingCartItem>()
                .HasOne(i => i.Listing)
                .WithMany(l => l.ShoppingCartItems)
                .HasForeignKey(i => i.ListingId)
                .OnDelete(DeleteBehavior.Cascade); // حذف عناصر السلة تلقائياً عند حذف Listing

            // ==========================================
            // 👤 علاقات المستخدم (User) مع باقي الكيانات
            // ==========================================

            // User → Listings (Seller)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.ListingsPosted)
                .WithOne(l => l.Seller)
                .HasForeignKey(l => l.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Orders (Buyer)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.OrdersPlaced)
                .WithOne(o => o.Buyer)
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Ratings (Restrict)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.RatingsGiven)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Shipments (Restrict)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.ShipmentsHandled)
                .WithOne(s => s.Shipper)
                .HasForeignKey(s => s.ShipperId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → ShoppingCartItems (Cascade)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.ShoppingCartItems)
                .WithOne(sci => sci.User)
                .HasForeignKey(sci => sci.UserId)
                .OnDelete(DeleteBehavior.Cascade); // حذف محتوى السلة لو المستخدم اتحذف

            // User → Carts (Cascade)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany<Cart>()
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product → Carts (Restrict)
            modelBuilder.Entity<Product>()
                .HasMany<Cart>()
                .WithOne(c => c.Product)
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order → OrderItems (Cascade)
            modelBuilder.Entity<Order>()
                .HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Product → OrderItems (Restrict)
            modelBuilder.Entity<Product>()
                .HasMany<OrderItem>()
                .WithOne(oi => oi.Product)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // 🚚 علاقات الشحن (Order / Shipment)
            // ==========================================

            // Order ↔ Shipment (1:1)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Shipment)
                .WithOne(s => s.Order)
                .HasForeignKey<Shipment>(s => s.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==========================================
            // 🔒 Unique Constraints (منع التكرار)
            // ==========================================

            // منع تكرار الإيميل
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // منع تكرار اسم التصنيف
            modelBuilder.Entity<ProductCategory>()
                .HasIndex(pc => pc.CategoryName)
                .IsUnique();

            // منع تكرار اسم الجامعة
            modelBuilder.Entity<University>()
                .HasIndex(u => u.UniversityName)
                .IsUnique();

            // منع تكرار اسم الكلية داخل نفس الجامعة
            modelBuilder.Entity<College>()
                .HasIndex(c => new { c.CollegeName, c.UniversityId })
                .IsUnique();

            // علاقة الرسائل المرسلة
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.MessagesSent)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // علاقة الرسائل المستلمة
            modelBuilder.Entity<ChatMessage>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.MessagesReceived)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==========================================
            // 🔹 Admin & Management Relationships
            // ==========================================

            // User → Reports (Reporter)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.ReportsSubmitted)
                .WithOne(r => r.Reporter)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Reports (ResolvedBy)
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.ReportsResolved)
                .WithOne(r => r.ResolvedBy)
                .HasForeignKey(r => r.ResolvedById)
                .OnDelete(DeleteBehavior.Restrict);

            // User → StudentVerifications
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Verifications)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → ActivityLogs
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.ActivityLogs)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Notifications
            modelBuilder.Entity<ApplicationUser>()
                .HasMany(u => u.Notifications)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order → Transactions
            modelBuilder.Entity<Order>()
                .HasMany(o => o.Transactions)
                .WithOne(t => t.Order)
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

        }

        internal void UpdateCategoryAttribute(CategoryAttribute categoryAttribute)
        {
            throw new NotImplementedException();
        }
    }
}