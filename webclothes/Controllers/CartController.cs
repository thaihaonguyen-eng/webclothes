using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webclothes.Data;
using webclothes.Models;
using System.Text.Json;

namespace webclothes.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly webclothes.Services.IEmailSender _emailSender;
        private readonly webclothes.Services.INotificationService _notificationService;
        private const string CartSessionKey = "CartSession";

        public CartController(ApplicationDbContext context, 
            webclothes.Services.IEmailSender emailSender,
            webclothes.Services.INotificationService notificationService)
        {
            _context = context;
            _emailSender = emailSender;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity = 1, string? size = null)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();

            if (!string.IsNullOrEmpty(product.Size) && string.IsNullOrEmpty(size))
            {
                TempData["CartError"] = "Vui lòng chọn kích cỡ (Size) trước khi thêm vào giỏ hàng.";
                return RedirectToAction("Details", "Products", new { id = product.Slug ?? product.Id.ToString() });
            }

            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);

            if (item != null)
            {
                item.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name ?? "Sản phẩm",
                    ProductImage = product.MainImageUrl ?? string.Empty,
                    Price = product.DisplayPrice,
                    Quantity = quantity,
                    Size = size
                });
            }

            SaveCartItems(cart);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int productId, string? size = null)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartItems(cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity, string? size = null)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (item != null && quantity > 0)
            {
                item.Quantity = quantity;
                SaveCartItems(cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult IncreaseQuantity(int productId, string? size = null)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (item != null)
            {
                var product = _context.Products.Find(productId);
                if (product != null && item.Quantity < product.StockQuantity)
                {
                    item.Quantity++;
                    SaveCartItems(cart);
                }
                else if (product != null)
                {
                    TempData["CartError"] = $"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} mặt hàng!";
                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult DecreaseQuantity(int productId, string? size = null)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (item != null && item.Quantity > 1)
            {
                item.Quantity--;
                SaveCartItems(cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cart = GetCartItems();
            if (!cart.Any()) return RedirectToAction("Index");

            decimal total = cart.Sum(i => i.Price * i.Quantity);
            decimal discount = 0;
            string? activeVoucher = HttpContext.Session.GetString("ActiveVoucherCode");

            if (!string.IsNullOrEmpty(activeVoucher))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == activeVoucher && v.Quantity > 0 && v.ExpiryDate >= DateTime.Now);
                if (voucher != null)
                {
                    if (voucher.DiscountType == "Percent")
                    {
                        discount = (total * (decimal)voucher.DiscountAmount) / 100;
                        if (voucher.MaxDiscount > 0 && discount > voucher.MaxDiscount)
                        {
                            discount = voucher.MaxDiscount;
                        }
                    }
                    else
                    {
                        discount = (decimal)voucher.DiscountAmount;
                    }
                }
            }

            ViewBag.Total = total;
            ViewBag.Discount = discount;
            ViewBag.FinalTotal = total - discount;
            ViewBag.ActiveVoucher = activeVoucher;

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string customerName, string phone, string address, string email, string paymentMethod)
        {
            var cart = GetCartItems();
            if (!cart.Any()) return RedirectToAction("Index");

            decimal total = cart.Sum(i => i.Price * i.Quantity);
            decimal discount = 0;
            string? voucherCode = HttpContext.Session.GetString("ActiveVoucherCode");

            if (!string.IsNullOrEmpty(voucherCode))
            {
                var v = _context.Vouchers.FirstOrDefault(x => x.Code == voucherCode);
                if (v != null && v.Quantity > 0 && v.ExpiryDate >= DateTime.Now)
                {
                    if (v.DiscountType == "Percent")
                    {
                        discount = (total * (decimal)v.DiscountAmount) / 100;
                        if (v.MaxDiscount > 0 && discount > v.MaxDiscount) discount = v.MaxDiscount;
                    }
                    else discount = (decimal)v.DiscountAmount;

                    v.Quantity--;
                }
            }

            Order? order = null;
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var item in cart)
                    {
                        var productCheck = await _context.Products.FindAsync(item.ProductId);
                        if (productCheck == null || productCheck.StockQuantity < item.Quantity)
                        {
                            TempData["CartError"] = $"Rất tiếc! '{item.ProductName}' chỉ còn lại {(productCheck?.StockQuantity ?? 0)} sản phẩm. Vui lòng cập nhật lại giỏ hàng.";
                            return RedirectToAction("Index");
                        }
                    }

                    order = new Order
                    {
                        CustomerName = customerName,
                        Phone = phone,
                        Address = address,
                        OrderDate = DateTime.Now,
                        TotalAmount = total - discount,
                        DiscountAmount = discount,
                        VoucherCode = voucherCode,
                        Status = paymentMethod == "VNPAY" ? "Chờ thanh toán VNPay" : "Chờ xử lý",
                        UserId = GetCurrentUserId() ?? email
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var item in cart)
                    {
                        var product = await _context.Products.FindAsync(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= item.Quantity;
                        }

                        _context.OrderDetails.Add(new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.Price,
                            Size = item.Size
                        });
                    }

                    _context.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        Status = order.Status,
                        ChangedAt = DateTime.Now,
                        ChangedBy = customerName
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    HttpContext.Session.Remove(CartSessionKey);
                    HttpContext.Session.Remove("ActiveVoucherCode");

                    // QR logic
                    HttpContext.Session.SetString("OrderId", order.Id.ToString());
                    HttpContext.Session.SetString("OrderTotal", (total - discount).ToString());

                    // Send Confirmation Email
                    string toEmail = email ?? GetCurrentUserId() ?? "khachhang@example.com";
                    string emailSubject = $"Xác nhận đơn hàng #{order.Id} từ WEBCLOTHES";
                    string emailBody = $@"
                        <h3>Cảm ơn bạn đã đặt hàng tại WEBCLOTHES!</h3>
                        <p>Mã đơn hàng của bạn là: <strong>#{order.Id}</strong></p>
                        <p>Tổng tiền: <strong>{(total - discount):N0} VNĐ</strong></p>
                        <p>Trạng thái: <strong>{order.Status}</strong></p>
                        <p>Chúng tôi sẽ sớm liên hệ để giao hàng cho bạn.</p>
                    ";
                    try
                    {
                        await _emailSender.SendEmailAsync(toEmail, emailSubject, emailBody);
                    }
                    catch (Exception)
                    {
                        // Ignore email failure
                    }

                    // Notify Seller
                    await _notificationService.NotifyRoleAsync(
                        "Seller",
                        "Đơn hàng mới từ khách hàng",
                        $"Khách hàng {customerName} vừa đặt đơn #{order.Id} trị giá {(total - discount):N0} đ.",
                        "primary",
                        $"/Seller/Orders/Details/{order.Id}");
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["CartError"] = "Có lỗi xảy ra khi xử lý đơn hàng. Vui lòng thử lại.";
                    return RedirectToAction("Index");
                }
            }

            if (paymentMethod == "VNPAY")
            {
                return RedirectToAction("Payment", "VNPay", new { orderId = order.Id });
            }

            return RedirectToAction("OrderSuccess");
        }

        [HttpPost]
        public IActionResult ApplyVoucher(string code)
        {
            var cart = GetCartItems();
            decimal total = cart.Sum(i => i.Price * i.Quantity);

            var voucher = _context.Vouchers.FirstOrDefault(v => v.Code == code);
            if (voucher == null)
            {
                TempData["VoucherError"] = "Mã giảm giá không tồn tại.";
            }
            else if (voucher.Quantity <= 0)
            {
                TempData["VoucherError"] = "Mã giảm giá đã hết lượt sử dụng.";
            }
            else if (voucher.ExpiryDate < DateTime.Now)
            {
                TempData["VoucherError"] = "Mã giảm giá đã hết hạn.";
            }
            else if (voucher.MinOrderAmount > 0 && total < voucher.MinOrderAmount)
            {
                TempData["VoucherError"] = $"Đơn hàng tối thiểu {voucher.MinOrderAmount:N0} đ mới được dùng mã này.";
            }
            else
            {
                HttpContext.Session.SetString("ActiveVoucherCode", code);
                string msg = voucher.DiscountType == "Percent"
                    ? $"Áp dụng mã {code} thành công! (Giảm {voucher.DiscountAmount}%, tối đa {voucher.MaxDiscount:N0} đ)"
                    : $"Áp dụng mã {code} thành công! (Giảm {voucher.DiscountAmount:N0} đ)";
                TempData["SuccessMessage"] = msg;
            }

            return RedirectToAction("Checkout");
        }

        public IActionResult OrderSuccess()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddToCartAjax([FromForm] int productId, [FromForm] string? size)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            if (!string.IsNullOrEmpty(product.Size) && string.IsNullOrEmpty(size))
            {
                return Json(new { success = false, message = "Vui lòng chọn kích cỡ (Size)." });
            }

            var cart = GetCartItems();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            var requestedQuantity = (existingItem?.Quantity ?? 0) + 1;

            if (product.StockQuantity < requestedQuantity)
            {
                return Json(new { success = false, message = $"Sản phẩm chỉ còn {product.StockQuantity} mặt hàng trong kho!" });
            }

            if (existingItem != null)
            {
                existingItem.Quantity++;
                existingItem.Price = product.DisplayPrice;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name ?? "Sản phẩm",
                    ProductImage = product.MainImageUrl ?? string.Empty,
                    Price = product.DisplayPrice,
                    Quantity = 1,
                    Size = size
                });
            }

            SaveCartItems(cart);
            int totalItems = cart.Sum(c => c.Quantity);

            return Json(new
            {
                success = true,
                message = "Đã thêm vào giỏ hàng!",
                totalItems = totalItems
            });
        }

        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString(CartSessionKey);
            return sessionData == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(sessionData)!;
        }

        private void SaveCartItems(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }
}
