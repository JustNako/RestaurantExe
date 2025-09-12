using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Restaurant.Entities;
using Restaurant.Authorization;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Restaurant.TagHelpers
{
    [HtmlTargetElement("permission")]
    public class PermissionTagHelper : TagHelper
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public PermissionTagHelper(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Один permission
        public string AspPermission { get; set; }

        // Несколько permissions (OR)
        public string[] AspPermissions { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null || !user.Identity.IsAuthenticated)
            {
                output.SuppressOutput();
                return;
            }

            var hasPermission = false;

            // Проверяем один permission
            if (!string.IsNullOrEmpty(AspPermission))
            {
                hasPermission = CheckPermission(user, AspPermission).GetAwaiter().GetResult();
            }
            // Проверяем массив permissions
            else if (AspPermissions != null && AspPermissions.Length > 0)
            {
                foreach (var perm in AspPermissions)
                {
                    if (CheckPermission(user, perm).GetAwaiter().GetResult())
                    {
                        hasPermission = true;
                        break;
                    }
                }
            }

            if (!hasPermission)
            {
                output.SuppressOutput();
            }
        }

        private async Task<bool> CheckPermission(ClaimsPrincipal user, string permission)
        {
            // Сначала проверяем claims пользователя
            if (user.HasClaim(c => c.Type == Permissions.ClaimType && c.Value == permission))
            {
                return true;
            }

            // Проверяем роли пользователя
            var appUser = await _userManager.GetUserAsync(user);
            if (appUser == null) return false;

            var roles = await _userManager.GetRolesAsync(appUser);
            foreach (var roleName in roles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null) continue;

                var claims = await _roleManager.GetClaimsAsync(role);
                if (claims.Any(c => c.Type == Permissions.ClaimType && c.Value == permission))
                    return true;
            }

            return false;
        }
    }
}
