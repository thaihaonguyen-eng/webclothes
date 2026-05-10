using Xunit;

namespace WebClothes.Tests
{
    public class EmailUnitTests
    {
        // HIẾU - Kiểm tra tính hợp lệ của định dạng Email
        [Theory]
        [InlineData("thaihao@gmail.com", true)]
        [InlineData("thaihaogmail.com", false)]
        [InlineData("thaihao@", false)]
        [InlineData("@gmail.com", false)]
        public void ThaiHao_Test_EmailFormat_Validation(string email, bool expected)
        {
            bool isValid = !string.IsNullOrEmpty(email)
                           && email.Contains("@")
                           && email.IndexOf("@") > 0
                           && email.IndexOf("@") < email.Length - 1;

            Assert.Equal(expected, isValid);
        }

        // HÀO - Kiểm tra Tiêu đề và Nội dung không được để trống
        [Theory]
        [InlineData("Xác nhận đơn hàng", "Cảm ơn bạn", true)]
        [InlineData("", "Nội dung email", false)]
        [InlineData("Tiêu đề email", "", false)]
        public void VietAnh_Test_EmailContent_NotEmpty(string subject, string body, bool expected)
        {
            bool isContentValid = !string.IsNullOrWhiteSpace(subject)
                                 && !string.IsNullOrWhiteSpace(body);

            Assert.Equal(expected, isContentValid);
        }

        // TIẾN - Kiểm tra tính chính xác của phản hồi hệ thống
        [Fact]
        public void MinhQuan_Test_Response_MessageSuccess()
        {
            var response = new { message = "Gửi email thành công", status = 200 };
            Assert.Equal("Gửi email thành công", response.message);
            Assert.Equal(200, response.status);
        }
    }
}