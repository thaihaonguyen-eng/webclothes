using Microsoft.AspNetCore.Authorization; // Thêm để dùng [Authorize]
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webclothes.Data;
using webclothes.Models;

namespace webclothes.Controllers
{
    [Authorize] // Bảo mật: Bắt buộc phải đăng nhập mới vào được các trang trong Controller này
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang lịch sử đơn hàng của khách
        public async Task<IActionResult> History()
        {
            var userEmail = User.Identity.Name;
            var orders = await _context.Orders
                .Where(o => o.UserId == userEmail)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Tác phong xịn: Thêm trang xem chi tiết một đơn hàng cụ thể (nếu cần)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userEmail = User.Identity.Name;
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userEmail);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}