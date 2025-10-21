using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using OpenIDApp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;


var builder = WebApplication.CreateBuilder(args);

// mysql
builder.Services.AddDbContext<OpenIDContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    )
);

// google openID
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    /*options.Events.OnCreatingTicket = async context =>
    {
        var picture = context.User.GetProperty("picture").GetString();
        if (!string.IsNullOrEmpty(picture))
        {
            var identity = (ClaimsIdentity)context.Principal.Identity!;
            identity.AddClaim(new Claim("picture", picture));
        }
    };*/
})
.AddCookie(c =>
{
    c.Cookie.SameSite = SameSiteMode.Lax;       // tránh bị chặn khi quay về
    c.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    c.LoginPath = "/Account/Login";
    c.LogoutPath = "/Account/Logout";
    c.AccessDeniedPath = "/Account/AccessDenied";
})

.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Events.OnCreatingTicket = async context =>
    {
        var picture = context.User.GetProperty("picture").GetString();
        if (!string.IsNullOrEmpty(picture))
        {
            var identity = (ClaimsIdentity)context.Principal.Identity!;
            identity.AddClaim(new Claim("picture", picture));
        }
    };
})

.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"];
    options.CallbackPath = "/signin-github";
});

// mvc 
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => false;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
});
var app = builder.Build();

//  Middleware 
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

app.Run();
