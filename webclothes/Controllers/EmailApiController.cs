using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace webclothes.Controllers
{
    [Route("api/email")]
    [ApiController]
    public class EmailApiController : ControllerBase
    {
        [HttpPost("send")]
        public IActionResult SendEmail([FromBody] EmailRequest request)
        {
            // 1. Kiểm tra để trống (Test Case: Empty Email)
            if (string.IsNullOrEmpty(request.ToEmail))
            {
                return BadRequest(new { message = "Lỗi: Email không được để trống" });
            }

            // 2. Xử lý khoảng trắng (Test Case: Whitespace)
            string safeEmail = request.ToEmail.Trim();

            // 3. Kiểm tra định dạng (Test Case: Invalid Format)
            if (!safeEmail.Contains("@"))
            {
                return BadRequest(new { message = "Lỗi: Định dạng email không hợp lệ" });
            }

            try
            {
                using (var smtpClient = new SmtpClient("smtp.gmail.com"))
                {
                    smtpClient.Port = 587;
                    // Dùng Mật khẩu ứng dụng 16 chữ cái bạn đã tạo
                    smtpClient.Credentials = new NetworkCredential("nth04012005@gmail.com", "eiwm vqqc ujfp hnce");
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress("nth04012005@gmail.com", "Cửa hàng thời trang"),
                        Subject = request.Subject,
                        Body = request.Body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(safeEmail);

                    smtpClient.Send(mailMessage);
                }

                return Ok(new { message = "Gửi email thành công" });
            }
            catch (Exception ex)
            {
                // Trả về lỗi 500 nếu cấu hình SMTP sai hoặc mất mạng
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

    // Model hứng dữ liệu từ Postman gửi lên
    public class EmailRequest
    {
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}