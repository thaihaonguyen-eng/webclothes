using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace webclothes.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(200)]
        public string Name { get; set; } = null!;
        [Required]
        public string Description { get; set; } = null!;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public string? MainImageUrl { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; } = null!;
        public string? ImageName { get; set; }
        public string? Size { get; set; }

        public int StockQuantity { get; set; } = 100;

        // Flash Sale
        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalePrice { get; set; }
        public DateTime? SaleEndDate { get; set; }

        // Computed: Giá hiển thị (sale nếu đang sale, ngược lại giá gốc)
        [NotMapped]
        public decimal DisplayPrice => (SalePrice.HasValue && SaleEndDate.HasValue && SaleEndDate > DateTime.Now) ? SalePrice.Value : Price;
        [NotMapped]
        public bool IsOnSale => SalePrice.HasValue && SaleEndDate.HasValue && SaleEndDate > DateTime.Now && SalePrice < Price;

        public string? Slug { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation property for reviews
        public List<Review>? Reviews { get; set; }
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}