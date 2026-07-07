using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PrintNow.Web.Data;
using PrintNow.Web.Models;
using System.Security.Claims;

namespace PrintNow.Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly PrintNowContext _context;

        public ChatHub(PrintNowContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) 
                            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                // Tự động tham gia nhóm mang tên User ID của chính mình khi kết nối
                await Groups.AddToGroupAsync(Context.ConnectionId, "user_" + userIdStr);
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string conversationId, string content)
        {
            var senderIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderIdStr)) return;
            var senderId = int.Parse(senderIdStr);

            var convId = Guid.Parse(conversationId);
            var conversation = await _context.Conversations
                .Include(c => c.Customer)
                .Include(c => c.Shop)
                .ThenInclude(s => s.Owner)
                .FirstOrDefaultAsync(c => c.Id == convId);

            if (conversation == null) return;

            // Verify sender is part of the conversation
            var senderUser = await _context.Users.FindAsync(senderId);
            if (senderUser == null) return;

            // Add new message
            var message = new Message
            {
                ConversationId = convId,
                SenderId = senderId,
                Content = content,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Phát tin nhắn tới nhóm của khách hàng và nhóm của chủ tiệm in
            var customerGroup = "user_" + conversation.CustomerId;
            var shopOwnerGroup = "user_" + conversation.Shop.OwnerId;

            await Clients.Groups(customerGroup, shopOwnerGroup).SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                conversationId = conversation.Id.ToString(),
                senderId = senderId,
                senderName = senderUser.FullName,
                content = content,
                createdAt = message.CreatedAt.ToLocalTime().ToString("HH:mm")
            });
        }
    }
}
