using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace webclothes.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => (u.Email != null && u.Email.Contains(searchString)) || 
                                                   (u.UserName != null && u.UserName.Contains(searchString)) || 
                                                   (u.PhoneNumber != null && u.PhoneNumber.Contains(searchString)));
            }

            var users = await usersQuery.ToListAsync();
            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles.Add(user.Id, roles);
            }

            ViewBag.UserRoles = userRoles;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // Xóa tất cả các role hiện tại của user
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Không thể xóa quyền cũ của người dùng.";
                return RedirectToAction(nameof(Index));
            }

            // Thêm role mới (nếu có chọn)
            if (!string.IsNullOrEmpty(roleName) && roleName != "None")
            {
                var addResult = await _userManager.AddToRoleAsync(user, roleName);
                if (!addResult.Succeeded)
                {
                    TempData["ErrorMessage"] = "Lỗi khi cấp quyền mới.";
                    return RedirectToAction(nameof(Index));
                }
            }

            TempData["SuccessMessage"] = $"Đã cập nhật quyền thành công cho {user.Email}";
            return RedirectToAction(nameof(Index));
        }
    }
}
