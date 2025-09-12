using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Введите логин и пароль");
                return View();
            }

            // 1) Логин по полному имени
            var nameInput = username.Trim();
            var userByName = await _userManager.Users.FirstOrDefaultAsync(u => u.FullName == nameInput);
            if (userByName != null)
            {
                var pwdCheck = await _signInManager.CheckPasswordSignInAsync(userByName, password, lockoutOnFailure: false);
                if (pwdCheck.Succeeded)
                {
                    await _signInManager.SignInAsync(userByName, isPersistent: false);
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
            }

            // 2) Фоллбэк: логин по телефону (мы сохраняем UserName как только цифры из номера)
            var cleaned = new string(username.Where(char.IsDigit).ToArray());
            var result = await _signInManager.PasswordSignInAsync(cleaned, password, isPersistent: false, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                // 3) Последний фоллбэк: как введено
                result = await _signInManager.PasswordSignInAsync(username, password, isPersistent: false, lockoutOnFailure: false);
            }
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            ModelState.AddModelError("", "Неверный логин или пароль");
            return View();
        }

        [Authorize]
        [AcceptVerbs("GET", "POST")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
