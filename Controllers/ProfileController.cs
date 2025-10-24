using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using OpenIDApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace OpenIDApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly OpenIDContext _context;
        public ProfileController(OpenIDContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email == null) return RedirectToAction("Index", "Home");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                TempData["Error"] = "Tên không hợp lệ.";
                return RedirectToAction("Index");
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Index");
            }


            if (user.LastNameChange != null && user.LastNameChange > DateTime.Now.AddDays(-7))
            {
                TempData["Error"] = "Bạn chỉ có thể đổi tên sau 7 ngày.";
                return RedirectToAction("Index");
            }

            user.Name = newName;
            user.LastNameChange = DateTime.Now;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật tên thành công!";

            // Cập nhật lại claim hiện tại để hiển thị ngay
            var claimsIdentity = (ClaimsIdentity)User.Identity!;
            var oldClaim = claimsIdentity.FindFirst(ClaimTypes.Name);
            if (oldClaim != null)
            {
                claimsIdentity.RemoveClaim(oldClaim);
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, newName));
            }

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity)
            );

            return RedirectToAction("Index");
        }
    }
}
