using System.Collections.Concurrent;

namespace PrintNow.Web.Services
{
    public class OnlineUserInfo
    {
        public string ConnectionId { get; set; } = string.Empty;
        public int? UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string CurrentUrl { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    }

    public class OnlineUserTracker
    {
        private readonly ConcurrentDictionary<string, OnlineUserInfo> _connections = new();

        public void AddOrUpdateConnection(string connectionId, OnlineUserInfo info)
        {
            _connections[connectionId] = info;
        }

        public bool RemoveConnection(string connectionId, out OnlineUserInfo? removedInfo)
        {
            var result = _connections.TryRemove(connectionId, out var info);
            removedInfo = info;
            return result;
        }

        public List<OnlineUserInfo> GetOnlineUsers()
        {
            return _connections.Values.OrderByDescending(u => u.ConnectedAt).ToList();
        }
    }
}
