using Microsoft.AspNetCore.Mvc;
using webclothes.Data;
using webclothes.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace webclothes.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // 1. Hiển thị danh sách Sản phẩm
        public IActionResult Index(string searchString, int? categoryId, bool lowStock, string sortOrder)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = categoryId;
            ViewData["LowStock"] = lowStock;
            ViewData["CurrentSort"] = sortOrder;

            ViewData["PriceSortParm"] = sortOrder == "price_asc" ? "price_desc" : "price_asc";
            ViewData["StockSortParm"] = sortOrder == "stock_asc" ? "stock_desc" : "stock_asc";

            var products = _context.Products
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.Contains(searchString) || (p.Description != null && p.Description.Contains(searchString)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (lowStock)
            {
                products = products.Where(p => p.StockQuantity <= 5);
            }

            switch (sortOrder)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "stock_asc":
                    products = products.OrderBy(p => p.StockQuantity);
                    break;
                case "stock_desc":
                    products = products.OrderByDescending(p => p.StockQuantity);
                    break;
                default:
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            var categories = _context.Categories
                .Where(c => !c.IsDeleted)
                .ToDictionary(c => c.Id, c => c.Name);
            ViewBag.CategoriesDict = categories;
            return View(products.ToList());
        }

        // 2. Form Thêm sản phẩm mới
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(
                _context.Categories.Where(c => !c.IsDeleted).ToList(), "Id", "Name");
            return View();
        }

        // 3. Xử lý lưu sản phẩm mới
        [HttpPost]
        public IActionResult Create(Product product, IFormFile? imageFile, List<IFormFile>? galleryFiles)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Images");

            if (ModelState.IsValid)
            {
                // Upload ảnh đại diện
                if (imageFile != null && imageFile.Length > 0)
                {
                    product.MainImageUrl = SaveUploadedFile(imageFile);
                    product.ImageName = Path.GetFileName(product.MainImageUrl);
                }

                // Tự động tạo Slug từ tên sản phẩm
                if (string.IsNullOrEmpty(product.Slug))
                {
                    product.Slug = GenerateSlug(product.Name);
                }

                _context.Products.Add(product);
                _context.SaveChanges();

                // Upload ảnh phụ (Gallery)
                SaveGalleryImages(product.Id, galleryFiles);

                TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(
                _context.Categories.Where(c => !c.IsDeleted).ToList(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 4. Form Sửa sản phẩm
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var product = _context.Products
                .Include(p => p.Images)
                .FirstOrDefault(p => p.Id == id && !p.IsDeleted);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }
            ViewBag.Categories = new SelectList(
                _context.Categories.Where(c => !c.IsDeleted).ToList(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 5. Xử lý lưu thông tin sau khi Sửa
        [HttpPost]
        public IActionResult Edit(int id, Product product, IFormFile? imageFile, List<IFormFile>? galleryFiles)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Category");
            ModelState.Remove("Images");

            if (ModelState.IsValid)
            {
                // Upload ảnh đại diện mới (nếu có)
                if (imageFile != null && imageFile.Length > 0)
                {
                    product.MainImageUrl = SaveUploadedFile(imageFile);
                    product.ImageName = Path.GetFileName(product.MainImageUrl);
                }
                
                // Tự động tạo Slug nếu chưa có
                if (string.IsNullOrEmpty(product.Slug))
                {
                    product.Slug = GenerateSlug(product.Name);
                }

                _context.Products.Update(product);
                _context.SaveChanges();

                // Upload ảnh phụ mới (Gallery)
                SaveGalleryImages(product.Id, galleryFiles);

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(
                _context.Categories.Where(c => !c.IsDeleted).ToList(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // 6. Xử lý Xóa sản phẩm (Soft Delete)
        [HttpPost]
        public IActionResult Delete(int id)
        {
            var product = _context.Products.Find(id);
            if (product != null)
            {
                product.IsDeleted = true;
                _context.Products.Update(product);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // 7. Xóa ảnh phụ (AJAX)
        [HttpPost]
        public IActionResult DeleteGalleryImage(int id)
        {
            var image = _context.ProductImages.Find(id);
            if (image != null)
            {
                DeletePhysicalFile(image.ImageUrl);

                _context.ProductImages.Remove(image);
                _context.SaveChanges();
                return Json(new { success = true, message = "Đã xóa ảnh" });
            }
            return Json(new { success = false, message = "Không tìm thấy ảnh" });
        }

        // ============== HELPER METHODS ==============

        private string SaveUploadedFile(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images/products");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }
            return "/images/products/" + uniqueFileName;
        }

        private void SaveGalleryImages(int productId, List<IFormFile>? galleryFiles)
        {
            if (galleryFiles == null || galleryFiles.Count == 0) return;

            foreach (var file in galleryFiles)
            {
                if (file.Length > 0)
                {
                    string imageUrl = SaveUploadedFile(file);
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = productId,
                        ImageUrl = imageUrl,
                        IsMain = false
                    });
                }
            }
            _context.SaveChanges();
        }

        private void DeletePhysicalFile(string imageUrl)
        {
            var fileName = imageUrl.Replace("/images/products/", "");
            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images/products", fileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
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