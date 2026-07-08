using System.Collections.Concurrent;

namespace PrintNow.Web.Services
{
    /// <summary>
    /// Singleton service theo dõi người dùng đang hoạt động (online) in-memory.
    /// Không cần DB - mỗi request authenticated sẽ cập nhật last activity.
    /// </summary>
    public class ActiveUserTracker
    {
        private readonly ConcurrentDictionary<int, DateTime> _lastActivity = new();

        /// <summary>
        /// Ghi nhận hoạt động của user
        /// </summary>
        public void TrackUser(int userId)
        {
            _lastActivity[userId] = DateTime.UtcNow;
        }

        /// <summary>
        /// Đếm số user online trong khoảng minutes phút gần đây
        /// </summary>
        public int GetOnlineUserCount(int minutes = 15)
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-minutes);
            return _lastActivity.Values.Count(t => t >= cutoff);
        }

        /// <summary>
        /// Lấy số lượng user hoạt động theo từng giờ trong 24h qua (cho biểu đồ)
        /// Trả về mảng 24 phần tử, mỗi phần tử là số user online trong giờ đó
        /// </summary>
        public int[] GetHourlyActivity(int hours = 24)
        {
            var now = DateTime.UtcNow;
            var result = new int[hours];

            foreach (var time in _lastActivity.Values)
            {
                var diffHours = (int)(now - time).TotalHours;
                if (diffHours >= 0 && diffHours < hours)
                {
                    result[diffHours]++;
                }
            }

            return result;
        }

        /// <summary>
        /// Lấy số lượng user hoạt động theo từng ngày trong N ngày qua
        /// </summary>
        public int[] GetDailyActivity(int days = 14)
        {
            var now = DateTime.UtcNow.Date;
            var result = new int[days];

            foreach (var time in _lastActivity.Values)
            {
                var diffDays = (int)(now - time.Date).TotalDays;
                if (diffDays >= 0 && diffDays < days)
                {
                    result[diffDays]++;
                }
            }

            return result;
        }
    }
}
