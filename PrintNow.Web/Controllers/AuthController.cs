using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PrintNow.Web.Data;
using PrintNow.Web.Models;
using System.Security.Claims;

namespace PrintNow.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly PrintNowContext _context;

        public AuthController(PrintNowContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            
            // Note: In a real application, you should hash the input password and compare with PasswordHash.
            // For this demo, we're doing a simple match.
            if (user != null && user.PasswordHash == password)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectBasedOnRole(user.Role);
            }

            ViewBag.Error = "Email hoặc mật khẩu không chính xác.";
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole();
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string role, string fullName, string phone, string email, string password, string? shopName)
        {
            // Simple validation
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                ViewBag.Role = role; // Keep the selected tab
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ViewBag.Error = "Email đã được sử dụng.";
                ViewBag.Role = role;
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                Phone = phone,
                Email = email,
                PasswordHash = password, // Remember to hash in production
                Role = role
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // If ShopOwner, also create a Shop record
            if (role == "ShopOwner" && !string.IsNullOrWhiteSpace(shopName))
            {
                var shop = new Shop
                {
                    OwnerId = user.Id,
                    ShopName = shopName,
                    IsActive = true
                };
                _context.Shops.Add(shop);
                await _context.SaveChangesAsync();
            }

            // Auto-login after registration
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectBasedOnRole(user.Role);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            // Nếu có session bị từ chối truy cập, redirect về đúng Dashboard của họ
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole();
            }
            return RedirectToAction("Login");
        }

        private IActionResult RedirectBasedOnRole(string? role = null)
        {
            if (string.IsNullOrEmpty(role))
            {
                role = User.FindFirstValue(ClaimTypes.Role);
            }

            if (role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }
            if (role == "ShopOwner")
            {
                return RedirectToAction("Index", "ShopAdmin");
            }
            return RedirectToAction("Index", "Customer");
        }
    }
}
