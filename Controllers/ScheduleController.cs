using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIDApp.Models;

namespace OpenIDApp.Controllers
{
    [Authorize]
    public class ScheduleController : Controller
    {
        private readonly OpenIDContext _context;

        public ScheduleController(OpenIDContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "student")]
        public async Task<IActionResult> Student()
        {
            return View();
        }
    }
}
