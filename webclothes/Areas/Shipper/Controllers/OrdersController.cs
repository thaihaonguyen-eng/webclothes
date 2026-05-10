using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using webclothes.Data;

namespace webclothes.Areas.Shipper.Controllers
{
    [Area("Shipper")]
    [Authorize(Roles = "Shipper")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly webclothes.Services.INotificationService _notificationService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public OrdersController(
            ApplicationDbContext context,
            webclothes.Services.INotificationService notificationService,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _notificationService = notificationService;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index(string? filter)
        {
            var query = _context.Orders
                .Where(o => o.Status == "Chờ lấy hàng" || o.Status == "Đang giao hàng" || o.Status == "Hoàn thành" || o.Status == "Thất bại");

            if (!string.IsNullOrEmpty(filter) && filter != "all")
            {
                query = query.Where(o => o.Status == filter);
            }

            ViewBag.CurrentFilter = filter ?? "all";
            var orders = query.OrderByDescending(o => o.OrderDate).ToList();
            return View(orders);
        }

        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? shipperNote, IFormFile? proofImage)
        {
            var allowedStatuses = new[] { "Đang giao hàng", "Hoàn thành", "Thất bại" };
            if (!allowedStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chuyển sang trạng thái này.";
                return RedirectToAction("Details", new { id });
            }

            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return RedirectToAction("Details", new { id });
            }

            var oldStatus = order.Status;
            order.Status = status;

            // Logic hoàn lại tồn kho nếu hủy đơn
            if (status == "Thất bại" && oldStatus != "Đã hủy" && oldStatus != "Thất bại")
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

            if (!string.IsNullOrWhiteSpace(shipperNote))
            {
                order.ShipperNote = shipperNote.Trim();
            }

            if (proofImage != null && proofImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "shipper-proof");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var safeFileName = Path.GetFileName(proofImage.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}_{safeFileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await proofImage.CopyToAsync(stream);
                order.ShipperProofImageUrl = $"/images/shipper-proof/{uniqueFileName}";
            }

            _context.OrderStatusHistories.Add(new webclothes.Models.OrderStatusHistory
            {
                OrderId = id,
                Status = status,
                ChangedBy = User.Identity?.Name ?? "Shipper",
                Note = shipperNote,
                ChangedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _notificationService.NotifyUserAsync(
                order.UserId,
                $"Đơn hàng #{order.Id} cập nhật từ shipper",
                string.IsNullOrWhiteSpace(shipperNote) ? $"Trạng thái mới: {status}." : $"Trạng thái mới: {status}. Ghi chú: {shipperNote}",
                status == "Thất bại" ? "danger" : "success",
                $"/Orders/Details/{order.Id}");

            await _notificationService.NotifyRoleAsync(
                "Seller",
                $"Shipper cập nhật đơn #{order.Id}",
                $"Trạng thái giao hàng mới: {status}.",
                status == "Thất bại" ? "warning" : "info",
                $"/Seller/Orders/Details/{order.Id}");

            TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("Details", new { id });
        }
    }
}
