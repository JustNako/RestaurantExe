// Areas/Admin/Controllers/TablesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Data;
using Restaurant.Entities;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Authorization;

namespace Restaurant.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TablesController : Controller
    {
        private readonly AppDbContext _context;

        public TablesController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Index()
        {
            var tables = await _context.Tables.ToListAsync();
            return View(tables);
        }

        [Authorize(Policy = Permissions.Orders.View)]
        public async Task<IActionResult> Dashboard()
        {
            var tables = await _context.Tables
                .Include(t => t.Reservations)
                .ToListAsync();
            
            // Передаем информацию о статусах столов
            ViewBag.TableStatuses = tables.ToDictionary(
                t => t.Id, 
                t => Restaurant.Helpers.TableStatusHelper.GetTableStatus(_context, t)
            );
            
            // Передаем информацию о текущих и будущих бронированиях
            ViewBag.CurrentReservations = tables.ToDictionary(
                t => t.Id,
                t => Restaurant.Helpers.TableStatusHelper.GetCurrentReservation(_context, t.Id)
            );
            
            ViewBag.NextReservations = tables.ToDictionary(
                t => t.Id,
                t => Restaurant.Helpers.TableStatusHelper.GetNextReservation(_context, t.Id)
            );
            
            return View(tables);
        }

        [Authorize(Policy = Permissions.Orders.Create)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Create)]
        public async Task<IActionResult> Create(Table table)
        {
            if (!ModelState.IsValid)
                return View(table);

            // Проверяем, не существует ли уже стол с таким номером
            var existingTable = await _context.Tables.FirstOrDefaultAsync(t => t.Number == table.Number);
            if (existingTable != null)
            {
                ModelState.AddModelError("Number", "Стол с таким номером уже существует");
                return View(table);
            }

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            // Автоматически создаем QR код для нового стола
            var qrCode = GenerateUniqueQrCode();
            var qrLink = new QrLink
            {
                Code = qrCode,
                Type = QrLinkType.Table,
                TableId = table.Id,
                Description = $"Стол №{table.Number}",
                IsActive = true
            };

            _context.QrLinks.Add(qrLink);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = $"✅ Стол №{table.Number} успешно добавлен! QR код: {qrCode}";
            return RedirectToAction(nameof(Dashboard));
        }

        private string GenerateUniqueQrCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            string code;
            
            do
            {
                code = new string(Enumerable.Repeat(chars, 8)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_context.QrLinks.Any(q => q.Code == code));

            return code;
        }

        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> Edit(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();
            return View(table);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Orders.Edit)]
        public async Task<IActionResult> Edit(Table table)
        {
            if (!ModelState.IsValid)
                return View(table);

            _context.Tables.Update(table);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Dashboard));
        }

        [Authorize(Policy = Permissions.Orders.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();
            return View(table);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = Permissions.Orders.Delete)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null) return NotFound();

            try
            {
                _context.Tables.Remove(table);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"🗑️ Стол №{table.Number} удален.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "Невозможно удалить стол, так как он связан с заказами.";
            }
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
