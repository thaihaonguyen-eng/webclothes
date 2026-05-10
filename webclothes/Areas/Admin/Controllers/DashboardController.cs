using Microsoft.AspNetCore.Mvc;
using webclothes.Data;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
namespace webclothes.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Tính toán các con số thống kê
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalRevenue = _context.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.PendingOrders = _context.Orders.Count(o => o.Status == "Chờ xử lý");

            // Chart Data: Doanh thu theo 12 tháng của năm hiện tại
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = new decimal[12];

            var ordersThisYear = _context.Orders
                .Where(o => o.OrderDate.Year == currentYear)
                .ToList(); // Mang về bộ nhớ để tính toán nhanh

            for (int month = 1; month <= 12; month++)
            {
                monthlyRevenue[month - 1] = ordersThisYear
                    .Where(o => o.OrderDate.Month == month)
                    .Sum(o => o.TotalAmount);
            }

            ViewBag.ChartData = string.Join(",", monthlyRevenue);

            // Hoạt động gần đây (Hoạt động của mọi tài khoản)
            ViewBag.RecentActivities = _context.OrderStatusHistories
                .OrderByDescending(h => h.ChangedAt)
                .Take(10)
                .ToList();

            return View();
        }
    }
}