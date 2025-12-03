

namespace StudentBazaar.Web.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IGenericRepository<ChatMessage> _chatRepo;
        private readonly IGenericRepository<Product> _productRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(
            IGenericRepository<ChatMessage> chatRepo,
            IGenericRepository<Product> productRepo,
            UserManager<ApplicationUser> userManager,
            IHubContext<ChatHub> hubContext)
        {
            _chatRepo = chatRepo;
            _productRepo = productRepo;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        private int GetCurrentUserId()
        {
            var idStr = _userManager.GetUserId(User);
            return int.Parse(idStr!);
        }

        // مفتاح الجروب لنفس المحادثة بين نفس الشخصين على نفس المنتج
        private string BuildConversationKey(int user1, int user2, int productId)
        {
            var a = Math.Min(user1, user2);
            var b = Math.Max(user1, user2);
            return $"chat-{a}-{b}-p{productId}";
        }

        // ==========================
        // 1) فتح محادثة مع صاحب المنتج (من زرار Buy / Contact)
        // ==========================
        [HttpGet]
        public async Task<IActionResult> WithSeller(int productId)
        {
            var product = await _productRepo.GetFirstOrDefaultAsync(
                p => p.Id == productId,
                includeWord: "Owner");

            if (product == null)
                return NotFound();

            if (product.OwnerId == null)
                return BadRequest("Product has no owner.");

            var currentUserId = GetCurrentUserId();
            var otherUserId = product.OwnerId.Value;

            var messages = await _chatRepo.GetAllAsync(
                m => m.ProductId == productId &&
                     ((m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                      (m.SenderId == otherUserId && m.ReceiverId == currentUserId)),
                includeWord: "Sender,Receiver");

            // Mark all unread messages as read when opening the conversation
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _chatRepo.SaveAsync();
            }

            var allConversations = await GetAllConversationsAsync(currentUserId);
            
            // Mark active conversation
            foreach (var conv in allConversations)
            {
                conv.IsActive = (conv.ProductId == productId && conv.OtherUserId == otherUserId);
            }

            var vm = new ChatConversationViewModel
            {
                ProductId = product.Id,
                Product = product,
                OtherUserId = otherUserId,
                OtherUserName = product.Owner.FullName,
                Messages = messages.OrderBy(m => m.SentAt).ToList(),
                CurrentUserId = currentUserId,
                IsSeller = (currentUserId == product.OwnerId),
                ConversationKey = BuildConversationKey(currentUserId, otherUserId, product.Id),
                AllConversations = allConversations
            };

            return View("Conversation", vm);
        }

        // Helper method to get all conversations for sidebar
        private async Task<List<ConversationSummary>> GetAllConversationsAsync(int currentUserId)
        {
            var allMessages = await _chatRepo.GetAllAsync(
                m => m.SenderId == currentUserId || m.ReceiverId == currentUserId,
                includeWord: "Product,Sender,Receiver");

            var conversations = allMessages
                .GroupBy(m => new
                {
                    m.ProductId,
                    OtherUserId = (m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                })
                .Select(g =>
                {
                    var last = g.OrderByDescending(x => x.SentAt).First();
                    var other = last.SenderId == currentUserId ? last.Receiver : last.Sender;
                    var unread = g.Count(x => x.ReceiverId == currentUserId && !x.IsRead);
                    var otherUserImage = !string.IsNullOrEmpty(other?.ProfilePictureUrl)
                        ? other.ProfilePictureUrl
                        : "https://bootdey.com/img/Content/avatar/avatar3.png";

                    return new ConversationSummary
                    {
                        ProductId = g.Key.ProductId ?? 0,
                        OtherUserId = g.Key.OtherUserId,
                        OtherUserName = other?.FullName ?? "User",
                        OtherUserImage = otherUserImage,
                        LastMessage = last.Content,
                        LastMessageAt = last.SentAt,
                        UnreadCount = unread,
                        IsActive = false // Will be set based on current view
                    };
                })
                .OrderByDescending(x => x.LastMessageAt)
                .ToList();

            return conversations;
        }

        // نفس شاشة المحادثة لكن تُفتح من صفحة "محادثاتك"
        [HttpGet]
        public async Task<IActionResult> Open(int? productId = null, int? otherUserId = null)
        {
            var currentUserId = GetCurrentUserId();
            var allConversations = await GetAllConversationsAsync(currentUserId);

            // If no parameters, show "Select a conversation" message
            if (!productId.HasValue || !otherUserId.HasValue)
            {
                // Show empty state with "Select a conversation" message
                var vm = new ChatConversationViewModel
                {
                    ProductId = 0,
                    Product = new Product { Name = "No Product", Price = 0 },
                    OtherUserId = 0,
                    OtherUserName = "",
                    Messages = new List<ChatMessage>(),
                    CurrentUserId = currentUserId,
                    IsSeller = false,
                    ConversationKey = "",
                    AllConversations = allConversations
                };
                return View("Conversation", vm);
            }

            var product = await _productRepo.GetFirstOrDefaultAsync(
                p => p.Id == productId.Value,
                includeWord: "Owner");

            if (product == null)
                return NotFound();

            var messages = await _chatRepo.GetAllAsync(
                m => m.ProductId == productId.Value &&
                     ((m.SenderId == currentUserId && m.ReceiverId == otherUserId.Value) ||
                      (m.SenderId == otherUserId.Value && m.ReceiverId == currentUserId)),
                includeWord: "Sender,Receiver");

            // Mark all unread messages as read when opening the conversation
            var unreadMessages = messages.Where(m => m.ReceiverId == currentUserId && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _chatRepo.SaveAsync();
                
                // Reload conversations to update unread counts
                allConversations = await GetAllConversationsAsync(currentUserId);
            }

            var otherUser = messages
                .Select(m => m.SenderId == currentUserId ? m.Receiver : m.Sender)
                .FirstOrDefault() ?? product.Owner;

            // Mark active conversation
            foreach (var conv in allConversations)
            {
                conv.IsActive = (conv.ProductId == productId.Value && conv.OtherUserId == otherUserId.Value);
            }

            var vm2 = new ChatConversationViewModel
            {
                ProductId = product.Id,
                Product = product,
                OtherUserId = otherUserId.Value,
                OtherUserName = otherUser?.FullName ?? "User",
                Messages = messages.OrderBy(m => m.SentAt).ToList(),
                CurrentUserId = currentUserId,
                IsSeller = (currentUserId == product.OwnerId),
                ConversationKey = BuildConversationKey(currentUserId, otherUserId.Value, product.Id),
                AllConversations = allConversations
            };

            return View("Conversation", vm2);
        }

        // ==========================
        // 2) إرسال رسالة  (AJAX + SignalR)
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send([FromForm] ChatConversationViewModel model)
        {
            var senderId = GetCurrentUserId();

            if (string.IsNullOrWhiteSpace(model.NewMessage))
            {
                // بدل ما نرمي BadRequest في وش المستخدم، نرجع JSON
                return Json(new { success = false, error = "Message is required." });
            }

            var message = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = model.OtherUserId,
                ProductId = model.ProductId,
                Content = model.NewMessage,
                SentAt = DateTime.UtcNow
            };

            await _chatRepo.AddAsync(message);
            await _chatRepo.SaveAsync();

            var convKey = BuildConversationKey(senderId, model.OtherUserId, model.ProductId);

            // Send to conversation group
            await _hubContext.Clients.Group(convKey).SendAsync("ReceiveMessage", new
            {
                productId = model.ProductId,
                senderId = senderId,
                receiverId = model.OtherUserId,
                content = message.Content,
                sentAt = message.SentAt.ToLocalTime().ToString("yyyy/MM/dd - hh:mm tt")
            });

            // Notify receiver about new unread message (for badge update)
            await _hubContext.Clients.User(model.OtherUserId.ToString()).SendAsync("NewUnreadMessage", new
            {
                fromUserId = senderId,
                toUserId = model.OtherUserId
            });

            return Json(new { success = true });
        }

        // ==========================
        // 3) صفحة "محادثاتك" - Redirected to Open
        // ==========================
        [HttpGet]
        public IActionResult MyConversations()
        {
            return RedirectToAction("Open");
        }

        // ==========================
        // 4) حذف المحادثة
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConversation(int productId, int otherUserId)
        {
            try
            {
                var currentUserId = GetCurrentUserId();

                // جلب جميع الرسائل في هذه المحادثة
                var messages = await _chatRepo.GetAllAsync(
                    m => m.ProductId == productId &&
                         ((m.SenderId == currentUserId && m.ReceiverId == otherUserId) ||
                          (m.SenderId == otherUserId && m.ReceiverId == currentUserId)));

                if (!messages.Any())
                {
                    TempData["Info"] = "No messages found to delete.";
                    return RedirectToAction("MyConversations");
                }

                // حذف جميع الرسائل
                _chatRepo.RemoveRange(messages);
                await _chatRepo.SaveAsync();

                TempData["Success"] = "Conversation deleted successfully.";
                return RedirectToAction("Open");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the conversation.";
                return RedirectToAction("Open");
            }
        }
    }
}
