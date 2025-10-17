using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using OpenIDApp.Data;

namespace OpenIDApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Login() => Challenge(
            new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") },
            GoogleDefaults.AuthenticationScheme);

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type.Contains("emailaddress"))?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == "name")?.Value;

            if (email != null)
            {
                var existing = _context.Users.FirstOrDefault(u => u.Email == email);
                if (existing == null)
                {
                    _context.Users.Add(new User { Email = email, Name = name });
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Dashboard", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
