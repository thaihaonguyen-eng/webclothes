using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webclothes.Data;
using webclothes.Models;

namespace webclothes.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang danh sách danh mục
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var categories = _context.Categories
                .Where(c => !c.IsDeleted)
                .Include(c => c.ParentCategory)
                .Include(c => c.SubCategories)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                categories = categories.Where(c => c.Name.Contains(searchString) || (c.Description != null && c.Description.Contains(searchString)));
            }

            return View(await categories.ToListAsync());
        }

        // 2. Trang tạo mới (GET)
        public IActionResult Create()
        {
            ViewBag.ParentCategories = _context.Categories.Where(c => c.ParentCategoryId == null).ToList();
            return View();
        }

        // 3. Xử lý tạo mới (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(category.Slug))
                {
                    category.Slug = GenerateSlug(category.Name);
                }

                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentCategories = _context.Categories.Where(c => c.ParentCategoryId == null).ToList();
            return View(category);
        }

        // 4. Trang chỉnh sửa (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            ViewBag.ParentCategories = _context.Categories.Where(c => c.ParentCategoryId == null && c.Id != id).ToList();
            return View(category);
        }

        // 5. Xử lý chỉnh sửa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (string.IsNullOrEmpty(category.Slug))
                    {
                        category.Slug = GenerateSlug(category.Name);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentCategories = _context.Categories.Where(c => c.ParentCategoryId == null && c.Id != id).ToList();
            return View(category);
        }

        // 6. Xóa danh mục
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories
                .Include(c => c.SubCategories)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (category == null) return NotFound();

            category.IsDeleted = true;
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.Id == id && !e.IsDeleted);
        }

        private string GenerateSlug(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return "";
            string str = phrase.ToLower();
            str = System.Text.RegularExpressions.Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s+", " ").Trim();
            str = System.Text.RegularExpressions.Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}