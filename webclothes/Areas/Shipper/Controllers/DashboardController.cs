using Microsoft.AspNetCore.Mvc;
using webclothes.Data;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace webclothes.Areas.Shipper.Controllers
{
    [Area("Shipper")]
    [Authorize(Roles = "Shipper")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Thống kê cho Shipper - chỉ tập trung vào đơn giao
            ViewBag.WaitingPickup = _context.Orders.Count(o => o.Status == "Chờ lấy hàng");
            ViewBag.Delivering = _context.Orders.Count(o => o.Status == "Đang giao hàng");
            ViewBag.Completed = _context.Orders.Count(o => o.Status == "Hoàn thành");
            ViewBag.Failed = _context.Orders.Count(o => o.Status == "Thất bại");

            // Tổng đơn shipper có thể xử lý
            ViewBag.TotalAssignable = _context.Orders.Count(o => 
                o.Status == "Chờ lấy hàng" || o.Status == "Đang giao hàng");

            // Đơn gần đây cần giao
            ViewBag.RecentOrders = _context.Orders
                .Where(o => o.Status == "Chờ lấy hàng" || o.Status == "Đang giao hàng")
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            return View();
        }
    }
}
