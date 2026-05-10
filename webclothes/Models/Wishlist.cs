using System;
using Microsoft.AspNetCore.Identity;

namespace webclothes.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int ProductId { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation
        public IdentityUser? User { get; set; }
        public Product? Product { get; set; }
    }
}
