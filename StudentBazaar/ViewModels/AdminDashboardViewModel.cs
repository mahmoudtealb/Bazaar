
namespace StudentBazaar.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalSold { get; set; }
        public int TotalMessages { get; set; }
        public int TotalOrders { get; set; }
        public int PendingReports { get; set; }
        public int PendingVerifications { get; set; }
        public decimal TotalRevenue { get; set; }
        public List<Product> RecentProducts { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();
        public List<Report> RecentReports { get; set; } = new();
    }
}

