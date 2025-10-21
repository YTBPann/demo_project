using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIDApp.Data;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;

using AppUser = OpenIDApp.Models.User;
using OpenIDApp.Models;

namespace OpenIDApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Login(string provider = GoogleDefaults.AuthenticationScheme, string? returnUrl = null)
        {
            var scheme = string.IsNullOrWhiteSpace(provider) ? GoogleDefaults.AuthenticationScheme : provider;
            var redirectUrl = Url.Action(nameof(ExternalResponse), "Account", new { provider = scheme, returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

            if (!string.IsNullOrEmpty(returnUrl))
            {
                properties.Items["returnUrl"] = returnUrl;
            }

            return Challenge(properties, scheme);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalResponse(string provider = GoogleDefaults.AuthenticationScheme, string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            var externalClaims = result.Principal?.Identities.FirstOrDefault()?.Claims ?? Enumerable.Empty<Claim>();
            var email = externalClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = externalClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var providerKey = externalClaims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var picture = externalClaims.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(providerKey))
            {
                return RedirectToAction("Index", "Home");
            }

            var scheme = string.IsNullOrWhiteSpace(provider) ? GoogleDefaults.AuthenticationScheme : provider;

            var existingLogin = await _context.UserLogins
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Provider == scheme && l.ProviderId == providerKey);

            AppUser user;

            if (existingLogin != null)
            {
                user = existingLogin.User;
            }
            else
            {
                AppUser? existingUser = null;

                if (!string.IsNullOrEmpty(email))
                {
                    // Gán role mặc định khi lần đầu đăng nhập
                    existingUser = await _context.Users
                        .Include(u => u.Logins)
                        .FirstOrDefaultAsync(u => u.Email == email);
                }

                // Lưu user vào Claims
                var userClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                if (!string.IsNullOrEmpty(user.Picture))
                {
                    userClaims.Add(new Claim("picture", user.Picture));
                }

                var claimsIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties());

                return RedirectToAction("Index", "Home");
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied(string? returnUrl = null)
        {
            return View();
        }
    }
}

/*{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Login()
        {
            string redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = result.Principal?.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var id = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var picture = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (email != null)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == email);
                if (user == null)
                {
                    // set role mặc định
                    user = new AppUser
                    {
                        GoogleId = id,
                        Name = name,
                        Email = email,
                        Picture = picture,
                        Role = "guest"
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // lưu user vào Claims (Session)
                var claimsIdentity = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties());

                // return RedirectToAction("Welcome", "Home");
                // Điều hướng tùy theo Role 
                if (user.Role == "teacher")
                    return RedirectToAction("Index", "Teacher");
                else
                    return RedirectToAction("Index", "Student");
      
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied(string? returnUrl = null)
        {
            return View();
        }
    }
}
*/