using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webclothes.Data;
using webclothes.Models;
using Microsoft.AspNetCore.Authorization;

namespace webclothes.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Seller")]
    public class VouchersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VouchersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var vouchers = _context.Vouchers.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                vouchers = vouchers.Where(v => v.Code.Contains(searchString));
            }

            vouchers = vouchers.OrderByDescending(v => v.Id);
            return View(await vouchers.ToListAsync());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo voucher thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(voucher);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Voucher voucher)
        {
            if (id != voucher.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Vouchers.Update(voucher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật voucher thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(voucher);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher != null)
            {
                _context.Vouchers.Remove(voucher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa voucher thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
