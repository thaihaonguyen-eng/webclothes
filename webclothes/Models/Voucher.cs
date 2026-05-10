using System;

namespace webclothes.Models
{
    public class Voucher
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!; // Mã: ví dụ GIAM20
        public decimal DiscountAmount { get; set; } // Số tiền giảm (hoặc % giảm)
        public int Quantity { get; set; } // Số lượng mã còn lại
        public DateTime ExpiryDate { get; set; } // Ngày hết hạn
        public string DiscountType { get; set; } = "Fixed"; // "Fixed" hoặc "Percent"
        public decimal MaxDiscount { get; set; } = 0; // Giảm tối đa (cho Percent)
        public decimal MinOrderAmount { get; set; } = 0; // Đơn tối thiểu
    }
}