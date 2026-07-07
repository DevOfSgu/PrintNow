using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintNow.Web.Data;
using PrintNow.Web.Models;
using System.Security.Claims;

namespace PrintNow.Web.Controllers
{
    [Authorize(Roles = "ShopOwner")]
    public class ShopAdminController : Controller
    {
        private readonly PrintNowContext _context;

        public ShopAdminController(PrintNowContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (shop == null)
            {
                return RedirectToAction("Index", "Home"); // Fallback
            }

            // Check if location is set
            if (shop.Latitude == null || shop.Longitude == null)
            {
                TempData["WarningMessage"] = "Vui lòng cấu hình vị trí tiệm in trên bản đồ trước khi bắt đầu sử dụng!";
                return RedirectToAction(nameof(Settings));
            }

            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
            if (today.DayOfWeek == DayOfWeek.Sunday) startOfWeek = startOfWeek.AddDays(-7);
            
            // 1. Thống kê số (Đơn hàng mới, Doanh thu, File chờ in)
            var newOrdersToday = await _context.Orders
                .Where(o => o.ShopId == shop.Id && o.Status != "Cart" && o.CreatedAt.Date == today)
                .CountAsync();
            var newOrdersYesterday = await _context.Orders
                .Where(o => o.ShopId == shop.Id && o.Status != "Cart" && o.CreatedAt.Date == yesterday)
                .CountAsync();

            var revenueToday = await _context.Orders
                .Where(o => o.ShopId == shop.Id && o.PaymentStatus == "Paid" && o.CreatedAt.Date == today)
                .SumAsync(o => o.TotalAmount);
            var revenueYesterday = await _context.Orders
                .Where(o => o.ShopId == shop.Id && o.PaymentStatus == "Paid" && o.CreatedAt.Date == yesterday)
                .SumAsync(o => o.TotalAmount);

            var pendingFilesCount = await _context.Orders
                .Where(o => o.ShopId == shop.Id && o.Status == "Pending")
                .SelectMany(o => o.OrderDetails)
                .CountAsync();

            // Tính % thay đổi
            double orderGrowth = newOrdersYesterday == 0 ? (newOrdersToday > 0 ? 100 : 0) : Math.Round((double)(newOrdersToday - newOrdersYesterday) / newOrdersYesterday * 100, 1);
            double revenueGrowth = revenueYesterday == 0 ? (revenueToday > 0 ? 100 : 0) : Math.Round((double)(revenueToday - revenueYesterday) / (double)revenueYesterday * 100, 1);

            ViewBag.NewOrders = newOrdersToday;
            ViewBag.OrderGrowth = orderGrowth;
            
            ViewBag.Revenue = revenueToday >= 1000000 ? (revenueToday / 1000000m).ToString("0.##") + "M" : revenueToday.ToString("#,##0");
            ViewBag.RevenueGrowth = revenueGrowth;
            
            ViewBag.PendingFiles = pendingFilesCount;

            // 2. Dữ liệu biểu đồ (Thống kê đơn hàng và doanh thu trong tuần)
            var recentOrdersForChart = await _context.Orders
                .Where(o => o.ShopId == shop.Id && o.Status != "Cart" && o.CreatedAt >= startOfWeek)
                .ToListAsync();

            // Mảng 7 ngày (T2 - CN)
            var orderCounts = new int[7];
            var revenues = new decimal[7];

            foreach (var order in recentOrdersForChart)
            {
                var dayIndex = (int)order.CreatedAt.DayOfWeek - 1;
                if (dayIndex < 0) dayIndex = 6; // Sunday is 0, make it 6

                orderCounts[dayIndex]++;
                if (order.PaymentStatus == "Paid")
                {
                    revenues[dayIndex] += order.TotalAmount;
                }
            }

            ViewBag.ChartOrderCounts = orderCounts;
            ViewBag.ChartRevenues = revenues;

            // 3. Đơn hàng gần đây (10 đơn mới nhất)
            var recentOrders = await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.ShopId == shop.Id && o.Status != "Cart")
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewBag.RecentOrders = recentOrders;

            return View(shop);
        }



