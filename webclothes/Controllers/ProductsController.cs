using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webclothes.Data;
using webclothes.Policies;

using Microsoft.Extensions.Caching.Memory;

namespace webclothes.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public ProductsController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index(int? categoryId, string? searchString, decimal? minPrice, decimal? maxPrice, string? sortOrder, int page = 1)
        {
            const int pageSize = 9;
            var query = _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted && !p.Category.IsDeleted)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
                var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == categoryId.Value);
                if (category != null)
                {
                    ViewData["CategoryName"] = category.Name;
                }
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Name.Contains(searchString));
                ViewBag.SearchString = searchString;
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
                ViewBag.MinPrice = minPrice;
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
                ViewBag.MaxPrice = maxPrice;
            }

            ViewBag.SortOrder = sortOrder;
            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.Id)
            };

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page < 1)
            {
                page = 1;
            }
            else if (page > totalPages)
            {
                page = totalPages;
            }

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categories = await _cache.GetOrCreateAsync("AllCategories", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
                return _context.Categories.AsNoTracking().ToListAsync();
            });

            ViewBag.Categories = categories;
            ViewBag.CategoryId = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_ProductListPartial", products);
            }

            return View(products);
        }

        public async Task<IActionResult> Details(string id)
        {
            bool isNumeric = int.TryParse(id, out int numericId);

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Reviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => (!p.IsDeleted) && (p.Slug == id || (isNumeric && p.Id == numericId)));

            if (product == null)
            {
                return NotFound("Không thể tìm thấy thông tin sản phẩm này.");
            }

            var relatedProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && !p.IsDeleted)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;
            ViewBag.AverageRating = product.Reviews != null && product.Reviews.Any()
                ? Math.Round(product.Reviews.Average(r => r.Rating), 1)
                : 0.0;
            ViewBag.ReviewCount = product.Reviews?.Count ?? 0;

            var hasOrdered = false;
            var isInWishlist = false;
            var hasReviewed = false;

            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);
                if (currentUser != null)
                {
                    hasOrdered = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .AnyAsync(o =>
                            (o.UserId == currentUser.Id || o.UserId == currentUser.Email) &&
                            o.Status == "Hoàn thành" &&
                            !o.IsDeleted &&
                            o.OrderDetails != null &&
                            o.OrderDetails.Any(od => od.ProductId == product.Id));

                    isInWishlist = await _context.Wishlists.AnyAsync(w => w.UserId == currentUser.Id && w.ProductId == product.Id);
                    hasReviewed = product.Reviews?.Any(r => r.UserId == currentUser.Id) == true;
                    ViewBag.CurrentUserReview = product.Reviews?
                        .OrderByDescending(r => r.CreatedAt)
                        .FirstOrDefault(r => r.UserId == currentUser.Id);
                }
            }

            ViewBag.HasOrdered = hasOrdered;
            ViewBag.HasReviewed = hasReviewed;
            ViewBag.CanReview = ReviewSubmissionPolicy.CanSubmit(hasOrdered, hasReviewed);
            ViewBag.IsInWishlist = isInWishlist;

            return View(product);
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity!.Name);
            var product = await _context.Products.FindAsync(productId);
            string redirectId = product?.Slug ?? productId.ToString();

            if (currentUser == null)
            {
                TempData["ErrorMsg"] = "Không thể xác định tài khoản để gửi đánh giá.";
                return RedirectToAction("Details", new { id = redirectId });
            }

            var hasOrdered = await _context.Orders
                .Include(o => o.OrderDetails)
                .AnyAsync(o =>
                    (o.UserId == currentUser.Id || o.UserId == currentUser.Email) &&
                    o.Status == "Hoàn thành" &&
                    !o.IsDeleted &&
                    o.OrderDetails != null &&
                    o.OrderDetails.Any(od => od.ProductId == productId));

            var hasExistingReview = await _context.Reviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == currentUser.Id);

            var validationError = ReviewSubmissionPolicy.GetValidationError(hasOrdered, hasExistingReview, rating, comment);
            if (validationError != null)
            {
                TempData["ErrorMsg"] = validationError;
                return RedirectToAction("Details", new { id = redirectId });
            }

            _context.Reviews.Add(new webclothes.Models.Review
            {
                ProductId = productId,
                UserId = currentUser.Id,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMsg"] = "Đánh giá của bạn đã được gửi thành công!";
            return RedirectToAction("Details", new { id = redirectId });
        }
    }
}
