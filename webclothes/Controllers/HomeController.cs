using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webclothes.Data;
using webclothes.Models;

namespace webclothes.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // L?y 8 s?n ph?m m?i nh?t ?? hi?n ra trang ch?
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(8)
                .ToListAsync();

            return View(products);
        }

        public IActionResult Contact() => View();

        public IActionResult Privacy() => View();
    }
}