        public async Task<IActionResult> Orders()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            
            if (shop == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .Where(o => o.ShopId == shop.Id && o.Status != "Cart")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ShopId == shop.Id);
            if (order != null && order.Status == "Pending")
            {
                order.Status = "Printing";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã nhận đơn hàng #{id}!";
            }
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        public async Task<IActionResult> CompletePrinting(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ShopId == shop.Id);
            if (order != null && order.Status == "Printing")
            {
                order.Status = "Ready";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đơn hàng #{id} đã in xong và sẵn sàng bàn giao!";
            }
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ShopId == shop.Id);
            if (order != null && order.Status == "Ready")
            {
                order.Status = "Completed";
                order.PaymentStatus = "Paid"; // Đánh dấu đã thanh toán khi giao hàng
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hoàn thành đơn hàng #{id}!";
            }
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id, string cancelReason)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ShopId == shop.Id);
            if (order != null)
            {
                order.Status = "Cancelled";
                order.CancelReason = string.IsNullOrWhiteSpace(cancelReason) ? "Cửa hàng hủy đơn" : cancelReason;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{id}!";
            }
            return RedirectToAction(nameof(Orders));
        }

        [HttpGet]
        public async Task<IActionResult> Services()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops
                .Include(s => s.Services)
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (shop == null) return RedirectToAction("Index", "Home");

            // Seed default core services if missing
            var coreServices = new List<(string Code, string Name, decimal Price)>
            {
                ("A4_BW", "In đen trắng A4 (1 mặt)", 500m),
                ("A4_COLOR", "In màu A4 (1 mặt)", 1500m),
                ("A3_BW", "In đen trắng A3", 1000m),
                ("A3_COLOR", "In màu A3", 3000m),
                ("A5_BW", "In đen trắng A5", 400m),
                ("A5_COLOR", "In màu A5", 1200m)
            };

            bool changed = false;
            foreach (var item in coreServices)
            {
                if (!shop.Services.Any(s => s.ServiceCode == item.Code))
                {
                    var newService = new Service
                    {
                        ShopId = shop.Id,
                        ServiceCode = item.Code,
                        ServiceName = item.Name,
                        BasePrice = item.Price,
                        Unit = "trang",
                        IsActive = true,
                        Type = "Core"
                    };
                    _context.Services.Add(newService);
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
                await _context.Entry(shop).Collection(s => s.Services).LoadAsync();
            }

            return View(shop.Services.ToList());
        }

        [HttpPost]
        public async Task<IActionResult> UpdateServicePrice(int id, decimal price)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Json(new { success = false, message = "Không tìm thấy tiệm in." });

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.ShopId == shop.Id);
            if (service == null) return Json(new { success = false, message = "Không tìm thấy dịch vụ." });

            service.BasePrice = price;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleServiceStatus(int id, bool isActive)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Json(new { success = false, message = "Không tìm thấy tiệm in." });

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.ShopId == shop.Id);
            if (service == null) return Json(new { success = false, message = "Không tìm thấy dịch vụ." });

            service.IsActive = isActive;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddAddonService(string serviceName, decimal basePrice, string unit)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Json(new { success = false, message = "Không tìm thấy tiệm in." });

            var service = new Service
            {
                ShopId = shop.Id,
                ServiceName = serviceName,
                BasePrice = basePrice,
                Unit = unit ?? "trang",
                IsActive = true,
                Type = "AddOn"
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = service.Id, name = service.ServiceName, price = service.BasePrice.ToString("N0") + " VNĐ / " + service.Unit });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteService(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            if (shop == null) return Json(new { success = false, message = "Không tìm thấy tiệm in." });

            var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.ShopId == shop.Id);
            if (service == null) return Json(new { success = false, message = "Không tìm thấy dịch vụ." });

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.OwnerId == userId);
            
            if (shop == null) return NotFound();

            return View(shop);
        }

        [HttpPost]
        public async Task<IActionResult> Settings(Shop updatedShop, string phone, string email, IFormFile? imageFile)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.OwnerId == userId);

            if (shop != null)
            {
                shop.ShopName = updatedShop.ShopName;
                shop.AddressText = updatedShop.AddressText ?? string.Empty;
                shop.Latitude = updatedShop.Latitude;
                shop.Longitude = updatedShop.Longitude;
                shop.Description = updatedShop.Description;
                shop.OperatingHoursText = updatedShop.OperatingHoursText;

                if (shop.Owner != null)
                {
                    shop.Owner.Phone = phone ?? string.Empty;
                    shop.Owner.Email = email ?? string.Empty;
                }

                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "shops", fileName);
                    
                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    shop.ImageUrl = "/images/shops/" + fileName;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }

            return RedirectToAction(nameof(Settings));
        }

        [HttpGet]
        public async Task<IActionResult> Messages(int? customerId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
            
            if (shop == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (customerId.HasValue)
            {
                var customerExists = await _context.Users.AnyAsync(u => u.Id == customerId.Value && u.Role == "Customer");
                if (customerExists)
                {
                    var existingConv = await _context.Conversations
                        .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value && c.ShopId == shop.Id);

                    if (existingConv == null)
                    {
                        var newConv = new Conversation
                        {
                            CustomerId = customerId.Value,
                            ShopId = shop.Id
                        };
                        _context.Conversations.Add(newConv);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            var conversations = await _context.Conversations
                .Include(c => c.Customer)
                .Include(c => c.Messages)
                .Where(c => c.ShopId == shop.Id)
                .OrderByDescending(c => c.Messages.Any() ? c.Messages.Max(m => m.CreatedAt) : c.CreatedAt)
                .ToListAsync();

            Guid? activeConversationId = null;
            if (customerId.HasValue)
            {
                activeConversationId = conversations.FirstOrDefault(c => c.CustomerId == customerId.Value)?.Id;
            }
            else
            {
                activeConversationId = conversations.FirstOrDefault()?.Id;
            }

            ViewBag.Conversations = conversations;
            ViewBag.ActiveConversationId = activeConversationId;

            if (activeConversationId.HasValue)
            {
                var activeConv = await _context.Conversations
                    .Include(c => c.Customer)
                    .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                    .FirstOrDefaultAsync(c => c.Id == activeConversationId.Value);

                // Mark messages from the customer as read
                var unreadMessages = activeConv?.Messages
                    .Where(m => m.SenderId != userId && !m.IsRead)
                    .ToList();
                if (unreadMessages != null && unreadMessages.Any())
                {
                    unreadMessages.ForEach(m => m.IsRead = true);
                    await _context.SaveChangesAsync();
                }

                return View(activeConv);
            }

            return View(null as Conversation);
        }
    }
}
