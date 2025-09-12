using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Restaurant.Entities;
using System;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Restaurant.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQLite with a predictable on-disk path: <ContentRoot>/App_Data/restaurant.db
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var contentRoot = builder.Environment.ContentRootPath;
    var dataDir = Path.Combine(contentRoot, "App_Data");
    if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
    var dbPath = Path.Combine(dataDir, "restaurant.db");
    options.UseSqlite($"Data Source={dbPath}");
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

// Refresh user claims immediately when roles/claims change
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.LogoutPath = "/Admin/Account/Logout";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
});

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim(Permissions.ClaimType, permission));
    }
});

builder.Services.AddHttpContextAccessor();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


// Create SQLite database if it doesn't exist (no migrations required)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Seed undeletable super admin
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var adminRoleName = "Admin";
    var protectedUserName = "superadmin"; // cannot be deleted
    var protectedPassword = "SuperAdmin#123"; // change after first login

    if (!await roleManager.RoleExistsAsync(adminRoleName))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));
    }

    // Ensure Admin role has all permission claims
    var adminRole = await roleManager.FindByNameAsync(adminRoleName);
    if (adminRole != null)
    {
        var currentClaims = await roleManager.GetClaimsAsync(adminRole);
        var existing = currentClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var p in Permissions.All)
        {
            if (!existing.Contains(p))
            {
                await roleManager.AddClaimAsync(adminRole, new Claim(Permissions.ClaimType, p));
            }
        }
    }

    var superAdmin = await userManager.FindByNameAsync(protectedUserName);
    if (superAdmin == null)
    {
        superAdmin = new ApplicationUser
        {
            UserName = protectedUserName,
            FullName = "Super Admin",
            PhoneNumber = "0000000000"
        };

        var createResult = await userManager.CreateAsync(superAdmin, protectedPassword);
        if (createResult.Succeeded)
        {
            await userManager.AddToRoleAsync(superAdmin, adminRoleName);
        }
    }

    // Ensure superadmin is a member of all roles
    var allRoleNames = roleManager.Roles.Select(r => r.Name!).ToList();
    var userRoles = await userManager.GetRolesAsync(superAdmin);
    foreach (var roleName in allRoleNames)
    {
        if (!userRoles.Contains(roleName))
        {
            await userManager.AddToRoleAsync(superAdmin, roleName);
        }
    }
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "legacy_account",
    pattern: "Account/{action=Login}/{id?}",
    defaults: new { area = "Admin", controller = "Account" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Redirect}/{action=ToAdmin}/{id?}");




app.Run();
