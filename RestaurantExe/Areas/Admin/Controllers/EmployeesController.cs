using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Entities;
using Restaurant.ViewModels;
using Restaurant.Authorization;


namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = Permissions.Users.Manage)]
    public class EmployeesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeesController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;

        }




        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var employees = new List<EditEmployeeViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                employees.Add(new EditEmployeeViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? ""
                });
            }

            return View(employees); // ✅ Теперь всё правильно
        }



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateEmployeeViewModel
            {
                Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync()
            };
            return View(model);
        }



        [HttpPost]

        public async Task<IActionResult> Create(CreateEmployeeViewModel model)
        {

            if (!ModelState.IsValid)
            {
                model.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var cleanedPhone = new string(model.PhoneNumber.Where(char.IsDigit).ToArray());
            var user = new ApplicationUser
            {
                // Логин = Полное имя
                UserName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.Role))
                await _userManager.AddToRoleAsync(user, model.Role);
            TempData["SuccessMessage"] = $"Пользователь {user.FullName} создан.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EditEmployeeViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Role = userRoles.FirstOrDefault(),
                Roles = allRoles
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditEmployeeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            var cleanedPhone = new string(model.PhoneNumber.Where(char.IsDigit).ToArray());
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            // Логин = Полное имя
            user.UserName = model.FullName.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError("", error.Description);

                model.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            // смена пароля (если введён)
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    model.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                    return View(model);
                }
            }

            // обновление роли (защита супер-админа)
            if (!string.Equals(user.UserName, "superadmin", StringComparison.OrdinalIgnoreCase))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!string.IsNullOrEmpty(model.Role))
                    await _userManager.AddToRoleAsync(user, model.Role);
            }

            TempData["SuccessMessage"] = $"Данные пользователя {user.FullName} обновлены.";
            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Protect permanent super admin
            if (string.Equals(user.UserName, "superadmin", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Этот пользователь защищен и не может быть удален.";
                return RedirectToAction("Index");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Ошибка при удалении пользователя.";
                return RedirectToAction("Index");
            }

            TempData["SuccessMessage"] = "Пользователь успешно удален.";
            return RedirectToAction("Index");
        }




    }



}
