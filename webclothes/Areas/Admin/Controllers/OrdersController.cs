using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using webclothes.Data;
using Microsoft.AspNetCore.Authorization;
namespace webclothes.Areas.Admin.Controllers
{
    [Area("Admin")] // Bắt buộc phải có thẻ này để định danh Area
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly webclothes.Services.INotificationService _notificationService;

        public OrdersController(ApplicationDbContext context, webclothes.Services.INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public IActionResult Index(string searchString, string status, DateTime? fromDate, DateTime? toDate, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentStatus"] = status;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentSort"] = sortOrder;

            ViewData["DateSortParm"] = string.IsNullOrEmpty(sortOrder) ? "date_asc" : "";
            ViewData["TotalSortParm"] = sortOrder == "total_asc" ? "total_desc" : "total_asc";

            var orders = _context.Orders.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => (o.CustomerName != null && o.CustomerName.Contains(searchString)) || 
                                           (o.Phone != null && o.Phone.Contains(searchString)) || 
                                           (o.Id.ToString().Contains(searchString)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.Status == status);
            }

            if (fromDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                orders = orders.Where(o => o.OrderDate <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            switch (sortOrder)
            {
                case "date_asc":
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
                case "total_asc":
                    orders = orders.OrderBy(o => o.TotalAmount);
                    break;
                case "total_desc":
                    orders = orders.OrderByDescending(o => o.TotalAmount);
                    break;
                default:
                    orders = orders.OrderByDescending(o => o.OrderDate);
                    break;
            }

            return View(orders.ToList());
        }

        public IActionResult Details(int id)
        {
            var order = _context.Orders.Include(o => o.OrderDetails).ThenInclude(d => d.Product).FirstOrDefault(o => o.Id == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order != null)
            {
                var oldStatus = order.Status;
                order.Status = status;

                // Logic hoàn lại tồn kho nếu hủy đơn
                if ((status == "Đã hủy" || status == "Thất bại") && (oldStatus != "Đã hủy" && oldStatus != "Thất bại"))
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = await _context.Products.FindAsync(detail.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity += detail.Quantity;
                        }
                    }
                }

                _context.OrderStatusHistories.Add(new webclothes.Models.OrderStatusHistory
                {
                    OrderId = id,
                    Status = status,
                    ChangedBy = User.Identity?.Name ?? "Admin",
                    ChangedAt = System.DateTime.Now
                });
                await _context.SaveChangesAsync();

                await _notificationService.NotifyUserAsync(
                    order.UserId,
                    $"Đơn hàng #{order.Id} đã được admin cập nhật",
                    $"Trạng thái mới: {status}.",
                    (status == "Đã hủy" || status == "Thất bại") ? "danger" : "info",
                    $"/Orders/Details/{order.Id}");
            }
            return RedirectToAction("Details", new { id = id });
        }
    }
}