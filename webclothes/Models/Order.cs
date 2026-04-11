using System;
using System.Collections.Generic;

namespace webclothes.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }

        // Mối quan hệ 1 Đơn hàng có nhiều Chi tiết đơn hàng
        public List<OrderDetail>? OrderDetails { get; set; }
    }
}