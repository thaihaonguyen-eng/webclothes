using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using webclothes.Data;
using webclothes.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace webclothes.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly webclothes.Services.IEmailSender _emailSender;

        public CartController(ApplicationDbContext context, webclothes.Services.IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        // 1. Thêm sản phẩm vào giỏ
        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart") ?? new List<CartItem>();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1
                });
            }

            HttpContext.Session.Set("MyCart", cart);
            TempData["SuccessMessage"] = $"Đã thêm {product.Name} vào giỏ hàng!";

            return RedirectToAction("Index", "Home");
        }

        // 2. Xem giỏ hàng
        public IActionResult Index()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart") ?? new List<CartItem>();
            return View(cart);
        }

        // 3. Xóa sản phẩm khỏi giỏ hàng
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart");

            if (cart != null)
            {
                cart.RemoveAll(p => p.ProductId == productId);
                HttpContext.Session.Set("MyCart", cart);
            }

            return RedirectToAction("Index");
        }

        public IActionResult IncreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart");
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    item.Quantity++;
                }
                HttpContext.Session.Set("MyCart", cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult DecreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart");
            if (cart != null)
            {
                var item = cart.FirstOrDefault(c => c.ProductId == productId);
                if (item != null)
                {
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                    }
                    else
                    {
                        cart.Remove(item); // Nếu số lượng nhỏ hơn hoặc bằng 1 thì xóa luôn
                    }
                }
                HttpContext.Session.Set("MyCart", cart);
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // PHẦN MỚI THÊM: XỬ LÝ THANH TOÁN & ĐẶT HÀNG
        // ==========================================

        // 4. Hiển thị form điền thông tin thanh toán
        [HttpGet]
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart") ?? new List<CartItem>();

            // Nếu giỏ hàng trống thì bắt quay về trang chủ
            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(cart);
        }

        // 5. Xử lý lưu đơn hàng vào Database khi khách bấm "Xác nhận đặt hàng"
        [HttpPost]
        public async Task<IActionResult> Checkout(string customerName, string phone, string address, string email)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index", "Home");

            // B1: Tạo Đơn hàng mới
            var order = new Order
            {
                CustomerName = customerName,
                Phone = phone,
                Address = address,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(c => c.Price * c.Quantity),
                Status = "Chờ xử lý",
                UserId = User.Identity.IsAuthenticated ? User.Identity.Name : null
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // B2: Tạo Chi tiết
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price
                };
                _context.OrderDetails.Add(orderDetail);
            }
            await _context.SaveChangesAsync();

            // Lưu thông tin để hiện mã QR
            HttpContext.Session.SetString("OrderTotal", order.TotalAmount.ToString());
            HttpContext.Session.SetString("OrderId", order.Id.ToString());

            // Gửi email xác nhận nếu có nhập email
            if (!string.IsNullOrEmpty(email))
            {
                string subject = $"Xác nhận đơn hàng #{order.Id} từ WEBCLOTHES";
                string message = $"<h3>Xin chào {customerName},</h3><p>Cảm ơn bạn đã đặt hàng. Tổng số tiền: <strong>{order.TotalAmount:N0} VNĐ</strong>.</p><p>Đơn hàng sẽ được giao đến: {address}</p>";
                await _emailSender.SendEmailAsync(email, subject, message);
            }

            HttpContext.Session.Remove("MyCart");
            return RedirectToAction("OrderSuccess");
        }

        // 6. Trang thông báo đặt hàng thành công
        public IActionResult OrderSuccess()
        {
            return View();
        }
        // Hàm mới xử lý Add to Cart bằng Ajax
        [HttpPost]
        public IActionResult AddToCartAjax([FromForm] int productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }

            var cart = HttpContext.Session.Get<List<CartItem>>("MyCart") ?? new List<CartItem>();
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = 1,
                    // Nếu Model CartItem chưa có ImageName thì bỏ dòng dưới đi nhé
                    // ImageName = product.ImageName 
                });
            }

            HttpContext.Session.Set("MyCart", cart);

            // Đếm xem trong giỏ đang có tổng bao nhiêu món đồ
            int totalItems = cart.Sum(c => c.Quantity);

            // Trả về dữ liệu JSON cho Javascript xử lý
            return Json(new
            {
                success = true,
                message = $"Đã thêm {product.Name} vào giỏ hàng!",
                cartCount = totalItems
            });
        }
    }
}