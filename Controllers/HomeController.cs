using OpenIDApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication; 
using Microsoft.AspNetCore.Authentication.Cookies;

namespace OpenIDApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly OpenIDContext _context;
        public HomeController(OpenIDContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated) // Nếu đã đăng nhập rồi thì tự chuyển sang trang chào mừng
            {
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
        
        [Authorize(Roles = "admin")]
        public IActionResult AdminDashboard()
        {
            ViewBag.Title = "Bảng điều khiển quản trị";
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View("AdminDashboard");
        }

        [Authorize(Roles = "teacher")]
        public IActionResult TeacherDashboard()
        {
            ViewBag.Title = "Trang giảng viên";
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View("TeacherDashboard");
        }

        [Authorize(Roles = "student")]
        public IActionResult StudentDashboard()
        {
            ViewBag.Title = "Trang sinh viên";
            ViewBag.UserName = User.FindFirstValue(ClaimTypes.Name);
            return View("StudentDashboard");
        }
        [Authorize]
        public IActionResult Profile()
        {
            var user = new
            {
                Name = User.Identity?.Name,
                Email = User.FindFirst(ClaimTypes.Email)?.Value,
                Provider = User.FindFirst("Provider")?.Value,
                Picture = User.FindFirst("Picture")?.Value,
                Role = User.FindFirst(ClaimTypes.Role)?.Value
            };

            return View(user);
        }
        
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateDisplayName(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
            {
                ViewBag.Message = "Tên không hợp lệ.";
                return RedirectToAction("Profile");
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (email == null)
            {
                ViewBag.Message = "Không tìm thấy người dùng.";
                return RedirectToAction("Profile");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.Name = newName;
                await _context.SaveChangesAsync();

                // Cập nhật lại claim đăng nhập để hiện tên mới ngay lập tức
                var claimsIdentity = (ClaimsIdentity)User.Identity!;
                var oldNameClaim = claimsIdentity.FindFirst(ClaimTypes.Name);
                if (oldNameClaim != null)
                {
                    claimsIdentity.RemoveClaim(oldNameClaim);
                    claimsIdentity.AddClaim(new Claim(ClaimTypes.Name, newName));
                }

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity)
                );

                ViewBag.Message = "Đổi tên thành công!";
            }

            return RedirectToAction("Profile");
        }

        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateRole(int id, string newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Role = newRole;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ManageUsers");
        }
    }
}
