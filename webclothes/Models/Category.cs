using System.ComponentModel.DataAnnotations;

namespace webclothes.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();

        public string? Slug { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Thêm trường hỗ trợ Danh mục con
        public int? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    }
}