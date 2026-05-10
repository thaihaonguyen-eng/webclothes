using System;

namespace webclothes.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string? Status { get; set; }
        public string? ChangedBy { get; set; } // Email/Username của người thay đổi
        public string? Note { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.Now;

        // Navigation
        public Order? Order { get; set; }
    }
}
