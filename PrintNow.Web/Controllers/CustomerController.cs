using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PrintNow.Web.Data;
using System.Security.Claims;
using PrintNow.Web.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using PdfSharp.Pdf.IO;
using NPOI.XWPF.UserModel;

namespace PrintNow.Web.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly PrintNowContext _context;
        private readonly IConfiguration _configuration;

        public CustomerController(PrintNowContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Orders()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            var orders = await _context.Orders
                .Include(o => o.Shop)
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerId == userId && o.Status != "Cart")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.ReviewedOrderIds = await _context.Reviews
                .Where(r => r.CustomerId == userId)
                .Select(r => r.OrderId)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> Messages(int? shopId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            if (shopId.HasValue)
            {
                var shopExists = await _context.Shops.AnyAsync(s => s.Id == shopId);
                if (shopExists)
                {
                    var existingConv = await _context.Conversations
                        .FirstOrDefaultAsync(c => c.CustomerId == userId && c.ShopId == shopId.Value);

                    if (existingConv == null)
                    {
                        var newConv = new Conversation
                        {
                            CustomerId = userId,
                            ShopId = shopId.Value
                        };
                        _context.Conversations.Add(newConv);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            var conversations = await _context.Conversations
                .Include(c => c.Shop)
                .Include(c => c.Messages)
                .Where(c => c.CustomerId == userId)
                .OrderByDescending(c => c.Messages.Any() ? c.Messages.Max(m => m.CreatedAt) : c.CreatedAt)
                .ToListAsync();

            Guid? activeConversationId = null;
            if (shopId.HasValue)
            {
                activeConversationId = conversations.FirstOrDefault(c => c.ShopId == shopId.Value)?.Id;
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
                    .Include(c => c.Shop)
                    .Include(c => c.Messages)
                    .ThenInclude(m => m.Sender)
                    .FirstOrDefaultAsync(c => c.Id == activeConversationId.Value);

                // Mark messages from the shop as read
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

        [HttpGet]
        public async Task<IActionResult> GetNearbyShops(double lat, double lng, double radiusInKm = 10)
        {
            // Simple bounding box approach for fast querying (approximate)
            double latOffset = radiusInKm / 111.0;
            double lngOffset = radiusInKm / (111.0 * Math.Cos(lat * Math.PI / 180.0));

            double minLat = lat - latOffset;
            double maxLat = lat + latOffset;
            double minLng = lng - lngOffset;
            double maxLng = lng + lngOffset;

            var now = DateTime.UtcNow;
            var shopsRaw = await _context.Shops
                .Include(s => s.Reviews)
                .Where(s => s.IsActive && !s.IsLocked && s.Latitude != null && s.Longitude != null)
                .Where(s => s.BankAccountName != null && s.BankAccountNumber != null && s.BankAccountName != "" && s.BankAccountNumber != "")
                .Where(s => s.Latitude >= minLat && s.Latitude <= maxLat && s.Longitude >= minLng && s.Longitude <= maxLng)
                .ToListAsync();

            // Lọc shop đã đóng phí sàn tháng hiện tại
            var hasFeeRecords = await _context.PlatformFees
                .AnyAsync(f => f.Month == now.Month && f.Year == now.Year);

            if (hasFeeRecords)
            {
                // Chỉ lọc nếu đã có hóa đơn phí sàn tháng này (tránh mất hết shop khi admin chưa tạo phí)
                var paidShopIds = await _context.PlatformFees
                    .Where(f => f.Month == now.Month && f.Year == now.Year && f.Status == "Paid")
                    .Select(f => f.ShopId)
                    .ToListAsync();

                shopsRaw = shopsRaw.Where(s => paidShopIds.Contains(s.Id)).ToList();
            }

            var shops = shopsRaw.Select(s => new
            {
                id = s.Id,
                name = s.ShopName,
                address = s.AddressText,
                imageUrl = s.ImageUrl,
                lat = s.Latitude,
                lng = s.Longitude,
                rating = s.Reviews.Any() ? Math.Round(s.Reviews.Average(r => r.Rating), 1) : 0.0,
                reviewCount = s.Reviews.Count,
                distance = Math.Round(CalculateHaversineDistance(lat, lng, s.Latitude!.Value, s.Longitude!.Value), 1)
            })
            .OrderBy(s => s.distance)
            .ToList();

            return Json(shops);
        }

        [HttpGet]
        public async Task<IActionResult> ShopDetail(int id, double? lat, double? lng)
        {
            await EnsureCoreServicesSeeded(id);

            var shop = await _context.Shops
                .Include(s => s.Owner)
                .Include(s => s.Services)
                .Include(s => s.Reviews).ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shop == null) return NotFound();

            // Check if shop is locked due to unpaid platform fee
            if (shop.IsLocked)
            {
                ViewBag.IsShopLocked = true;
            }

            ViewBag.UserLat = lat;
            ViewBag.UserLng = lng;
            
            double distance = 0;
            if (lat.HasValue && lng.HasValue && shop.Latitude.HasValue && shop.Longitude.HasValue)
            {
                distance = CalculateHaversineDistance(lat.Value, lng.Value, shop.Latitude.Value, shop.Longitude.Value);
            }
            ViewBag.Distance = Math.Round(distance, 1);

            return View(shop);
        }

        [HttpGet]
        public async Task<IActionResult> OrderConfig(int id)
        {
            await EnsureCoreServicesSeeded(id);

            var shop = await _context.Shops
                .Include(s => s.Services)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (shop == null) return NotFound();

            // Check if shop is locked
            if (shop.IsLocked)
            {
                TempData["ErrorMessage"] = "Tiệm in này hiện đang tạm khóa. Vui lòng quay lại sau.";
                return RedirectToAction("ShopDetail", new { id = id });
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdStr != null)
            {
                var userId = int.Parse(userIdStr);
                var cartOrder = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.CustomerId == userId && o.ShopId == id && o.Status == "Cart");
                ViewBag.CartOrder = cartOrder;
            }

            return View(shop);
        }

        private int CalculatePageCount(string filePath, string extension)
        {
            try
            {
                if (extension == ".pdf")
                {
                    using (var pdfDoc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import))
                    {
                        return pdfDoc.PageCount;
                    }
                }
                else if (extension == ".docx")
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        var doc = new XWPFDocument(stream);
                        return doc.GetProperties().ExtendedProperties.GetUnderlyingProperties().Pages;
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return 1;
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ParsePageCount(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, pageCount = 1 });
            }

            try
            {
                var ext = Path.GetExtension(file.FileName).ToLower();
                int pageCount = 1;

                using (var stream = file.OpenReadStream())
                {
                    if (ext == ".pdf")
                    {
                        using (var pdfDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import))
                        {
                            pageCount = pdfDoc.PageCount;
                        }
                    }
                    else if (ext == ".docx")
                    {
                        var doc = new XWPFDocument(stream);
                        pageCount = doc.GetProperties().ExtendedProperties.GetUnderlyingProperties().Pages;
                    }
                }

                if (pageCount <= 0) pageCount = 1;
                return Json(new { success = true, pageCount = pageCount });
            }
            catch
            {
                return Json(new { success = false, pageCount = 1 });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int shopId, string paperSize, bool isColor, int sides, int quantity, string note, int[]? selectedAddons, IFormFile file, int totalPages = 1)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            // Xử lý lưu file
            string fileUrl = "";
            string fileName = "";
            int calculatedPages = 1;
            if (file != null && file.Length > 0)
            {
                fileName = file.FileName;
                var ext = Path.GetExtension(file.FileName).ToLower();
                var uniqueName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "orders", uniqueName);
                
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory!);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                fileUrl = "/uploads/orders/" + uniqueName;

                calculatedPages = CalculatePageCount(filePath, ext);
            }

            // Tìm cấu hình giá in tài liệu cơ bản từ database
            var serviceCode = $"{paperSize}_{(isColor ? "COLOR" : "BW")}";
            var coreService = await _context.Services.FirstOrDefaultAsync(s => s.ShopId == shopId && s.ServiceCode == serviceCode && s.IsActive);
            
            if (coreService == null)
            {
                // Fallback nếu không cấu hình
                coreService = await _context.Services.FirstOrDefaultAsync(s => s.ShopId == shopId && s.Type == "Core" && s.IsActive);
            }

            decimal basePrice = coreService?.BasePrice ?? 500m;
            int serviceId = coreService?.Id ?? 0;

            // Hệ số in 2 mặt (mặc định nhân 1.5 nếu in 2 mặt)
            var sideMultiplier = sides == 2 ? 1.5m : 1m;
            var unitPrice = basePrice * sideMultiplier;
            
            // Số trang cuối cùng
            int finalPages = totalPages > 0 ? totalPages : calculatedPages;
            if (finalPages <= 0) finalPages = 1;

            var basePrintCost = unitPrice * finalPages * quantity;

            // Tính tiền tiện ích gia công đi kèm
            decimal addonCost = 0m;
            var addonDescriptions = new List<string>();
            if (selectedAddons != null && selectedAddons.Length > 0)
            {
                var addons = await _context.Services
                    .Where(s => selectedAddons.Contains(s.Id) && s.ShopId == shopId && s.Type == "AddOn" && s.IsActive)
                    .ToListAsync();

                foreach (var addon in addons)
                {
                    decimal singleAddonCost = addon.BasePrice * quantity;
                    addonCost += singleAddonCost;
                    addonDescriptions.Add($"{addon.ServiceName} (+{singleAddonCost.ToString("N0")}đ)");
                }
            }

            var printCost = basePrintCost + addonCost;

            // Ghi chú & danh sách gia công đi kèm
            var addonListStr = string.Join(", ", addonDescriptions);
            if (!string.IsNullOrEmpty(note))
            {
                addonListStr = string.IsNullOrEmpty(addonListStr) ? note : $"{addonListStr} | Ghi chú: {note}";
            }

            // Kiểm tra xem đã có Order nào trạng thái "Cart" cho Shop này chưa
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.CustomerId == userId && o.ShopId == shopId && o.Status == "Cart");
            
            if (order == null)
            {
                order = new Order
                {
                    CustomerId = userId,
                    ShopId = shopId,
                    TotalAmount = printCost,
                    Status = "Cart",
                    PaymentStatus = "Unpaid"
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }
            else
            {
                order.TotalAmount += printCost;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }

            // Tạo OrderDetail (File vừa tải lên)
            var detail = new OrderDetail
            {
                OrderId = order.Id,
                ServiceId = serviceId,
                FileUrl = fileUrl,
                FileName = fileName,
                Quantity = quantity,
                TotalPages = finalPages,
                PaperSize = paperSize,
                IsColor = isColor,
                Sides = sides,
                SubTotal = printCost,
                AddOnList = string.IsNullOrEmpty(addonListStr) ? null : addonListStr
            };

            _context.OrderDetails.Add(detail);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã thêm file vào giỏ hàng!";
            return RedirectToAction("OrderConfig", new { id = shopId });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int detailId, string returnUrl)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Find the detail and ensure it belongs to an order of this user which is in "Cart" status
            var detail = await _context.OrderDetails
                .Include(d => d.Order)
                .FirstOrDefaultAsync(d => d.Id == detailId && d.Order.CustomerId == userId && d.Order.Status == "Cart");

            if (detail != null)
            {
                var order = detail.Order;
                
                // Subtract the subtotal from the order's total amount
                order.TotalAmount -= detail.SubTotal;
                
                _context.OrderDetails.Remove(detail);
                
                // If there are no other details in this order, we could remove the order too,
                // but since we only check after saving, we can just do it here.
                var otherDetailsCount = await _context.OrderDetails.CountAsync(d => d.OrderId == order.Id && d.Id != detailId);
                
                if (otherDetailsCount == 0)
                {
                    _context.Orders.Remove(order);
                }
                else
                {
                    _context.Orders.Update(order);
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa tệp khỏi giỏ hàng.";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Cart");
        }

        [HttpGet]
        public async Task<IActionResult> Cart()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Lấy tất cả các giỏ hàng (Cart) của User này, Include chi tiết và tên Tiệm
            var carts = await _context.Orders
                .Include(o => o.Shop)
                .Include(o => o.OrderDetails)
                .Where(o => o.CustomerId == userId && o.Status == "Cart")
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(carts);
        }

        [HttpGet]
        public async Task<IActionResult> Checkout(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders
                .Include(o => o.Shop)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId && o.Status == "Cart");

            if (order == null)
            {
                return RedirectToAction("Cart");
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCheckout(int orderId, string deliveryMethod, string? shippingAddress, string paymentMethod)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId && o.Status == "Cart");

            if (order != null)
            {
                order.DeliveryMethod = deliveryMethod;
                order.PaymentMethod = paymentMethod;
                
                if (deliveryMethod == "Delivery")
                {
                    order.ShippingAddress = shippingAddress ?? string.Empty;
                    order.ShippingFee = 20000m;
                    order.TotalAmount += 20000m;
                }
                else
                {
                    order.ShippingAddress = null;
                    order.ShippingFee = 0m;
                }

                order.Status = "Pending";
                order.CreatedAt = DateTime.UtcNow;
                order.PaymentStatus = paymentMethod == "PayOS" ? "Unpaid" : "Unpaid";
                
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderInfo(int orderId, string deliveryMethod, string? shippingAddress, string paymentMethod)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId && o.Status == "Cart");

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
            }

            order.DeliveryMethod = deliveryMethod;
            order.PaymentMethod = paymentMethod;

            var baseAmount = order.TotalAmount - order.ShippingFee;
            if (deliveryMethod == "Delivery")
            {
                order.ShippingAddress = shippingAddress ?? string.Empty;
                order.ShippingFee = 20000m;
                order.TotalAmount = baseAmount + 20000m;
            }
            else
            {
                order.ShippingAddress = null;
                order.ShippingFee = 0m;
                order.TotalAmount = baseAmount;
            }

            order.Status = "Pending";
            order.CreatedAt = DateTime.UtcNow;
            order.PaymentStatus = "Unpaid";

            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order != null)
            {
                // Check transaction status
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == orderId && t.TransactionType == "OrderPayment");

                if (transaction != null)
                {
                    if (transaction.Status == "Completed")
                    {
                        order.PaymentStatus = "Paid";
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Thanh toán thành công! Đơn hàng của bạn đã được xác nhận.";
                    }
                    else
                    {
                        // Fallback: check PayOS directly
                        try
                        {
                            var payOS = HttpContext.RequestServices.GetRequiredService<PayOSClient>();
                            var paymentInfo = await payOS.PaymentRequests.GetPaymentLinkInfoAsync(long.Parse(transaction.PayOSOrderCode));
                            if (paymentInfo.Status == "PAID")
                            {
                                transaction.Status = "Completed";
                                transaction.CompletedAt = DateTime.UtcNow;
                                order.PaymentStatus = "Paid";

                                // Cộng doanh thu vào shop
                                var platformFeePercent = _configuration.GetValue<decimal>("PayOS:PlatformFeePercent");
                                if (platformFeePercent <= 0) platformFeePercent = 0.10m;
                                var shop = await _context.Shops.FindAsync(order.ShopId);
                                if (shop != null)
                                {
                                    shop.Balance += order.TotalAmount * (1 - platformFeePercent);
                                }

                                await _context.SaveChangesAsync();
                                TempData["SuccessMessage"] = "Thanh toán thành công! Đơn hàng của bạn đã được xác nhận.";
                            }
                            else
                            {
                                TempData["SuccessMessage"] = $"Đặt hàng thành công! Đơn hàng #{order.Id} đang chờ xác nhận thanh toán.";
                            }
                        }
                        catch
                        {
                            TempData["SuccessMessage"] = $"Đặt hàng thành công! Đơn hàng #{order.Id} đang chờ xác nhận thanh toán.";
                        }
                    }
                }
                else
                {
                    TempData["SuccessMessage"] = $"Đặt hàng thành công! Đơn hàng #{order.Id} đã được gửi đến tiệm in.";
                }
            }

            return RedirectToAction("Orders");
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCancel(int orderId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order != null)
            {
                // Revert status back to Cart if payment was cancelled
                order.Status = "Cart";
                order.PaymentStatus = "Unpaid";
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Thanh toán đã bị hủy. Bạn có thể thử lại.";
            return RedirectToAction("Cart");
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

            if (order == null)
            {
                return NotFound();
            }

            if (order.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng đã được xác nhận hoặc đang xử lý.";
                return RedirectToAction(nameof(Orders));
            }

            order.Status = "Cancelled";
            order.CancelReason = "Khách hàng tự hủy đơn";
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã hủy đơn hàng #{id} thành công!";
            return RedirectToAction(nameof(Orders));
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var R = 6371; // Bán kính trái đất tính bằng km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        [HttpGet]
        public async Task<IActionResult> CreateReview(int orderId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var order = await _context.Orders
                .Include(o => o.Shop)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null || order.Status != "Completed")
            {
                return RedirectToAction("Orders");
            }

            var alreadyReviewed = await _context.Reviews.AnyAsync(r => r.OrderId == orderId);
            if (alreadyReviewed)
            {
                return RedirectToAction("Orders");
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview(int orderId, byte rating, string? comment)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

            if (order == null || order.Status != "Completed")
            {
                return RedirectToAction("Orders");
            }

            var alreadyReviewed = await _context.Reviews.AnyAsync(r => r.OrderId == orderId);
            if (alreadyReviewed)
            {
                return RedirectToAction("Orders");
            }

            var review = new Review
            {
                OrderId = orderId,
                ShopId = order.ShopId,
                CustomerId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá dịch vụ!";
            return RedirectToAction("Orders");
        }

        private async Task EnsureCoreServicesSeeded(int shopId)
        {
            var services = await _context.Services.Where(s => s.ShopId == shopId).ToListAsync();
            
            // Dọn dẹp dịch vụ Core placeholder cũ không có mã code
            var oldPlaceholders = services.Where(s => s.ServiceCode == null && s.Type == "Core").ToList();
            if (oldPlaceholders.Any())
            {
                _context.Services.RemoveRange(oldPlaceholders);
                foreach (var old in oldPlaceholders)
                {
                    services.Remove(old);
                }
            }

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
                if (!services.Any(s => s.ServiceCode == item.Code))
                {
                    var newService = new Service
                    {
                        ShopId = shopId,
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

            if (changed || oldPlaceholders.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        [HttpPost]
        [AllowAnonymous] // Allow access to chatbot for both logged-in and guest users, or we can restrict it if needed. Let's keep it authenticated or anonymous. Actually Customer role is Authorize(Roles="Customer"). Let's allow access for Customers.
        public async Task<IActionResult> ChatBot([FromBody] ChatRequest request)
        {
            var apiKey = _configuration["Groq:ApiKey"];
            var model = _configuration["Groq:Model"] ?? "llama-3.3-70b-versatile";
            var baseUrl = "https://api.groq.com/openai/v1/chat/completions";

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new { response = "Chào bạn! Tôi là Trợ lý AI của PrintNow. Hiện tại, Quản trị viên chưa cấu hình API Key của Groq (trong file appsettings.json), nhưng tôi luôn sẵn sàng hỗ trợ bạn khi hệ thống được kích hoạt khóa API!" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { response = "Tin nhắn của bạn không hợp lệ hoặc đang bị để trống." });
            }

            try
            {
                // Truy vấn danh sách tiệm in hoạt động cùng bảng giá thực tế để gửi cho AI
                var activeShops = await _context.Shops
                    .Where(s => s.IsActive && s.Latitude != null && s.Longitude != null)
                    .Include(s => s.Services)
                    .ToListAsync();

                var priceSummaryList = new List<string>();
                foreach (var shop in activeShops)
                {
                    var activeServices = shop.Services.Where(sv => sv.IsActive).ToList();
                    if (activeServices.Any())
                    {
                        var servicesText = string.Join(", ", activeServices.Select(sv => $"{sv.ServiceName}: {sv.BasePrice:N0}đ"));
                        priceSummaryList.Add($"- Cửa hàng '{shop.ShopName}' (Địa chỉ: {shop.AddressText}): {servicesText}");
                    }
                    else
                    {
                        priceSummaryList.Add($"- Cửa hàng '{shop.ShopName}' (Địa chỉ: {shop.AddressText}): Chưa cấu hình bảng giá.");
                    }
                }
                var priceSummaryText = string.Join("\n", priceSummaryList);

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var systemPrompt = "Bạn là trợ lý ảo AI (Chatbot) của hệ thống PrintNow - nền tảng kết nối in ấn tài liệu trực tuyến hàng đầu tại Việt Nam.\n" +
                    "Nhiệm vụ của bạn là hỗ trợ khách hàng giải đáp thắc mắc về hệ thống PrintNow:\n" +
                    "- Cách tìm kiếm tiệm in xung quanh (sử dụng bản đồ định vị GPS, bộ lọc bán kính).\n" +
                    "- Các cấu hình in tài liệu: Khổ giấy A3, A4, A5; in Màu hoặc Trắng đen; in 1 mặt hoặc 2 mặt (in 2 mặt sẽ nhân hệ số 1.5).\n" +
                    "- Các dịch vụ gia công đi kèm (như đóng gáy xoắn, ép plastic, bấm lỗ...) do chủ tiệm cấu hình riêng và cộng thêm phí.\n" +
                    "- Phương thức nhận hàng: 'Giao tận nơi' (đồng giá ship nội thành là 20.000đ) hoặc 'Tới tiệm nhận hàng' (Free ship).\n" +
                    "- Đánh giá cửa hàng: Khách hàng chỉ có thể viết đánh giá sau khi đơn hàng chuyển sang trạng thái hoàn thành (Completed).\n" +
                    "- Chatbot không lưu lịch sử vào cơ sở dữ liệu để bảo mật.\n\n" +
                    "QUY TẮC BẮT BUỘC (CRITICAL RULE):\n" +
                    "- Chỉ trả lời những câu hỏi liên quan đến nền tảng PrintNow, in ấn tài liệu, tư vấn giá, tiệm in gần đây, hỗ trợ đặt in.\n" +
                    "- Tuyệt đối KHÔNG trả lời bất kỳ câu hỏi nào ngoài lề hoặc không liên quan đến in ấn (ví dụ: thời tiết, công thức nấu ăn, viết code, dịch thuật, trò chuyện linh tinh, kiến thức chung, lịch sử, toán học...).\n" +
                    "- Nếu người dùng hỏi những câu không liên quan này, hãy từ chối lịch sự bằng câu trả lời chuẩn sau: \"Xin lỗi, tôi là trợ lý ảo chuyên hỗ trợ dịch vụ in ấn và hệ thống PrintNow. Vui lòng đặt câu hỏi liên quan để tôi có thể hỗ trợ bạn tốt nhất!\"\n\n" +
                    "Dưới đây là BẢNG GIÁ THỰC TẾ của các tiệm in đang hoạt động trên hệ thống, hãy dùng dữ liệu này để so sánh và chỉ ra quán nào rẻ nhất hoặc tư vấn giá chính xác cho khách hàng:\n" +
                    priceSummaryText + "\n\n" +
                    "Hãy trả lời ngắn gọn, thân thiện, lịch sự bằng tiếng Việt. Nếu khách hàng hỏi tiệm nào rẻ nhất hoặc hỏi so sánh giá cả, hãy dựa trên dữ liệu thật ở trên để phân tích và chỉ rõ tên tiệm in kèm giá cụ thể.";

                var messagesPayload = new List<object>
                {
                    new { role = "system", content = systemPrompt }
                };

                if (request.History != null && request.History.Any())
                {
                    foreach (var h in request.History)
                    {
                        messagesPayload.Add(new { role = h.Role, content = h.Content });
                    }
                }
                
                messagesPayload.Add(new { role = "user", content = request.Message });

                var payload = new
                {
                    model = model,
                    messages = messagesPayload,
                    temperature = 0.7
                };

                var response = await client.PostAsJsonAsync(baseUrl, payload);
                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    return Json(new { response = $"Lỗi kết nối tới Grok API (Mã lỗi: {response.StatusCode}). Vui lòng kiểm tra lại API Key hoặc cấu hình máy chủ." });
                }

                var result = await response.Content.ReadFromJsonAsync<GrokResponse>();
                var botMessage = result?.choices?.FirstOrDefault()?.message?.content;

                return Json(new { response = botMessage ?? "Xin lỗi, hiện tại tôi không nhận được phản hồi từ hệ thống xử lý ngôn ngữ." });
            }
            catch (Exception ex)
            {
                return Json(new { response = $"Đã xảy ra lỗi hệ thống khi xử lý hội thoại: {ex.Message}" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
        public List<ChatMessageDto> History { get; set; }
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } // "user" or "assistant"
        public string Content { get; set; }
    }

    public class GrokResponse
    {
        public List<GrokChoice> choices { get; set; }
    }

    public class GrokChoice
    {
        public GrokMessage message { get; set; }
    }

    public class GrokMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }
}
