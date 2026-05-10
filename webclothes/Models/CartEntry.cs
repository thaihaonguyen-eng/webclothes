using System;

namespace webclothes.Models
{
    public class CartEntry
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public Product? Product { get; set; }
    }
}
