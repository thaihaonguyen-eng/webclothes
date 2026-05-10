using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using webclothes.Data;
using Microsoft.Extensions.Caching.Memory;

namespace webclothes.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

        public CategoryMenuViewComponent(ApplicationDbContext context, Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _cache.GetOrCreateAsync("CategoryMenu", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = System.TimeSpan.FromHours(12);
                return await _context.Categories
                    .AsNoTracking()
                    .Include(c => c.SubCategories)
                    .Where(c => c.ParentCategoryId == null && !c.IsDeleted)
                    .ToListAsync();
            });
                
            return View(categories);
        }
    }
}
