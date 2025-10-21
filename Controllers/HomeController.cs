using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace OpenIDApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                // Nếu đã đăng nhập rồi thì tự chuyển sang trang chào mừng
                return RedirectToAction("Welcome", "Home");
            }

            return View();
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            var name = User.Identity?.Name;
            ViewBag.Name = name;
            return View();
        }

        [Authorize]
        public IActionResult Welcome()
        {
            var name = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name ?? "bạn";
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "thành viên";

            ViewBag.UserName = name;
            ViewBag.Role = role;

            return View();
        }
        
    }
}
