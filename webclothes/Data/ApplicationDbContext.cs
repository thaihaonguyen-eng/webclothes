using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // Thêm thư viện này
using Microsoft.EntityFrameworkCore;
using webclothes.Models;

namespace webclothes.Data
{
    // ĐỔI DbContext THÀNH IdentityDbContext Ở DÒNG DƯỚI ĐÂY:
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
    }
}