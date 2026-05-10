using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webclothes.Data;
using webclothes.Models;

namespace webclothes.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.Identity?.Name;

            var orders = await _context.Orders
                .Where(o => o.UserId == userId || o.UserId == userEmail)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.Identity?.Name;

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(m => m.Id == id && (m.UserId == userId || m.UserId == userEmail));

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.StatusHistory = await _context.OrderStatusHistories
                .Where(h => h.OrderId == order.Id)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();

            return View(order);
        }
    }
}
