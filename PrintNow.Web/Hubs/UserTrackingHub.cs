using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PrintNow.Web.Data;
using PrintNow.Web.Services;
using System.Security.Claims;

namespace PrintNow.Web.Hubs
{
    public class UserTrackingHub : Hub
    {
        private readonly OnlineUserTracker _tracker;
        private readonly PrintNowContext _context;

        public UserTrackingHub(OnlineUserTracker tracker, PrintNowContext context)
        {
            _tracker = tracker;
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var currentUrl = httpContext?.Request.Query["currentUrl"].ToString() ?? "Unknown";
            var ipAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = httpContext?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            var connectionId = Context.ConnectionId;

            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                            ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            int? userId = null;
            string fullName = "Khách vãng lai";
            string email = "N/A";
            string role = "Guest";

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var parsedId))
            {
                userId = parsedId;
                var user = await _context.Users.FindAsync(parsedId);
                if (user != null)
                {
                    fullName = user.FullName;
                    email = user.Email;
                    role = user.Role;
                }
            }

            var info = new OnlineUserInfo
            {
                ConnectionId = connectionId,
                UserId = userId,
                FullName = fullName,
                Email = email,
                Role = role,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CurrentUrl = currentUrl,
                ConnectedAt = DateTime.UtcNow
            };

            _tracker.AddOrUpdateConnection(connectionId, info);

            // Notify all admins currently listening
            await Clients.Group("AdminGroup").SendAsync("UserConnected", info);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            if (_tracker.RemoveConnection(connectionId, out var removedInfo))
            {
                // Notify all admins currently listening
                await Clients.Group("AdminGroup").SendAsync("UserDisconnected", connectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task RegisterAdmin()
        {
            if (Context.User?.IsInRole("Admin") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
            }
        }
    }
}
