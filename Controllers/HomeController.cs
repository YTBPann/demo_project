using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace OpenIDApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => View(); // Trang Home

        [Authorize]
        public IActionResult Dashboard()
        {
            var name = User.Identity?.Name;
            ViewBag.Name = name;
            return View();
        }
    }
}
