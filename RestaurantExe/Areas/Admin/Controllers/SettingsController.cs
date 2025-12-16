using Microsoft.AspNetCore.Mvc;
using Restaurant.Data;
using Restaurant.Entities;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        // Отобразить текущие настройки
        public async Task<IActionResult> Index()
        {
            var settings = await _context.Settings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new Settings(); // чтобы форма не была пустой
            }
            return View(settings);
        }

        // Обновить настройки (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Settings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = await _context.Settings.FirstOrDefaultAsync();

            if (existing == null)
            {
                _context.Settings.Add(model);
            }
            else
            {
                existing.RestaurantName = model.RestaurantName;
                existing.ServiceChargePercentage = model.ServiceChargePercentage;
                // другие поля обновить по необходимости
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Настройки сохранены!";
            return RedirectToAction(nameof(Index));
        }
    }
}
