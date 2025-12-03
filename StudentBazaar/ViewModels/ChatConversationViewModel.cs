
namespace StudentBazaar.Web.ViewModels
{
    public class ChatConversationViewModel
    {
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;

        // مين اللي فاتح الشات الآن
        public int CurrentUserId { get; set; }

        // هل هو البائع ولا المشتري
        public bool IsSeller { get; set; }

        public IEnumerable<ChatMessage> Messages { get; set; } = Enumerable.Empty<ChatMessage>();

        [Required(ErrorMessage = "Message is required.")]
        [MaxLength(2000)]
        public string NewMessage { get; set; } = string.Empty;

        // مفتاح الجروب في SignalR
        public string ConversationKey { get; set; } = string.Empty;

        // List of all conversations for sidebar
        public List<ConversationSummary> AllConversations { get; set; } = new();
    }

    // عنصر في صفحة "محادثاتك"
    public class ChatConversationItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;

        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }

        public int UnreadCount { get; set; }
    }

    // صفحة "محادثاتك" فيها يمين/شمال
    public class ChatMyConversationsViewModel
    {
        public List<ChatConversationItemViewModel> AsBuyer { get; set; } = new();
        public List<ChatConversationItemViewModel> AsSeller { get; set; } = new();
    }

    // Summary for sidebar conversation list
    public class ConversationSummary
    {
        public int ProductId { get; set; }
        public int OtherUserId { get; set; }
        public string OtherUserName { get; set; } = string.Empty;
        public string OtherUserImage { get; set; } = string.Empty;
        public string LastMessage { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastMessageAt { get; set; }
    }
}
