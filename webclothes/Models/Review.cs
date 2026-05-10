using System;
using Microsoft.AspNetCore.Identity;

namespace webclothes.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string? UserId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Product? Product { get; set; }
        public IdentityUser? User { get; set; }
    }
}
