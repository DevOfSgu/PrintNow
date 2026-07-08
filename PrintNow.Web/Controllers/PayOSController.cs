using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models;
using PrintNow.Web.Data;
using PrintNow.Web.Models;

namespace PrintNow.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PayOSController : Controller
    {
        private readonly PayOSClient _payOS;
        private readonly PrintNowContext _context;
        private readonly IConfiguration _configuration;

        public PayOSController(PayOSClient payOS, PrintNowContext context, IConfiguration configuration)
        {
            _payOS = payOS;
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Webhook nhận thông báo thanh toán từ PayOS
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] Webhook webhook)
        {
            try
            {
                // Xác thực chữ ký từ PayOS (SDK v2)
                var verifiedData = await _payOS.Webhooks.VerifyAsync(webhook);

                if (verifiedData == null)
                {
                    return BadRequest(new { error = "Invalid webhook signature" });
                }

                var orderCode = verifiedData.OrderCode;
                var code = verifiedData.Code ?? "";
                var status = (code == "00") ? "PAID" : "CANCELLED";

                // Tìm giao dịch trong hệ thống
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.PayOSOrderCode == orderCode.ToString());

                if (transaction == null)
                {
                    return Ok(new { message = "Transaction not found, but webhook received" });
                }

                if (status == "PAID")
                {
                    transaction.Status = "Completed";
                    transaction.CompletedAt = DateTime.UtcNow;

                    if (transaction.TransactionType == "OrderPayment" && transaction.OrderId.HasValue)
                    {
                        // Cập nhật trạng thái đơn hàng
                        var order = await _context.Orders.FindAsync(transaction.OrderId.Value);
                        if (order != null)
                        {
                            order.PaymentStatus = "Paid";
                            order.PaymentMethod = "PayOS";

                            // Cộng doanh thu vào số dư của shop (đã trừ phí nền tảng)
                            var shop = await _context.Shops.FindAsync(order.ShopId);
                            if (shop != null)
                            {
                                // Giả sử phí nền tảng là 10% trên mỗi đơn hàng
                                var platformFeePercent = _configuration.GetValue<decimal>("PayOS:PlatformFeePercent");
                                if (platformFeePercent <= 0) platformFeePercent = 0.10m;
                                
                                var shopRevenue = order.TotalAmount * (1 - platformFeePercent);
                                shop.Balance += shopRevenue;
                            }
                        }
                    }
                    else if (transaction.TransactionType == "PlatformFee" && transaction.PlatformFeeId.HasValue)
                    {
                        // Cập nhật trạng thái phí sàn
                        var fee = await _context.PlatformFees.FindAsync(transaction.PlatformFeeId.Value);
                        if (fee != null)
                        {
                            fee.Status = "Paid";
                            fee.PaidAt = DateTime.UtcNow;

                            // Mở khóa shop
                            var shop = await _context.Shops.FindAsync(fee.ShopId);
                            if (shop != null)
                            {
                                shop.IsLocked = false;
                                shop.IsActive = true;
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                else if (status == "CANCELLED")
                {
                    transaction.Status = "Cancelled";
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán của một order code
        /// </summary>
        [HttpGet("check-status/{orderCode}")]
        public async Task<IActionResult> CheckPaymentStatus(string orderCode)
        {
            try
            {
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.PayOSOrderCode == orderCode);

                if (transaction == null)
                {
                    return NotFound(new { message = "Transaction not found" });
                }

                var paymentInfo = await _payOS.PaymentRequests.GetPaymentLinkInfoAsync(long.Parse(orderCode));

                return Ok(new
                {
                    status = paymentInfo.Status,
                    transactionStatus = transaction.Status,
                    amount = paymentInfo.Amount,
                    transaction.Amount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán phí sàn theo feeId
        /// </summary>
        [HttpGet("check-fee-status")]
        public async Task<IActionResult> CheckFeePaymentStatus(int feeId)
        {
            try
            {
                var fee = await _context.PlatformFees.FindAsync(feeId);
                if (fee == null)
                {
                    return NotFound(new { message = "Fee not found" });
                }

                if (fee.Status == "Paid")
                {
                    return Ok(new { status = "Completed", feeStatus = fee.Status });
                }

                if (!string.IsNullOrEmpty(fee.PayOSOrderCode))
                {
                    try
                    {
                        var paymentInfo = await _payOS.PaymentRequests.GetPaymentLinkInfoAsync(long.Parse(fee.PayOSOrderCode));
                        var payOSStatus = paymentInfo.Status;

                        if (payOSStatus == "PAID")
                        {
                            fee.Status = "Paid";
                            fee.PaidAt = DateTime.UtcNow;

                            // Cập nhật trạng thái giao dịch
                            var transaction = await _context.PaymentTransactions
                                .FirstOrDefaultAsync(t => t.PayOSOrderCode == fee.PayOSOrderCode);
                            if (transaction != null)
                            {
                                transaction.Status = "Completed";
                                transaction.CompletedAt = DateTime.UtcNow;
                            }

                            var shop = await _context.Shops.FindAsync(fee.ShopId);
                            if (shop != null)
                            {
                                shop.IsLocked = false;
                                shop.IsActive = true;
                            }

                            await _context.SaveChangesAsync();

                            return Ok(new { status = "Completed", feeStatus = "Paid" });
                        }

                        return Ok(new { status = payOSStatus, feeStatus = fee.Status });
                    }
                    catch
                    {
                        return Ok(new { status = "Unknown", feeStatus = fee.Status });
                    }
                }

                return Ok(new { status = "NoPaymentLink", feeStatus = fee.Status });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán đơn hàng theo orderId
        /// Trả về cả qrCode và checkoutUrl nếu có giao dịch đang chờ
        /// </summary>
        [HttpGet("check-order-status")]
        public async Task<IActionResult> CheckOrderPaymentStatus(int orderId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerId == userId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                if (order.PaymentStatus == "Paid")
                {
                    return Ok(new { status = "Completed", paymentStatus = order.PaymentStatus });
                }

                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == orderId && t.TransactionType == "OrderPayment");

                if (transaction != null && transaction.Status == "Completed")
                {
                    return Ok(new { status = "Completed", paymentStatus = order.PaymentStatus });
                }

                // Nếu có giao dịch đang chờ, trả về qrCode và checkoutUrl
                if (transaction != null && transaction.Status == "Pending")
                {
                    string? existingQrCode = null;
                    string? existingCheckoutUrl = null;

                    if (!string.IsNullOrEmpty(transaction.PayOSResponse))
                    {
                        var parts = transaction.PayOSResponse.Split('|');
                        existingCheckoutUrl = parts[0];
                        existingQrCode = parts.Length > 1 ? parts[1] : null;
                    }

                    // Kiểm tra trạng thái từ PayOS
                    if (!string.IsNullOrEmpty(transaction.PayOSOrderCode))
                    {
                        try
                        {
                            var paymentInfo = await _payOS.PaymentRequests.GetPaymentLinkInfoAsync(long.Parse(transaction.PayOSOrderCode));
                            if (paymentInfo.Status == "PAID")
                            {
                                transaction.Status = "Completed";
                                transaction.CompletedAt = DateTime.UtcNow;
                                order.PaymentStatus = "Paid";

                                var shop = await _context.Shops.FindAsync(order.ShopId);
                                if (shop != null)
                                {
                                    var platformFeePercent = _configuration.GetValue<decimal>("PayOS:PlatformFeePercent");
                                    if (platformFeePercent <= 0) platformFeePercent = 0.10m;
                                    shop.Balance += order.TotalAmount * (1 - platformFeePercent);
                                }

                                await _context.SaveChangesAsync();
                                return Ok(new { status = "Completed", paymentStatus = "Paid" });
                            }

                            return Ok(new
                            {
                                status = paymentInfo.Status,
                                paymentStatus = order.PaymentStatus,
                                hasExistingPayment = true,
                                checkoutUrl = existingCheckoutUrl,
                                qrCode = existingQrCode,
                                amount = order.TotalAmount
                            });
                        }
                        catch
                        {
                            // PayOS API error, still return existing payment data
                        }
                    }

                    return Ok(new
                    {
                        status = "Pending",
                        paymentStatus = order.PaymentStatus,
                        hasExistingPayment = true,
                        checkoutUrl = existingCheckoutUrl,
                        qrCode = existingQrCode,
                        amount = order.TotalAmount
                    });
                }

                // Kiểm tra trực tiếp từ PayOS nếu có orderCode
                if (transaction != null && !string.IsNullOrEmpty(transaction.PayOSOrderCode))
                {
                    try
                    {
                        var paymentInfo = await _payOS.PaymentRequests.GetPaymentLinkInfoAsync(long.Parse(transaction.PayOSOrderCode));
                        if (paymentInfo.Status == "PAID")
                        {
                            transaction.Status = "Completed";
                            transaction.CompletedAt = DateTime.UtcNow;
                            order.PaymentStatus = "Paid";

                            var shop = await _context.Shops.FindAsync(order.ShopId);
                            if (shop != null)
                            {
                                var platformFeePercent = _configuration.GetValue<decimal>("PayOS:PlatformFeePercent");
                                if (platformFeePercent <= 0) platformFeePercent = 0.10m;
                                shop.Balance += order.TotalAmount * (1 - platformFeePercent);
                            }

                            await _context.SaveChangesAsync();
                            return Ok(new { status = "Completed", paymentStatus = "Paid" });
                        }
                        return Ok(new { status = paymentInfo.Status, paymentStatus = order.PaymentStatus });
                    }
                    catch
                    {
                        return Ok(new { status = "Unknown", paymentStatus = order.PaymentStatus });
                    }
                }

                return Ok(new { status = "Pending", paymentStatus = order.PaymentStatus, hasExistingPayment = false });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Tạo link thanh toán PayOS cho đơn hàng
        /// </summary>
        [Authorize]
        [HttpPost("create-order-payment")]
        public async Task<IActionResult> CreateOrderPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == userId && (o.Status == "Cart" || o.Status == "Pending"));

                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                }

                // Tạo mã đơn hàng unique
                var orderCode = long.Parse(DateTimeOffset.Now.ToString("ffffff"));

                // Tạo description (tối đa 25 ký tự)
                var description = $"PN{order.Id}";

                var domain = $"{Request.Scheme}://{Request.Host}";

                var paymentRequest = new CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = (int)order.TotalAmount,
                    Description = description,
                    Items = order.OrderDetails.Select(d => new Item
                    {
                        Name = d.FileName.Length > 30 ? d.FileName[..30] : d.FileName,
                        Quantity = d.Quantity,
                        Price = (int)d.SubTotal
                    }).ToList(),
                    ReturnUrl = $"{domain}/Customer/PaymentSuccess?orderId={order.Id}",
                    CancelUrl = $"{domain}/Customer/PaymentCancel?orderId={order.Id}"
                };

                var response = await _payOS.PaymentRequests.CreateAsync(paymentRequest);

                // Lấy qrCode từ PayOS (VietQR - quét được bằng app ngân hàng)
                string? qrCode = null;
                try { qrCode = response.QrCode; } catch { /* fallback */ }

                // Lưu giao dịch (lưu cả checkoutUrl và qrCode vào PayOSResponse)
                var transaction = new PaymentTransaction
                {
                    TransactionType = "OrderPayment",
                    OrderId = order.Id,
                    ShopId = order.ShopId,
                    Amount = order.TotalAmount,
                    PayOSOrderCode = orderCode.ToString(),
                    PayOSPaymentLinkId = response.PaymentLinkId,
                    PayOSResponse = $"{response.CheckoutUrl}|{qrCode}",
                    Status = "Pending"
                };
                _context.PaymentTransactions.Add(transaction);

                // Cập nhật order với PayOS order code
                order.PaymentMethod = "PayOS";

                await _context.SaveChangesAsync();

                return Json(new { success = true, checkoutUrl = response.CheckoutUrl, qrCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi tạo thanh toán: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tạo link thanh toán PayOS cho phí sàn
        /// </summary>
        [Authorize]
        [HttpPost("create-fee-payment")]
        public async Task<IActionResult> CreateFeePayment([FromBody] CreateFeePaymentRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.OwnerId == userId);
                if (shop == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy tiệm in." });
                }

                var fee = await _context.PlatformFees.FindAsync(request.FeeId);
                if (fee == null || fee.ShopId != shop.Id)
                {
                    return Json(new { success = false, message = "Không tìm thấy hóa đơn phí sàn." });
                }

                if (fee.Status == "Paid")
                {
                    return Json(new { success = false, message = "Hóa đơn này đã được thanh toán." });
                }

                var orderCode = long.Parse(DateTimeOffset.Now.ToString("ffffff"));
                var domain = $"{Request.Scheme}://{Request.Host}";

                var paymentRequest = new CreatePaymentLinkRequest
                {
                    OrderCode = orderCode,
                    Amount = (int)fee.Amount,
                    Description = $"Phi san T{fee.Month}/{fee.Year}",
                    Items = new List<Item>
                    {
                        new Item { Name = $"Phí sàn tháng {fee.Month}/{fee.Year}", Quantity = 1, Price = (int)fee.Amount }
                    },
                    ReturnUrl = $"{domain}/ShopAdmin/FeePaymentSuccess?feeId={fee.Id}",
                    CancelUrl = $"{domain}/ShopAdmin/FeePaymentCancel?feeId={fee.Id}"
                };

                var response = await _payOS.PaymentRequests.CreateAsync(paymentRequest);

                // Lấy qrCode từ PayOS (VietQR - quét được bằng app ngân hàng)
                string? qrCode = null;
                try { qrCode = response.QrCode; } catch { /* fallback */ }

                // Lưu giao dịch (lưu cả checkoutUrl và qrCode vào PayOSResponse để dùng lại)
                var transaction = new PaymentTransaction
                {
                    TransactionType = "PlatformFee",
                    PlatformFeeId = fee.Id,
                    ShopId = shop.Id,
                    Amount = fee.Amount,
                    PayOSOrderCode = orderCode.ToString(),
                    PayOSPaymentLinkId = response.PaymentLinkId,
                    PayOSResponse = $"{response.CheckoutUrl}|{qrCode}",
                    Status = "Pending"
                };
                _context.PaymentTransactions.Add(transaction);

                fee.PayOSOrderCode = orderCode.ToString();
                await _context.SaveChangesAsync();

                return Json(new { success = true, checkoutUrl = response.CheckoutUrl, qrCode });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi tạo thanh toán: {ex.Message}" });
            }
        }
    }

    public class CreatePaymentRequest
    {
        public int OrderId { get; set; }
    }

    public class CreateFeePaymentRequest
    {
        public int FeeId { get; set; }
    }
}
