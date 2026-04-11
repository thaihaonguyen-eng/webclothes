public class Voucher
{
    public int Id { get; set; }
    public string Code { get; set; } = null!; // Mã: ví dụ GIAM20
    public decimal DiscountAmount { get; set; } // Số tiền giảm
    public int Quantity { get; set; } // Số lượng mã còn lại
    public DateTime ExpiryDate { get; set; } // Ngày hết hạn
}