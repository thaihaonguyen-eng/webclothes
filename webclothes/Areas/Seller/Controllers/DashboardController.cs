using Microsoft.AspNetCore.Mvc;
using webclothes.Data;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace webclothes.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Seller")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Thống kê cho Seller
            ViewBag.TotalProducts = _context.Products.Count();
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalRevenue = _context.Orders
                .Where(o => o.Status == "Hoàn thành")
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;
            ViewBag.PendingOrders = _context.Orders.Count(o => o.Status == "Chờ xử lý");
            ViewBag.ProcessingOrders = _context.Orders.Count(o => o.Status == "Chờ lấy hàng");
            ViewBag.CompletedOrders = _context.Orders.Count(o => o.Status == "Hoàn thành");
            ViewBag.TotalCategories = _context.Categories.Count();
            ViewBag.TotalVouchers = _context.Vouchers.Count();

            // Chart Data: Doanh thu theo 12 tháng
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = new decimal[12];
            var ordersThisYear = _context.Orders
                .Where(o => o.OrderDate.Year == currentYear && o.Status == "Hoàn thành")
                .ToList();

            for (int month = 1; month <= 12; month++)
            {
                monthlyRevenue[month - 1] = ordersThisYear
                    .Where(o => o.OrderDate.Month == month)
                    .Sum(o => o.TotalAmount);
            }
            ViewBag.ChartData = string.Join(",", monthlyRevenue);

            // Đơn hàng gần đây
            ViewBag.RecentOrders = _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToList();

            // Cảnh báo tồn kho thấp (< 10)
            ViewBag.LowStockProducts = _context.Products
                .Where(p => p.StockQuantity > 0 && p.StockQuantity < 10)
                .OrderBy(p => p.StockQuantity)
                .ToList();

            return View();
        }

        public IActionResult ExportOrdersCsv()
        {
            var orders = _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("OrderId,CustomerName,Phone,OrderDate,Status,TotalAmount,VoucherCode,DiscountAmount");

            foreach (var order in orders)
            {
                csv.AppendLine(string.Join(",",
                    order.Id,
                    EscapeCsv(order.CustomerName),
                    EscapeCsv(order.Phone),
                    order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    EscapeCsv(order.Status),
                    order.TotalAmount,
                    EscapeCsv(order.VoucherCode),
                    order.DiscountAmount));
            }

            var fileName = $"seller-orders-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "\"\"";
            }

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
