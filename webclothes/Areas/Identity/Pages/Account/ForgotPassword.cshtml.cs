using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using webclothes.Services;

namespace webclothes.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
            [Display(Name = "Tên đăng nhập")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                // Tìm tài khoản dựa trên Email
                var user = await _userManager.FindByEmailAsync(Input.Email);

                // Khớp tài khoản: kiểm tra xem user tồn tại và Username có khớp không
                if (user != null && user.UserName == Input.UserName)
                {
                    // Tạo mật khẩu mới theo định dạng "matkhauXXXX"
                    Random random = new Random();
                    string newPassword = "matkhau" + random.Next(1000, 9999).ToString();

                    // Generate reset token and reset password directly
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

                    if (resetResult.Succeeded)
                    {
                        // Gửi email
                        string subject = "Mật khẩu mới của bạn - Webclothes";
                        string message = $"<p>Xin chào <strong>{user.UserName}</strong>,</p>" +
                                         $"<p>Mật khẩu mới của bạn đã được tạo thành công.</p>" +
                                         $"<p>Mật khẩu mới của bạn là: <strong style='color:red;'>{newPassword}</strong></p>" +
                                         $"<p>Vui lòng đăng nhập bằng mật khẩu này và đổi lại mật khẩu để đảm bảo an toàn.</p>";

                        await _emailSender.SendEmailAsync(Input.Email, subject, message);

                        StatusMessage = "Mật khẩu mới đã được gửi tới email của bạn. Vui lòng kiểm tra hộp thư.";
                        return Page();
                    }
                    else
                    {
                        foreach (var error in resetResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }
                else
                {
                    // Không khớp
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc email không chính xác.");
                }
            }

            return Page();
        }
    }
}
