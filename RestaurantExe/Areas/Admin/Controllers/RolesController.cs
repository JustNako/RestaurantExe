using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Authorization;
using Perms = Restaurant.Authorization.Permissions;
using System.Security.Claims;
using Restaurant.ViewModels;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = Perms.Roles.Manage)]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Название роли обязательно");
                return View();
            }

            var exists = await _roleManager.RoleExistsAsync(name);
            if (exists)
            {
                ModelState.AddModelError("", "Такая роль уже существует");
                return View();
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(name));
            if (result.Succeeded)
            {
                // ensure superadmin has this role as well
                // Using UserManager would be ideal, but not injected here; rely on startup sync.
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
                return NotFound();

            var result = await _roleManager.DeleteAsync(role);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Permissions(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var current = await _roleManager.GetClaimsAsync(role);
            var model = new RolePermissionsViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name!,
                Permissions = Perms.All
                    .Select(p => new PermissionItem
                    {
                        Name = p,
                        Category = p.Split('.')[0],
                        Selected = current.Any(c => c.Type == Perms.ClaimType && c.Value == p)
                    })
                    .ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Permissions(RolePermissionsViewModel model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return NotFound();

            var current = await _roleManager.GetClaimsAsync(role);
            foreach (var c in current.Where(c => c.Type == Perms.ClaimType))
                await _roleManager.RemoveClaimAsync(role, c);

            var selected = model.Permissions.Where(p => p.Selected).Select(p => p.Name);
            foreach (var p in selected)
                await _roleManager.AddClaimAsync(role, new Claim(Perms.ClaimType, p));

            return RedirectToAction(nameof(Index));
        }
    }
}
