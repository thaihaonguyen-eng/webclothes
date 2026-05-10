using System;

namespace webclothes.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // Null = gửi cho tất cả
        public string? TargetRole { get; set; } // "Seller", "Shipper", "Admin"
        public string Title { get; set; } = null!;
        public string? Message { get; set; }
        public string Type { get; set; } = "info"; // info, success, warning, danger
        public string? LinkUrl { get; set; } // Đường dẫn khi click vào
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
