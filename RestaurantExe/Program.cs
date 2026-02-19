using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurant.Authorization;
using Restaurant.Data;
using Restaurant.Entities;
using Restaurant.Hubs;
using Restaurant.Services;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

// Куки-аутентификация (как было)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Admin/Account/Login";
    options.LogoutPath = "/Admin/Account/Logout";
    options.AccessDeniedPath = "/Admin/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
});

// Авторизация по Permissions
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Permissions.All)
    {
        options.AddPolicy(permission, policy =>
            policy.RequireClaim(Permissions.ClaimType, permission));
    }
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddSignalR();
builder.Services.AddHostedService<ReservationReminderService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Инициализация БД и супер-админа (как у вас уже есть)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    var adminRoleName = "Admin";
    var protectedUserName = "superadmin";
    var protectedPassword = "SuperAdmin#123";

    if (!await roleManager.RoleExistsAsync(adminRoleName))
        await roleManager.CreateAsync(new IdentityRole(adminRoleName));

    var adminRole = await roleManager.FindByNameAsync(adminRoleName);
    if (adminRole != null)
    {
        var currentClaims = await roleManager.GetClaimsAsync(adminRole);
        var existing = currentClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var p in Permissions.All)
            if (!existing.Contains(p))
                await roleManager.AddClaimAsync(adminRole, new Claim(Permissions.ClaimType, p));
    }

    var superAdmin = await userManager.FindByNameAsync(protectedUserName);
    if (superAdmin == null)
    {
        superAdmin = new ApplicationUser
        {
            UserName = protectedUserName,
            FullName = "SuperAdmin",
            PhoneNumber = "0000000000"
        };

        var createResult = await userManager.CreateAsync(superAdmin, protectedPassword);
        if (createResult.Succeeded)
            await userManager.AddToRoleAsync(superAdmin, adminRoleName);
    }

    var allRoleNames = roleManager.Roles.Select(r => r.Name!).ToList();
    var userRoles = await userManager.GetRolesAsync(superAdmin);
    foreach (var roleName in allRoleNames)
        if (!userRoles.Contains(roleName))
            await userManager.AddToRoleAsync(superAdmin, roleName);
}

app.UseHttpsRedirection();
app.UseSession();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationsHub>("/notificationsHub");

app.Run();