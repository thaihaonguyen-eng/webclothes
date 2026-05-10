using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace webclothes.Controllers
{
    [Authorize]
    public class AccountRedirectController : Controller
    {
        /// <summary>
        /// Redirect người dùng sau khi đăng nhập đến đúng Area theo role
        /// </summary>
        public IActionResult RedirectByRole()
        {
            if (User.IsInRole("Admin"))
            {
                return Redirect("/Admin/Dashboard");
            }
            else if (User.IsInRole("Seller"))
            {
                return Redirect("/Seller/Dashboard");
            }
            else if (User.IsInRole("Shipper"))
            {
                return Redirect("/Shipper/Dashboard");
            }
            
            // Người dùng thông thường -> trang chủ
            return Redirect("/");
        }
    }
}
