using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintNow.Web.Data;
using PrintNow.Web.Models;
using PrintNow.Web.Services;
using System.Security.Claims;

namespace PrintNow.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly PrintNowContext _context;
        private readonly ActiveUserTracker _activeUserTracker;
        private readonly OnlineUserTracker _onlineUserTracker;

        public AdminController(PrintNowContext context, ActiveUserTracker activeUserTracker, OnlineUserTracker onlineUserTracker)
        {
            _context = context;
            _activeUserTracker = activeUserTracker;
            _onlineUserTracker = onlineUserTracker;
        }

        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            // ========== THỐNG KÊ TIỆM IN ==========
            var totalShops = await _context.Shops.CountAsync();
            var activeShops = await _context.Shops.CountAsync(s => s.IsActive);
            var lockedShops = await _context.Shops.CountAsync(s => s.IsLocked);

            // ========== DOANH THU PHÍ SÀN ==========
            var totalPlatformFee = await _context.PlatformFees
                .Where(f => f.Status == "Paid")
                .SumAsync(f => f.Amount);

            var pendingFees = await _context.PlatformFees
                .CountAsync(f => f.Status == "Unpaid");

            var thisMonth = now.Month;
            var thisYear = now.Year;
            var thisMonthFeeRevenue = await _context.PlatformFees
                .Where(f => f.Month == thisMonth && f.Year == thisYear && f.Status == "Paid")
                .SumAsync(f => f.Amount);

            // ========== YÊU CẦU RÚT TIỀN ==========
            var pendingWithdrawals = await _context.WithdrawalRequests
                .CountAsync(w => w.Status == "Pending");

            var totalWithdrawn = await _context.WithdrawalRequests
                .Where(w => w.Status == "Approved")
                .SumAsync(w => w.Amount);

            // ========== NGƯỜI DÙNG ONLINE ==========
            var onlineUsers = _activeUserTracker.GetOnlineUserCount(15);

            // ========== THỐNG KÊ NGƯỜI DÙNG MỚI ==========
            var newUsersToday = await _context.Users
                .CountAsync(u => u.CreatedAt.Date == today);

            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);
            var newUsersThisWeek = await _context.Users
                .CountAsync(u => u.CreatedAt >= startOfWeek);

            var startOfMonth = new DateTime(thisYear, thisMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var newUsersThisMonth = await _context.Users
                .CountAsync(u => u.CreatedAt >= startOfMonth);

            // ========== XU HƯỚNG DOANH THU PHÍ SÀN (6 tháng gần nhất) ==========
            var revenueTrendLabels = new List<string>();
            var revenueTrendData = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var m = now.AddMonths(-i).Month;
                var y = now.AddMonths(-i).Year;
                var monthRevenue = await _context.PlatformFees
                    .Where(f => f.Month == m && f.Year == y && f.Status == "Paid")
                    .SumAsync(f => f.Amount);

                revenueTrendLabels.Add($"T{m}/{y}");
                revenueTrendData.Add(monthRevenue);
            }

            // ========== XU HƯỚNG NGƯỜI DÙNG MỚI (14 ngày gần nhất) ==========
            var userTrendLabels = new List<string>();
            var userTrendData = new List<int>();

            for (int i = 13; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var count = await _context.Users
                    .CountAsync(u => u.CreatedAt.Date == date);

                userTrendLabels.Add(date.Day.ToString());
                userTrendData.Add(count);
            }

            // ========== TỔNG DOANH THU ĐƠN HÀNG (6 tháng gần nhất) ==========
            var orderRevenueTrendData = new List<decimal>();

            for (int i = 5; i >= 0; i--)
            {
                var m = now.AddMonths(-i).Month;
                var y = now.AddMonths(-i).Year;
                var startOfMonthUtc = new DateTime(y, m, 1, 0, 0, 0, DateTimeKind.Utc);
                var endOfMonthUtc = startOfMonthUtc.AddMonths(1);

                var monthOrderRevenue = await _context.Orders
                    .Where(o => o.PaymentStatus == "Paid" && o.CreatedAt >= startOfMonthUtc && o.CreatedAt < endOfMonthUtc)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

                orderRevenueTrendData.Add(monthOrderRevenue);
            }

            // ========== HOẠT ĐỘNG NGƯỜI DÙNG THEO GIỜ (24h qua) ==========
            var hourlyActivity = _activeUserTracker.GetHourlyActivity(24);

            ViewBag.TotalShops = totalShops;
            ViewBag.ActiveShops = activeShops;
            ViewBag.LockedShops = lockedShops;
            ViewBag.TotalPlatformFee = totalPlatformFee;
            ViewBag.PendingFees = pendingFees;
            ViewBag.ThisMonthFeeRevenue = thisMonthFeeRevenue;
            ViewBag.PendingWithdrawals = pendingWithdrawals;
            ViewBag.TotalWithdrawn = totalWithdrawn;

            // Online users
            ViewBag.OnlineUsers = onlineUsers;

            // New users
            ViewBag.NewUsersToday = newUsersToday;
            ViewBag.NewUsersThisWeek = newUsersThisWeek;
            ViewBag.NewUsersThisMonth = newUsersThisMonth;
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // Charts
            ViewBag.RevenueTrendLabels = revenueTrendLabels;
            ViewBag.RevenueTrendData = revenueTrendData;
            ViewBag.OrderRevenueTrendData = orderRevenueTrendData;
            ViewBag.UserTrendLabels = userTrendLabels;
            ViewBag.UserTrendData = userTrendData;
            ViewBag.HourlyActivity = hourlyActivity;

            return View();
        }

        [HttpGet]
        public JsonResult GetOnlineUserCount()
        {
            var onlineUsers = _activeUserTracker.GetOnlineUserCount(15);
            return Json(new { onlineUsers });
        }

        public async Task<IActionResult> Shops()
        {
            var shops = await _context.Shops
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return View(shops);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleShopLock(int shopId, bool isLocked)
        {
            var shop = await _context.Shops.FindAsync(shopId);
            if (shop != null)
            {
                shop.IsLocked = isLocked;
                shop.IsActive = !isLocked;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Không tìm thấy tiệm in." });
        }

        public async Task<IActionResult> PlatformFees()
        {
            var fees = await _context.PlatformFees
                .Include(f => f.Shop)
                .OrderByDescending(f => f.Year)
                .ThenByDescending(f => f.Month)
                .ThenBy(f => f.Shop.ShopName)
                .ToListAsync();

            return View(fees);
        }

        [HttpPost]
        public async Task<IActionResult> GenerateMonthlyFees()
        {
            var now = DateTime.UtcNow;
            var month = now.Month;
            var year = now.Year;

            var activeShops = await _context.Shops
                .Where(s => s.IsActive)
                .ToListAsync();

            int generated = 0;
            foreach (var shop in activeShops)
            {
                var exists = await _context.PlatformFees
                    .AnyAsync(f => f.ShopId == shop.Id && f.Month == month && f.Year == year);

                if (!exists)
                {
                    _context.PlatformFees.Add(new PlatformFee
                    {
                        ShopId = shop.Id,
                        Month = month,
                        Year = year,
                        Amount = 199000m,
                        Status = "Unpaid"
                    });
                    generated++;
                }
            }

            if (generated > 0)
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo {generated} hóa đơn phí sàn cho tháng {month}/{year}!";
            }
            else
            {
                TempData["SuccessMessage"] = $"Tất cả tiệm đã có hóa đơn phí sàn tháng {month}/{year}.";
            }

            return RedirectToAction(nameof(PlatformFees));
        }

        [HttpPost]
        public async Task<IActionResult> MarkFeeAsPaid(int feeId)
        {
            var fee = await _context.PlatformFees.FindAsync(feeId);
            if (fee != null && fee.Status == "Unpaid")
            {
                fee.Status = "Paid";
                fee.PaidAt = DateTime.UtcNow;

                // Unlock shop
                var shop = await _context.Shops.FindAsync(fee.ShopId);
                if (shop != null)
                {
                    shop.IsLocked = false;
                    shop.IsActive = true;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã xác nhận thanh toán phí sàn tháng {fee.Month}/{fee.Year} cho tiệm!";
            }
            return RedirectToAction(nameof(PlatformFees));
        }

        public async Task<IActionResult> Withdrawals()
        {
            var withdrawals = await _context.WithdrawalRequests
                .Include(w => w.Shop)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(withdrawals);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveWithdrawal(int id, string? adminNote)
        {
            var withdrawal = await _context.WithdrawalRequests.FindAsync(id);
            if (withdrawal != null && withdrawal.Status == "Pending")
            {
                withdrawal.Status = "Approved";
                withdrawal.AdminNote = adminNote;
                withdrawal.ProcessedAt = DateTime.UtcNow;

                // Subtract from shop balance
                var shop = await _context.Shops.FindAsync(withdrawal.ShopId);
                if (shop != null)
                {
                    shop.Balance -= withdrawal.Amount;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã duyệt yêu cầu rút {withdrawal.Amount:N0}đ!";
            }
            return RedirectToAction(nameof(Withdrawals));
        }

        [HttpPost]
        public async Task<IActionResult> RejectWithdrawal(int id, string? adminNote)
        {
            var withdrawal = await _context.WithdrawalRequests.FindAsync(id);
            if (withdrawal != null && withdrawal.Status == "Pending")
            {
                withdrawal.Status = "Rejected";
                withdrawal.AdminNote = adminNote;
                withdrawal.ProcessedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã từ chối yêu cầu rút tiền!";
            }
            return RedirectToAction(nameof(Withdrawals));
        }

        [HttpGet]
        public IActionResult OnlineUsers()
        {
            var onlineUsers = _onlineUserTracker.GetOnlineUsers();
            return View(onlineUsers);
        }
    }
}
