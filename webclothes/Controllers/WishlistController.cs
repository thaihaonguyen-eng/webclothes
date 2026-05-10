using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webclothes.Data;
using webclothes.Models;

namespace webclothes.Controllers
{
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang danh sách yêu thích
        public async Task<IActionResult> Index()
        {
            var userId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name))?.Id;
            if (userId == null) return RedirectToAction("Index", "Home");

            var wishlist = await _context.Wishlists
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedDate)
                .ToListAsync();

            return View(wishlist);
        }

        // Toggle yêu thích (Ajax)
        [HttpPost]
        public async Task<IActionResult> Toggle([FromForm] int productId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            if (user == null) return Json(new { success = false, message = "Bạn cần đăng nhập!" });

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = false, message = "Đã bỏ yêu thích" });
            }
            else
            {
                _context.Wishlists.Add(new Wishlist
                {
                    UserId = user.Id,
                    ProductId = productId,
                    AddedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return Json(new { success = true, added = true, message = "Đã thêm vào yêu thích â¤ï¸" });
            }
        }

        // Xóa khỏi wishlist
        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            if (user == null) return RedirectToAction("Index");

            var item = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == productId);

            if (item != null)
            {
                _context.Wishlists.Remove(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
