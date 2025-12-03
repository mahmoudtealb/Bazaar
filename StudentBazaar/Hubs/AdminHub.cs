

namespace StudentBazaar.Web.Hubs
{
    public class AdminHub : Hub
    {
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        }

        public async Task NotifyNewReport(int reportId)
        {
            await Clients.Group("Admins").SendAsync("NewReport", reportId);
        }

        public async Task NotifyNewProduct(int productId)
        {
            await Clients.Group("Admins").SendAsync("NewProduct", productId);
        }

        public async Task NotifyNewVerification(int verificationId)
        {
            await Clients.Group("Admins").SendAsync("NewVerification", verificationId);
        }

        public async Task NotifySuspiciousActivity(string message)
        {
            await Clients.Group("Admins").SendAsync("SuspiciousActivity", message);
        }

        public async Task NotifyNewOrder(int orderId)
        {
            await Clients.Group("Admins").SendAsync("NewOrder", orderId);
        }
    }
}

