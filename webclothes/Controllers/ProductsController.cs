using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using webclothes.Data;

namespace webclothes.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang hiển thị danh sách sản phẩm theo danh mục (hoặc tất cả)
        public async Task<IActionResult> Index(int? categoryId)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                var category = await _context.Categories.FindAsync(categoryId.Value);
                if (category != null)
                {
                    ViewData["CategoryName"] = category.Name;
                }
            }

            var products = await query.OrderByDescending(p => p.Id).ToListAsync();
            return View(products);
        }

        // Trang thông tin chi tiết một sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound("Không thể tìm thấy thông tin sản phẩm này.");
            }

            // Lấy sản phẩm mười liên quan (cùng danh mục)
            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }
    }
}
